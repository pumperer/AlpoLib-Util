using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace alpoLib.Util.WebApiClient
{
    public class WebApiClientInitializeContext
    {
        public string BaseUrl { get; init; }
        public int MaxConcurrentRequests { get; init; } = 3;
        public int DefaultMaxRetries { get; init; } = 3;
        public float DefaultRetryDelay { get; init; } = 1f;
    }

    public abstract class WebApiClient : MonoBehaviour
    {
        protected string BaseUrl;
        protected int MaxConcurrentRequests;
        protected int DefaultMaxRetries;
        protected float DefaultRetryDelay;

        private readonly Queue<RequestQueueItem> _requestQueue = new();
        private readonly List<Coroutine> _activeRequests = new();
        private bool _isProcessingQueue;

        protected abstract void OnHttpError(long responseCode, string error);

        protected static T Initialize<T>(WebApiClientInitializeContext context) where T : WebApiClient
        {
            var go = new GameObject();
            DontDestroyOnLoad(go);
            
            var client = go.AddComponent<T>();
            client.BaseUrl = context.BaseUrl;
            client.MaxConcurrentRequests = context.MaxConcurrentRequests;
            client.DefaultMaxRetries = context.DefaultMaxRetries;
            client.DefaultRetryDelay = context.DefaultRetryDelay;
            client.StartCoroutine(client.Co_ProcessRequestQueue());
            go.name = client.name;
            
            return client;
        }

        public void QueueRequest<TRequest, TResponse>(
            string endpoint,
            string method,
            TRequest requestData,
            Action<TResponse> onSuccess,
            Action<string> onLogicError = null,
            Action<long, string> onHttpErrorOverride = null,
            int maxRetries = -1,
            float retryDelay = -1f)
        {
            var queueItem = new RequestQueueItem
            {
                Endpoint = endpoint,
                Method = method,
                JsonData = requestData != null ? JsonConvert.SerializeObject(requestData) : null,
                OnSuccess = (response) => {
                    try
                    {
                        var deserializedResponse = JsonConvert.DeserializeObject<TResponse>(response);
                        onSuccess?.Invoke(deserializedResponse);
                    }
                    catch (Exception ex)
                    {
                        onLogicError?.Invoke($"Deserialization error: {ex.Message}");
                    }
                },
                OnLogicError = onLogicError,
                OnHttpError = onHttpErrorOverride ?? OnHttpError,
                RetryCount = 0,
                MaxRetries = maxRetries >= 0 ? maxRetries : DefaultMaxRetries,
                RetryDelay = retryDelay >= 0 ? retryDelay : DefaultRetryDelay
            };

            _requestQueue.Enqueue(queueItem);
        }

        private IEnumerator Co_ProcessRequestQueue()
        {
            _isProcessingQueue = true;

            while (_isProcessingQueue)
            {
                if (_requestQueue.Count > 0 && _activeRequests.Count < MaxConcurrentRequests)
                {
                    var request = _requestQueue.Dequeue();
                    var coroutine = StartCoroutine(Co_ExecuteRequest(request));
                    _activeRequests.Add(coroutine);
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        private IEnumerator Co_ExecuteRequest(RequestQueueItem requestItem)
        {
            var url = $"{BaseUrl}{requestItem.Endpoint}";
            var request = CreateRequest(url, requestItem.Method, requestItem.JsonData);

            yield return request.SendWebRequest();

            // 요청 완료 후 활성 요청 목록에서 제거
            _activeRequests.RemoveAll(c => c == null);

            if (request.result != UnityWebRequest.Result.Success)
            {
                if (ShouldRetry(request.responseCode) && requestItem.RetryCount < requestItem.MaxRetries)
                {
                    requestItem.RetryCount++;
                    Debug.LogWarning($"Request failed, retrying ({requestItem.RetryCount}/{requestItem.MaxRetries})");
                    
                    yield return new WaitForSeconds(requestItem.RetryDelay * requestItem.RetryCount);
                    _requestQueue.Enqueue(requestItem); // 재시도를 위해 큐에 다시 추가
                }
                else
                {
                    requestItem.OnHttpError?.Invoke(request.responseCode, request.error);
                }
            }
            else
            {
                requestItem.OnSuccess?.Invoke(request.downloadHandler.text);
            }

            request.Dispose();
        }

        private bool ShouldRetry(long responseCode)
        {
            // 재시도할 HTTP 상태 코드 정의
            return responseCode >= 500 || responseCode == 408 || responseCode == 429;
        }

        private UnityWebRequest CreateRequest(string url, string method, string jsonData)
        {
            UnityWebRequest request = new UnityWebRequest(url, method);

            if (!string.IsNullOrEmpty(jsonData) && (method == "POST" || method == "PUT"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            }

            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            return request;
        }

        public void ClearQueue()
        {
            _requestQueue.Clear();
        }

        public int GetQueueCount()
        {
            return _requestQueue.Count;
        }

        public int GetActiveRequestCount()
        {
            return _activeRequests.Count;
        }

        private void OnDestroy()
        {
            _isProcessingQueue = false;
            StopAllCoroutines();
        }
    }
}
