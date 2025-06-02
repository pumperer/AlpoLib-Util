using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace alpoLib.Util
{
    public interface IDontDestroyGameObject
    {
        bool Preserve { get; }
        GameObject GameObject { get; }
    }
    
    public class DontDestroyGameObject : MonoBehaviour, IDontDestroyGameObject
    {
        [SerializeField] private bool preserved = true;
        public bool Preserve => preserved;
        public GameObject GameObject => gameObject;
        
        private static List<IDontDestroyGameObject> allDontDestroyObjects = new();
        
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            allDontDestroyObjects.Add(this);
        }

        public static void AddDontDestroyGameObject(MonoBehaviour go)
        {
            if (go is not IDontDestroyGameObject ddgo)
                return;
            
            allDontDestroyObjects.Add(ddgo);
        }

        public static void DestroyAllDontDestroyGameObject()
        {
            var deleteList = new List<IDontDestroyGameObject>();
            foreach (var go in allDontDestroyObjects)
            {
                if (go is { Preserve: false })
                {
                    Destroy(go.GameObject);
                    deleteList.Add(go);
                }
            }

            allDontDestroyObjects.RemoveAll(o => deleteList.Contains(o));
        }
    }
}