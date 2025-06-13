using System;

namespace alpoLib.Util
{
    public class DefaultTimeGetter : ITimeGetter
    {
        public DateTimeOffset GetTime()
        {
            return DateTimeOffset.Now;
        }
    }
}