using System;
using UnityEngine;

namespace alpoLib.Util
{
    public sealed class ApplicationPauseListener : SingletonMonoBehaviour<ApplicationPauseListener>
    {
        public static bool InQuitProcess { get; private set; }
        public static event Action OnPauseEvent;
        public static event Action OnResumeEvent;
        public static event Action OnQuitEvent;
        public static event Action OnSaveEvent;

        public void ResetAll()
        {
            if (OnPauseEvent != null)
            {
                foreach (var d in OnPauseEvent.GetInvocationList())
                    OnPauseEvent -= (Action)d;
            }

            if (OnResumeEvent != null)
            {
                foreach (var d in OnResumeEvent.GetInvocationList())
                    OnResumeEvent -= (Action)d;
            }
            
            if (OnSaveEvent != null)
            {
                foreach (var d in OnSaveEvent.GetInvocationList())
                    OnSaveEvent -= (Action)d;
            }

            InQuitProcess = false;
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            Debug.Log($"<color=yellow>Application Pause : {pauseStatus}</color>");

            if (pauseStatus)
            {
                OnPauseEvent?.Invoke();
                OnSaveEvent?.Invoke();
            }
            else
                OnResumeEvent?.Invoke();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            Debug.Log($"<color=yellow>Application Focus : {hasFocus}</color>");
#if UNITY_EDITOR
            if (Application.runInBackground)
            {
                if (!hasFocus)
                {
                    OnPauseEvent?.Invoke();
                    OnSaveEvent?.Invoke();
                }
                else
                    OnResumeEvent?.Invoke();
            }
#endif
        }

        private void OnApplicationQuit()
        {
            Debug.Log($"<color=yellow>Application Quit</color>");
            InQuitProcess = true;
            OnQuitEvent?.Invoke();
            OnSaveEvent?.Invoke();
        }
    }
}