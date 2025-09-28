namespace alpoLib.Util.WebApiClient
{
    public abstract class BaseApiService<T> where T : WebApiClient
    {
        protected readonly T APIClient;

        public BaseApiService(T client)
        {
            APIClient = client;
        }
    }
}
