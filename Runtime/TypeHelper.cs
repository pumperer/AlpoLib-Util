using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace alpoLib.Util
{
    public static class TypeHelper
    {
        private static Assembly _gameAssembly;
        private static Type[] _typesInGameAssembly;
        private static Dictionary<string, Dictionary<string, Type>> _typesInGameAssemblyWithNs;
            
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void Init()
        {
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            _gameAssembly = assemblies.FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
            _typesInGameAssembly = _gameAssembly?.GetTypes();
            if (_typesInGameAssembly == null)
                return;
            
            _typesInGameAssemblyWithNs = new Dictionary<string, Dictionary<string, Type>>();
            foreach (var type in _typesInGameAssembly)
            {
                if (type.IsNested)
                    continue;
                
                var key = type.Name;
                var ns = type.Namespace ?? "";
                if (!_typesInGameAssemblyWithNs.ContainsKey(ns))
                    _typesInGameAssemblyWithNs.Add(ns, new Dictionary<string, Type>());
                var dic = _typesInGameAssemblyWithNs[ns];
                if (dic.TryGetValue(key, out var t))
                    Debug.LogError("같은 타입이 있으면 안되는데...");
                else
                    dic.Add(key, type);
            }
        }

        public static Type GetType(string typeName, string namespaceName = "")
        {
            if (!string.IsNullOrEmpty(namespaceName))
            {
                if(_typesInGameAssemblyWithNs.TryGetValue(namespaceName, out var typesInGameAssembly))
                    if (typesInGameAssembly.TryGetValue(typeName, out var t))
                        return t;
            }
            
            foreach (var (ns, dic) in _typesInGameAssemblyWithNs)
            {
                if (dic.TryGetValue(typeName, out var t))
                    return t;
            }

            return null;
        }
    }
}