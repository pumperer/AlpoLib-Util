using System;

namespace alpoLib.Util.WebApiClient
{
    /// <summary>
    /// Web API 서비스 클래스들의 기본 추상 클래스
    /// - 특정 WebApiClient 타입에 대한 강타입 지원
    /// - 상속받는 서비스 클래스에서 공통된 API 클라이언트 접근 제공
    /// - 제네릭 제약을 통해 타입 안전성 보장
    /// </summary>
    /// <typeparam name="T">사용할 WebApiClient의 구체적인 타입 (WebApiClient를 상속받은 클래스)</typeparam>
    public abstract class BaseApiService<T> where T : WebApiClient
    {
        /// <summary>
        /// API 요청을 처리하는 WebApiClient 인스턴스
        /// - readonly로 선언하여 생성 후 변경 불가능
        /// - 상속받는 클래스에서 모든 HTTP 요청에 사용
        /// </summary>
        protected readonly T APIClient;

        /// <summary>
        /// BaseApiService의 기본 생성자
        /// - WebApiClient 인스턴스를 주입받아 내부 필드에 저장
        /// - 의존성 주입 패턴을 통해 클라이언트와 서비스 간의 결합도를 낮춤
        /// </summary>
        /// <param name="client">API 요청 처리에 사용할 WebApiClient 인스턴스</param>
        /// <exception cref="ArgumentNullException">client가 null인 경우 발생</exception>
        protected BaseApiService(T client)
        {
            APIClient = client ?? throw new ArgumentNullException(nameof(client));
        }
    }
}
