using System;
using System.Collections.Generic;
using UnityEngine;

namespace alpoLib.Util
{
    [RequireComponent(typeof(Collider))]
    public class TriggerObserver : MonoBehaviour
    {
        private readonly HashSet<Collider> _overlapping = new();
        
        public event Action<Collider> OnTriggerEnterEvent;
        public event Action<Collider> OnTriggerExitEvent;
        
        private void OnTriggerEnter(Collider other)
        {
            if (_overlapping.Add(other))
            {
                OnTriggerEnterEvent?.Invoke(other);
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (_overlapping.Remove(other))
            {
                OnTriggerExitEvent?.Invoke(other);
            }
        }

        private void ForceExitAll()
        {
            foreach (var c in _overlapping)
            {
                OnTriggerExitEvent?.Invoke(c);
            }
            _overlapping.Clear();
        }
        
        public void NotifyExitManually(Collider other)
        {
            if (_overlapping.Remove(other))
            {
                OnTriggerExitEvent?.Invoke(other);
            }
        }
        
        private void OnDisable()
        {
            ForceExitAll();
        }

        private void OnDestroy()
        {
            ForceExitAll();
        }
    }
}