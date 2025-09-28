using System;

namespace alpoLib.Util.WebApiClient
{
    /// <summary>
    /// Web API 요청 큐에 저장되는 개별 요청 항목을 나타내는 데이터 클래스
    /// 각 HTTP 요청에 필요한 모든 정보와 콜백 함수들을 포함
    /// </summary>
    public class RequestQueueItem
    {
        /// <summary>
        /// API 엔드포인트 경로 (예: "/api/users/login")
        /// </summary>
        public string Endpoint;
        
        /// <summary>
        /// HTTP 메서드 (GET, POST, PUT, DELETE 등)
        /// </summary>
        public EMethod Method;
        
        /// <summary>
        /// 요청 본문에 포함될 JSON 문자열 데이터
        /// </summary>
        public string JsonData;

        /// <summary>
        /// HTTP 요청 성공 시 호출되는 콜백 함수
        /// 서버로부터 받은 JSON 응답 문자열을 매개변수로 받음
        /// </summary>
        public Action<string> OnSuccess;
        
        /// <summary>
        /// 논리적 에러 발생 시 호출되는 콜백 함수
        /// (JSON 파싱 실패, 데이터 검증 실패 등)
        /// 에러 메시지를 매개변수로 받음
        /// </summary>
        public Action<string> OnLogicError;
        
        /// <summary>
        /// HTTP 에러 발생 시 호출되는 콜백 함수
        /// HTTP 상태 코드와 에러 메시지를 매개변수로 받음
        /// </summary>
        public Action<long, string> OnHttpError;

        /// <summary>
        /// 현재까지 시도한 재시도 횟수 (0부터 시작)
        /// </summary>
        public int RetryCount;
        
        /// <summary>
        /// 이 요청의 최대 재시도 허용 횟수
        /// </summary>
        public int MaxRetries;
        
        /// <summary>
        /// 재시도 간격(초) - 재시도 횟수에 곱해져서 지수적 백오프 구현
        /// </summary>
        public float RetryDelay;
    }
}
