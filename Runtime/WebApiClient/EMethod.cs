namespace alpoLib.Util.WebApiClient
{
    /// <summary>
    /// HTTP 메서드를 정의하는 열거형
    /// 표준 HTTP 메서드들을 타입 안전하게 사용할 수 있도록 제공
    /// </summary>
    public enum EMethod
    {
        /// <summary>
        /// GET 메서드 - 리소스 조회
        /// </summary>
        GET,
        
        /// <summary>
        /// POST 메서드 - 리소스 생성
        /// </summary>
        POST,
        
        /// <summary>
        /// PUT 메서드 - 리소스 전체 수정
        /// </summary>
        PUT,
        
        /// <summary>
        /// PATCH 메서드 - 리소스 부분 수정
        /// </summary>
        PATCH,
        
        /// <summary>
        /// DELETE 메서드 - 리소스 삭제
        /// </summary>
        DELETE,
        
        /// <summary>
        /// HEAD 메서드 - 헤더 정보만 조회
        /// </summary>
        HEAD,
        
        /// <summary>
        /// OPTIONS 메서드 - 서버가 지원하는 메서드 조회
        /// </summary>
        OPTIONS
    }

    /// <summary>
    /// EMethod 열거형에 대한 확장 메서드를 제공하는 정적 클래스
    /// </summary>
    public static class EMethodExtensions
    {
        /// <summary>
        /// EMethod 열거형을 HTTP 메서드 문자열로 변환
        /// </summary>
        /// <param name="method">변환할 EMethod 값</param>
        /// <returns>HTTP 메서드 문자열</returns>
        public static string ToMethodString(this EMethod method)
        {
            return method.ToString();
        }
    }
}
