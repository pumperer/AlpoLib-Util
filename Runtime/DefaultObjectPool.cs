using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace alpoLib.Util
{
    public interface IObjectPool<T> where T : Component
    {
        T Get();
        void Release(T obj);
    }
    
    public class DefaultObjectPool<T> : IObjectPool<T>, IDisposable where T : Component
    {
        private readonly ObjectPool<T> _objectPool;
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        
        private readonly List<T> _usingList = new();
        
        public DefaultObjectPool(GameObject prefab, Transform parent = null)
        {
            _prefab = prefab;
            _objectPool = new ObjectPool<T>(CreateAction, ActionOnGet, ActionOnRelease, ActionOnDestroy);
            _parent = parent;
        }
        
        public void Preload(int count)
        {
            using var _ = ListPool<T>.Get(out var list);
            for (var i = 0; i < count; i++)
            {
                var o = _objectPool?.Get();
                list.Add(o);
            }

            if (list.Count == 0)
                return;
            
            foreach (var o in list.Where(o => o))
            {
                _objectPool?.Release(o);
            }
        }

        public T Get()
        {
            var obj = _objectPool?.Get();
            _usingList.Add(obj);
            return obj;
        }
        
        public void Release(T obj)
        {
            if (!obj)
                return;
            
            _objectPool?.Release(obj);
            _usingList.Remove(obj);
        }
        
        private void ActionOnDestroy(T obj)
        {
            if (obj && obj.gameObject)
                Object.Destroy(obj.gameObject);
        }

        private void ActionOnRelease(T obj)
        {
            if (obj)
                obj.gameObject.SetActive(false);
        }

        private void ActionOnGet(T obj)
        {
            if (obj)
                obj.gameObject.SetActive(true);
        }

        private T CreateAction()
        {
            return Object.Instantiate(_prefab, _parent).GetComponent<T>();
        }

        public void Dispose()
        {
            foreach (var obj in _usingList)
                _objectPool?.Release(obj);
            _usingList.Clear();
            _objectPool?.Dispose();
        }
    }
}