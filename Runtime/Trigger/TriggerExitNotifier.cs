using System;
using UnityEngine;

namespace alpoLib.Util
{
    public class TriggerExitNotifier : MonoBehaviour
    {
        private TriggerObserver _observer;
        
        public void Initialize(TriggerObserver observer)
        {
            _observer = observer;
        }

        private void OnDestroy()
        {
            if (_observer)
            {
                var c = GetComponent<Collider>();
                if (c)
                    _observer.NotifyExitManually(c);
            }
        }

        private void OnDisable()
        {
            if (_observer)
            {
                var c = GetComponent<Collider>();
                if (c)
                    _observer.NotifyExitManually(c);
            }
        }
    }
}