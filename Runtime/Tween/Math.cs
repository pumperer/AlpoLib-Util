using UnityEngine;

namespace alpoLib.Util
{
    public static class ALMath
    {
        public static float SpringLerp(float strength, float deltaTime)
        {
            if (deltaTime > 1f)
                deltaTime = 1f;
            var ms = Mathf.RoundToInt(deltaTime * 1000f);
            deltaTime = 0.001f * strength;
            var cumulative = 0f;
            for (var i = 0; i < ms; ++i)
                cumulative = Mathf.Lerp(cumulative, 1f, deltaTime);
            return cumulative;
        }

        public static Vector3 SpringLerp(Vector3 from, Vector3 to, float strength, float deltaTime)
        {
            return Vector3.Lerp(from, to, SpringLerp(strength, deltaTime));
        }
    }
}