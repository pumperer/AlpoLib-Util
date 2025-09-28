using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace alpoLib.Util.WebApiClient
{
    /// <summary>
    /// Web API 클라이언트 초기화에 필요한 컨텍스트 정보를 담는 클래스
    /// init 접근자를 사용하여 객체 생성 시에만 값 설정 가능 (불변성 보장)
    /// </summary>
    public class WebApiClientInitializeContext
    {
        /// <summary>
        /// API 서버의 기본 URL (예: "https://api.example.com")
        /// </summary>
        public string BaseUrl { get; init; }
        
        /// <summary>
        /// 동시에 실행할 수 있는 최대 요청 수 (기본값: 3)
        /// </summary>
        public int MaxConcurrentRequests { get; init; } = 3;
        
        /// <summary>
        /// 요청 실패 시 재시도할 최대 횟수 (기본값: 3)
        /// </summary>
        public int DefaultMaxRetries { get; init; } = 3;
        
        /// <summary>
        /// 재시도 간격(초) (기본값: 1초)
        /// </summary>
        public float DefaultRetryDelay { get; init; } = 1f;
    }

    /// <summary>
    /// Unity용 Web API 클라이언트 추상 클래스
    /// - 비동기 HTTP 요청 처리 및 큐 관리
    /// - 자동 재시도 로직 구현
    /// - JSON 직렬화/역직렬화 자동 처리
    /// - 상속받는 클래스에서 HTTP 에러 처리 로직 구현 필요
    /// </summary>
    public abstract class WebApiClient : MonoBehaviour
    {
        /// <summary>
        /// API 서버의 기본 URL
        /// </summary>
        private string _baseUrl;
        
        /// <summary>
        /// 동시 실행 가능한 최대 요청 수
        /// </summary>
        private int _maxConcurrentRequests;
        
        /// <summary>
        /// 기본 재시도 최대 횟수
        /// </summary>
        private int _defaultMaxRetries;
        
        /// <summary>
        /// 기본 재시도 간격(초)
        /// </summary>
        private float _defaultRetryDelay;

        /// <summary>
        /// 대기 중인 요청들을 저장하는 큐
        /// </summary>
        private readonly Queue<RequestQueueItem> _requestQueue = new();
        
        /// <summary>
        /// 현재 실행 중인 코루틴들을 추적하는 리스트
        /// </summary>
        private readonly List<Coroutine> _activeRequests = new();
        
        /// <summary>
        /// 요청 큐 처리 상태 플래그
        /// </summary>
        private bool _isProcessingQueue;

        /// <summary>
        /// HTTP 에러 발생 시 호출되는 추상 메서드
        /// 상속받는 클래스에서 구체적인 에러 처리 로직 구현 필요
        /// </summary>
        /// <param name="responseCode">HTTP 상태 코드</param>
        /// <param name="error">에러 메시지</param>
        protected abstract void OnHttpError(long responseCode, string error);

        /// <summary>
        /// Web API 클라이언트를 초기화하고 GameObject를 생성하는 정적 메서드
        /// - 새로운 GameObject 생성 및 DontDestroyOnLoad 적용
        /// - 컨텍스트 정보를 바탕으로 클라이언트 설정
        /// - 요청 처리 큐 자동 시작
        /// </summary>
        /// <typeparam name="T">생성할 WebApiClient의 구체적인 타입</typeparam>
        /// <param name="context">초기화에 필요한 컨텍스트 정보</param>
        /// <returns>초기화된 WebApiClient 인스턴스</returns>
        protected static T Initialize<T>(WebApiClientInitializeContext context) where T : WebApiClient
        {
            var go = new GameObject();
            DontDestroyOnLoad(go);
            
            var client = go.AddComponent<T>();
            client.SetContext(context);
            client.StartCoroutine(client.Co_ProcessRequestQueue());
            go.name = client.name;
            
            return client;
        }
        
        private void SetContext(WebApiClientInitializeContext context)
        {
            _baseUrl = context.BaseUrl;
            _maxConcurrentRequests = context.MaxConcurrentRequests;
            _defaultMaxRetries = context.DefaultMaxRetries;
            _defaultRetryDelay = context.DefaultRetryDelay;
        }

        /// <summary>
        /// API 요청을 큐에 추가하는 메인 메서드
        /// - 자동 JSON 직렬화/역직렬화 처리
        /// - 성공/실패 콜백 등록
        /// - 재시도 설정 가능
        /// </summary>
        /// <typeparam name="TRequest">요청 데이터 타입</typeparam>
        /// <typeparam name="TResponse">응답 데이터 타입</typeparam>
        /// <param name="endpoint">API 엔드포인트 경로</param>
        /// <param name="method">HTTP 메서드 (GET, POST, PUT, DELETE 등)</param>
        /// <param name="requestData">요청에 포함할 데이터 (JSON으로 변환됨)</param>
        /// <param name="onSuccess">성공 시 호출될 콜백</param>
        /// <param name="onLogicError">논리적 에러 (파싱 실패 등) 발생 시 호출될 콜백</param>
        /// <param name="onHttpErrorOverride">특정 요청에 대한 HTTP 에러 처리 오버라이드</param>
        /// <param name="maxRetries">이 요청의 최대 재시도 횟수 (-1일 경우 기본값 사용)</param>
        /// <param name="retryDelay">이 요청의 재시도 간격 (-1일 경우 기본값 사용)</param>
        public void QueueRequest<TRequest, TResponse>(
            string endpoint,
            EMethod method,
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
                OnSuccess = response => {
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
                MaxRetries = maxRetries >= 0 ? maxRetries : _defaultMaxRetries,
                RetryDelay = retryDelay >= 0 ? retryDelay : _defaultRetryDelay
            };

            _requestQueue.Enqueue(queueItem);
        }

        /// <summary>
        /// 대기 중인 모든 요청을 큐에서 제거
        /// </summary>
        public void ClearQueue()
        {
            _requestQueue.Clear();
        }

        /// <summary>
        /// 현재 큐에 대기 중인 요청 수를 반환
        /// </summary>
        /// <returns>대기 중인 요청 수</returns>
        public int GetQueueCount()
        {
            return _requestQueue.Count;
        }

        /// <summary>
        /// 현재 실행 중인 요청 수를 반환
        /// </summary>
        /// <returns>실행 중인 요청 수</returns>
        public int GetActiveRequestCount()
        {
            return _activeRequests.Count;
        }

        /// <summary>
        /// 요청 큐를 지속적으로 처리하는 코루틴
        /// - 동시 실행 요청 수 제한 관리
        /// - 0.1초 간격으로 큐 상태 확인
        /// </summary>
        private IEnumerator Co_ProcessRequestQueue()
        {
            _isProcessingQueue = true;

            while (_isProcessingQueue)
            {
                // 대기 중인 요청이 있고 동시 실행 한도 내에 있을 때 새 요청 시작
                if (_requestQueue.Count > 0 && _activeRequests.Count < _maxConcurrentRequests)
                {
                    var request = _requestQueue.Dequeue();
                    var coroutine = StartCoroutine(Co_ExecuteRequest(request));
                    _activeRequests.Add(coroutine);
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        /// <summary>
        /// 개별 API 요청을 실행하는 코루틴
        /// - UnityWebRequest를 통한 HTTP 통신
        /// - 실패 시 재시도 로직 처리
        /// - 성공/실패 콜백 호출
        /// </summary>
        /// <param name="requestItem">실행할 요청 항목</param>
        private IEnumerator Co_ExecuteRequest(RequestQueueItem requestItem)
        {
            var url = $"{_baseUrl}{requestItem.Endpoint}";
            var request = CreateRequest(url, requestItem.Method, requestItem.JsonData);

            yield return request.SendWebRequest();

            // 완료된 요청을 활성 목록에서 제거 (null 체크로 완료된 코루틴들 정리)
            _activeRequests.RemoveAll(c => c == null);

            if (request.result != UnityWebRequest.Result.Success)
            {
                // 재시도 가능한 에러이고 재시도 횟수가 남아있는 경우
                if (ShouldRetry(request.responseCode) && requestItem.RetryCount < requestItem.MaxRetries)
                {
                    requestItem.RetryCount++;
                    Debug.LogWarning($"Request failed, retrying ({requestItem.RetryCount}/{requestItem.MaxRetries})");
                    
                    // 재시도 횟수에 비례하여 대기 시간 증가 (지수적 백오프)
                    yield return new WaitForSeconds(requestItem.RetryDelay * requestItem.RetryCount);
                    _requestQueue.Enqueue(requestItem); // 재시도를 위해 큐에 다시 추가
                }
                else
                {
                    // 재시도 불가능하거나 재시도 횟수 초과 시 에러 콜백 호출
                    requestItem.OnHttpError?.Invoke(request.responseCode, request.error);
                }
            }
            else
            {
                // 성공 시 응답 데이터로 성공 콜백 호출
                requestItem.OnSuccess?.Invoke(request.downloadHandler.text);
            }

            request.Dispose();
        }

        /// <summary>
        /// HTTP 상태 코드에 따라 재시도 가능 여부를 판단
        /// </summary>
        /// <param name="responseCode">HTTP 응답 코드</param>
        /// <returns>재시도 가능 여부</returns>
        private static bool ShouldRetry(long responseCode)
        {
            // 서버 에러(5xx), 요청 타임아웃(408), Too Many Requests(429)의 경우 재시도
            return responseCode is >= 500 or 408 or 429;
        }

        /// <summary>
        /// HTTP 요청 객체를 생성하는 메서드
        /// </summary>
        /// <param name="url">요청할 완전한 URL</param>
        /// <param name="method">HTTP 메서드 enum</param>
        /// <param name="jsonData">요청 본문에 포함할 JSON 데이터</param>
        /// <returns>설정이 완료된 UnityWebRequest 객체</returns>
        private static UnityWebRequest CreateRequest(string url, EMethod method, string jsonData)
        {
            var methodString = method.ToMethodString();
            var request = new UnityWebRequest(url, methodString);

            // POST, PUT, PATCH 요청의 경우 JSON 데이터를 본문에 추가
            if (!string.IsNullOrEmpty(jsonData) && 
                method is EMethod.POST or EMethod.PUT or EMethod.PATCH)
            {
                var bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            }

            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            return request;
        }

        /// <summary>
        /// GameObject 파괴 시 호출되는 Unity 라이프사이클 메서드
        /// - 큐 처리 중단
        /// - 진행 중인 모든 코루틴 정리
        /// </summary>
        protected void OnDestroy()
        {
            _isProcessingQueue = false;
            StopAllCoroutines();
        }
    }
}
