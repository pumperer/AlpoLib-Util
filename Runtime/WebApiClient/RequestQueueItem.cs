using System;

namespace alpoLib.Util.WebApiClient
{
    public class RequestQueueItem
    {
        public string Endpoint;
        public string Method;
        public string JsonData;
        public Action<string> OnSuccess;
        public Action<string> OnLogicError;
        public Action<long, string> OnHttpError;
        public int RetryCount;
        public int MaxRetries;
        public float RetryDelay;
    }
}
