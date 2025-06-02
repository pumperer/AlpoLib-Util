using System;
using UnityEngine;

namespace alpoLib.Util
{
    public interface ITimeGetter
    {
        DateTimeOffset GetTime();
    }
}