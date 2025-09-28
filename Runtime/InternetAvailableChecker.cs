using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace alpoLib.Util
{
    /// <summary>
    /// InternetReachabilityVerifier 참고하여 작성됨.
    /// </summary>
    public class InternetAvailableChecker : MonoBehaviour
    {
        /// <summary>
        /// 인터넷 연결 감지 방식을 정의합니다.
        /// </summary>
        public enum EDetectionMethod
        {
            /// <summary>
            /// 플랫폼별 기본값 사용 (Android: gstatic, iOS: Apple, 기타: Google)
            /// </summary>
            DefaultByPlatform = 0,

            /// <summary>
            /// Google의 HTTP 204 응답을 확인
            /// </summary>
            Google204Https,

            /// <summary>
            /// Apple의 "Success" 텍스트 응답을 확인
            /// </summary>
            AppleHttps,

            /// <summary>
            /// Google의 gstatic HTTP 204 응답을 확인
            /// </summary>
            GStaticHttps,

            /// <summary>
            /// 사용자 정의 URL 및 검증 방식
            /// </summary>
            Custom,
        }

        /// <summary>
        /// 인터넷 연결 상태를 나타냅니다.
        /// </summary>
        public enum EStatus
        {
            /// <summary>
            /// 초기 상태 (아직 체크하지 않음)
            /// </summary>
            None = 0,

            /// <summary>
            /// 네트워크 연결 없음 / 인터넷 접속 불가
            /// </summary>
            Offline,

            /// <summary>
            /// 네트워크 연결 있음, 인터넷 접속 확인 중
            /// </summary>
            Pending,

            /// <summary>
            /// 인터넷 접속 확인 중 오류 발생, 곧 재시도 예정
            /// </summary>
            Error,

            /// <summary>
            /// 캡티브 포털 감지됨 (예: 와이파이 로그인 화면), 곧 재시도 예정
            /// </summary>
            ResponseMismatch,

            /// <summary>
            /// 인터넷 접속이 확인되고 정상 작동 중
            /// </summary>
            NetAvailable
        }

        /// <summary>
        /// InternetAvailableChecker 초기화에 필요한 설정 정보를 담는 클래스입니다.
        /// </summary>
        public class InitializeContext
        {
            /// <summary>
            /// 인터넷 연결 감지 방식
            /// </summary>
            public EDetectionMethod DetectionMethod = EDetectionMethod.DefaultByPlatform;

            /// <summary>
            /// 감지 방식용 커스텀 URL
            /// </summary>
            public string CustomMethodURL = "";

            /// <summary>
            /// 커스텀 URL로부터 예상되는 응답 데이터
            /// </summary>
            public string CustomMethodExpectedData = "OK";

            /// <summary>
            /// 기본 체크 주기 (초)
            /// </summary>
            public float DefaultCheckPeriod = 5.0f;

            /// <summary>
            /// 오류 후 재시도 지연 시간 (초)
            /// </summary>
            public float ErrorRetryDelay = 15.0f;

            /// <summary>
            /// 캡티브 포털 감지 후 재시도 지연 시간 (초)
            /// </summary>
            public float MismatchRetryDelay = 5.0f;

            /// <summary>
            /// 커스텀 응답 검증 델리게이트
            /// </summary>
            public CustomMethodCheckerDelegate CustomMethodChecker;
        }

        /// <summary>
        /// 상태 변경 이벤트 델리게이트
        /// </summary>
        public delegate void StatusChangedDelegate(EStatus prevStatus, EStatus newStatus);

        /// <summary>
        /// 커스텀 방식 검증 델리게이트
        /// </summary>
        public delegate bool CustomMethodCheckerDelegate(UnityWebRequest request, string expectedData);

        /// <summary>
        /// 현재 감지 방식
        /// </summary>
        private EDetectionMethod _detectionMethod = EDetectionMethod.DefaultByPlatform;

        /// <summary>
        /// 커스텀 감지 방식에 사용할 URL
        /// </summary>
        private string _customMethodURL = "";

        /// <summary>
        /// 커스텀 감지 방식에서 예상되는 응답 데이터
        /// </summary>
        private string _customMethodExpectedData = "OK";

        /// <summary>
        /// 기본 체크 주기 (초)
        /// </summary>
        private float _defaultCheckPeriod = 5.0f;

        /// <summary>
        /// 오류 후 재시도 지연 시간 (초)
        /// </summary>
        private float _errorRetryDelay = 15.0f;

        /// <summary>
        /// 캡티브 포털 감지 후 재시도 지연 시간 (초)
        /// </summary>
        private float _mismatchRetryDelay = 5.0f;

        /// <summary>
        /// 커스텀 응답 검증 델리게이트
        /// </summary>
        private CustomMethodCheckerDelegate customMethodChecker;

        /// <summary>
        /// 상태 변경 이벤트 델리게이트
        /// </summary>
        private StatusChangedDelegate _onStatusChanged;

        /// <summary>
        /// 현재 인터넷 연결 상태
        /// </summary>
        private EStatus _status = EStatus.None;

        /// <summary>
        /// 마지막 발생한 오류 메시지
        /// </summary>
        private string _lastError = "";

        /// <summary>
        /// 인터넷 연결이 끊어진 시점의 시간
        /// </summary>
        private float _noInternetStartTime;

        /// <summary>
        /// 체킹 코루틴 실행 여부
        /// </summary>
        private bool _isRunning;

        /// <summary>
        /// 싱글톤 인스턴스
        /// </summary>
        private static InternetAvailableChecker _instance;

        /// <summary>
        /// 초기화 컨텍스트 캐시
        /// </summary>
        private static InitializeContext _initializeContext;

        /// <summary>
        /// 현재 인터넷 연결 상태를 반환합니다.
        /// </summary>
        public static EStatus CurrentStatus => _instance?._status ?? EStatus.None;

        /// <summary>
        /// 마지막 발생한 오류 메시지를 반환합니다.
        /// </summary>
        public static string LastError => _instance?._lastError ?? "";

        /// <summary>
        /// 인터넷 연결 상태 변경 시 발생하는 이벤트입니다.
        /// 구독 시 현재 상태로 즉시 한 번 호출됩니다.
        /// </summary>
        public static event StatusChangedDelegate OnStatusChanged
        {
            add
            {
                if (_instance == null || value == null)
                    return;
                _instance._onStatusChanged += value;
                value(_instance._status, _instance._status);
            }
            remove
            {
                if (_instance != null)
                    _instance._onStatusChanged -= value;
            }
        }

        /// <summary>
        /// 인터넷 연결이 끊어진 시간을 초 단위로 반환합니다.
        /// NetVerified 상태일 때는 0을 반환합니다.
        /// </summary>
        public static float TimeWithoutInternet
        {
            get
            {
                if (_instance == null)
                    return 0;
                if (_instance._status == EStatus.NetAvailable)
                    return 0;
                return Time.realtimeSinceStartup - _instance._noInternetStartTime;
            }
        }

        /// <summary>
        /// InternetAvailableChecker를 초기화합니다. 인스턴스가 없으면 새로 생성합니다.
        /// </summary>
        /// <param name="initContext">초기화 컨텍스트</param>
        public static void Initialize(InitializeContext initContext)
        {
            _initializeContext = initContext;

            // 인스턴스가 없으면 새로 생성
            if (_instance == null)
            {
                var go = new GameObject("InternetAvailableChecker");
                _instance = go.AddComponent<InternetAvailableChecker>();
            }

            _instance.ApplyInitializeContext(initContext);
        }

        /// <summary>
        /// 기본 설정으로 InternetAvailableChecker를 초기화합니다. 인스턴스가 없으면 새로 생성합니다.
        /// </summary>
        public static void InitializeWithDefaults()
        {
            Initialize(new InitializeContext());
        }

        private void ApplyInitializeContext(InitializeContext context)
        {
            _detectionMethod = context.DetectionMethod;
            _customMethodURL = context.CustomMethodURL;
            _customMethodExpectedData = context.CustomMethodExpectedData;
            _defaultCheckPeriod = context.DefaultCheckPeriod;
            _errorRetryDelay = context.ErrorRetryDelay;
            _mismatchRetryDelay = context.MismatchRetryDelay;
            customMethodChecker = context.CustomMethodChecker;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // 초기화 컨텍스트가 있다면 적용
            if (_initializeContext != null)
            {
                ApplyInitializeContext(_initializeContext);
            }
        }

        private void Start()
        {
            SetupDetectionMethod();
            StartChecking();
        }

        private void OnEnable()
        {
            if (_instance == this)
                StartChecking();
        }

        private void OnDisable()
        {
            StopChecking();
        }

        private void OnDestroy()
        {
            StopChecking();
            _instance = null;
        }

        /// <summary>
        /// 인터넷 연결 상태 체크를 시작합니다.
        /// 이미 실행 중인 경우에는 무시됩니다.
        /// </summary>
        public static void StartChecking()
        {
            if (_instance == null)
                return;

            if (_instance._isRunning)
                return;

            _instance._isRunning = true;
            _instance.StartCoroutine(_instance.CheckingCoroutine());
        }

        /// <summary>
        /// 인터넷 연결 상태 체크를 중지합니다.
        /// 모든 관련 코루틴을 정지시킵니다.
        /// </summary>
        public static void StopChecking()
        {
            if (_instance == null)
                return;

            _instance._isRunning = false;
            _instance.StopAllCoroutines();
        }

        /// <summary>
        /// 인터넷 연결 상태를 강제로 재검사합니다.
        /// 상태를 Pending으로 변경하여 즉시 검증을 시도합니다.
        /// </summary>
        public static void ForceRecheck()
        {
            _instance?.SetStatus(EStatus.Pending);
        }

        /// <summary>
        /// 인터넷 연결이 확인될 때까지 대기하는 코루틴입니다.
        /// NetAvailable 상태가 아닌 경우 강제 재검사를 수행한 후 대기합니다.
        /// </summary>
        /// <returns>NetAvailable 상태가 될 때까지 대기하는 코루틴</returns>
        public static IEnumerator WaitForNetAvailable()
        {
            yield return _instance?.Co_WaitForNetAvailable();
        }

        private IEnumerator Co_WaitForNetAvailable()
        {
            if (_status != EStatus.NetAvailable)
                ForceRecheck();

            while (_status != EStatus.NetAvailable)
                yield return null;
        }

        private void SetupDetectionMethod()
        {
            if (_detectionMethod == EDetectionMethod.DefaultByPlatform)
            {
#if UNITY_ANDROID
            _detectionMethod = EDetectionMethod.ConnectivityCheckGStaticHttps;
#elif UNITY_IOS
            _detectionMethod = EDetectionMethod.AppleHttps;
#else
                _detectionMethod = EDetectionMethod.Google204Https;
#endif
            }

            if (_detectionMethod == EDetectionMethod.Custom && string.IsNullOrEmpty(_customMethodURL))
            {
                Debug.LogError("InternetAvailableChecker: 커스텀 방식이 선택되었지만 URL이 비어있습니다!");
                enabled = false;
            }
        }

        private void SetStatus(EStatus newStatus)
        {
            var previousStatus = _status;
            _status = newStatus;

            if (previousStatus == EStatus.NetAvailable && _status != EStatus.NetAvailable)
                _noInternetStartTime = Time.realtimeSinceStartup;

            _onStatusChanged?.Invoke(previousStatus, newStatus);
        }

        private IEnumerator CheckingCoroutine()
        {
            var previousReachability = Application.internetReachability;
            UpdateStatusByReachability(previousReachability);
            _noInternetStartTime = Time.realtimeSinceStartup;

            while (_isRunning)
            {
                switch (_status)
                {
                    // 지연 시간과 함께 상태 전환 처리
                    case EStatus.Error:
                    {
                        // 오류 상태에서는 일정 시간 대기 후 재검증 시도
                        yield return Co_WaitForRealTimeSeconds(_errorRetryDelay);
                        if (_status == EStatus.Error) // 상태가 변경되지 않았는지 확인
                            SetStatus(EStatus.Pending);
                        break;
                    }
                    case EStatus.ResponseMismatch:
                    {
                        // 캡티브 포털 감지 상태에서는 일정 시간 대기 후 재검증 시도
                        yield return Co_WaitForRealTimeSeconds(_mismatchRetryDelay);
                        if (_status == EStatus.ResponseMismatch) // 상태가 변경되지 않았는지 확인
                            SetStatus(EStatus.Pending);
                        break;
                    }
                    case EStatus.None:
                    case EStatus.Offline:
                    case EStatus.Pending:
                    case EStatus.NetAvailable:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                // Unity의 네트워크 도달성 변화 확인
                var currentReachability = Application.internetReachability;
                if (previousReachability != currentReachability)
                {
                    UpdateStatusByReachability(currentReachability);
                    previousReachability = currentReachability;
                }

                // 대기 중인 경우 검증 수행
                if (_status == EStatus.Pending)
                    yield return StartCoroutine(VerifyInternetConnection());

                // 다음 체크까지 대기
                yield return Co_WaitForRealTimeSeconds(_defaultCheckPeriod);
            }

            yield break;

            void UpdateStatusByReachability(NetworkReachability reachability)
            {
                SetStatus(reachability != NetworkReachability.NotReachable
                    ? EStatus.Pending
                    : EStatus.Offline);
            }
        }

        private IEnumerator VerifyInternetConnection()
        {
            var url = GetDetectionURL();

            using var request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                _lastError = request.error;
                SetStatus(EStatus.Error);
            }
            else
            {
                var isSuccess = CheckResponse(request);
                SetStatus(isSuccess ? EStatus.NetAvailable : EStatus.ResponseMismatch);
            }
        }

        private string GetDetectionURL()
        {
            var url = _detectionMethod switch
            {
                EDetectionMethod.Google204Https => "https://clients3.google.com/generate_204",
                EDetectionMethod.AppleHttps => "https://www.apple.com/library/test/success.html",
                EDetectionMethod.GStaticHttps => "https://connectivitycheck.gstatic.com/generate_204",
                EDetectionMethod.Custom => _customMethodURL,
                _ => "https://clients3.google.com/generate_204"
            };

            // 신뢰성을 위한 캐시 버스터 추가
            if (url.Contains("?"))
                url += "&";
            else
                url += "?";

            url += "t=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            return url;
        }

        private bool CheckResponse(UnityWebRequest request)
        {
            return _detectionMethod switch
            {
                EDetectionMethod.Google204Https or EDetectionMethod.GStaticHttps
                    => request.responseCode == 204,

                EDetectionMethod.AppleHttps => CheckAppleResponse(request),

                EDetectionMethod.Custom => CheckCustomResponse(request),

                _ => request.responseCode == 204
            };
        }

        private bool CheckAppleResponse(UnityWebRequest request)
        {
            if (request.responseCode != 200) return false;

            var responseText = request.downloadHandler.text.ToLower();
            return responseText.Contains("<body>success</body>") || responseText.Contains("<title>success</title>");
        }

        private bool CheckCustomResponse(UnityWebRequest request)
        {
            if (customMethodChecker != null)
                return customMethodChecker(request, _customMethodExpectedData);

            if (string.IsNullOrEmpty(_customMethodExpectedData))
                return request.downloadHandler.data?.Length == 0;

            return request.downloadHandler.text?.StartsWith(_customMethodExpectedData) ?? false;
        }

        private IEnumerator Co_WaitForRealTimeSeconds(float seconds)
        {
            var startTime = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - startTime < seconds)
                yield return null;
        }
    }
}