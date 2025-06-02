using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace alpoLib.Util
{
    /// <summary>
    /// 휘발성 설정용입니다.
    /// </summary>
    public abstract class GameState
    {
    }

    /// <summary>
    /// 비휘발성 설정용입니다.
    /// </summary>
    public abstract class PersistentGameState : GameState
    {
    }
    
    public class GameStateManager : Singleton<GameStateManager>
    {
        private Dictionary<Type, GameState> states = new();
        private Dictionary<string, object> serializationBase = new();

        private string userKey;

        public GameStateManager()
        {
            Load();
        }

        public void SetUserKey(long id)
        {
            var ulId = (ulong)id;
            ulId = (ulId >> 27) | (ulId << 37);
            var bytes = BitConverter.GetBytes(ulId);

            var s = new List<byte>();
            s.Add(88);
            s.Add(72);
            s.AddRange(bytes);
            s.Add(53);
            s.Add(32);

            var h = new SHA1CryptoServiceProvider();
            var hb = h.ComputeHash(s.ToArray());
            userKey = Convert.ToBase64String(hb);
        }

        private GameState GetState(Type type, bool createNewIfNull)
        {
            if (states.TryGetValue(type, out var state))
                return state;

            if (serializationBase.TryGetValue(type.Name, out var deserialized))
            {
                var gs = JsonConvert.DeserializeObject((string)deserialized, type) as GameState;
                states.Add(type, gs);
                return gs;
            }

            if (!createNewIfNull)
                return null;
            
            var newState = Activator.CreateInstance(type) as GameState;
            states.Add(type, newState);
            return newState;
        }

        public T GetState<T>(bool createNewIfNull = true) where T : GameState
        {
            return GetState(typeof(T), createNewIfNull) as T;
        }

        public void ClearState<T>() where T : GameState
        {
            var type = typeof(T);
            states.Remove(type);
            serializationBase.Remove(type.Name);
        }

        public void SetState(GameState state)
        {
            if (!states.ContainsKey(state.GetType()))
                states.Add(state.GetType(), state);
            else
                states[state.GetType()] = state;
        }

        public void ClearAll()
        {
            states.Clear();
            serializationBase.Clear();
        }

        public void Save()
        {
            foreach (var (_, state) in states)
            {
                if (state is not PersistentGameState)
                    continue;

                var stateName = state.GetType().Name;
                serializationBase[stateName] = JsonConvert.SerializeObject(state);
            }

            var pref = new Preference();
            pref.SetDataDictionary(serializationBase);
            pref.Save($"State_{userKey}_");
        }
        
        private void Load()
        {
            var pref = new Preference();
            pref.Load($"State_{userKey}_");
            serializationBase = pref.GetDataDictionary();
        }
    }
}