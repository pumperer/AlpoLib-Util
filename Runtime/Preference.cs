using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace alpoLib.Util
{
    public class Preference
    {
        private Dictionary<string, object> datas = new();

        private byte[] ObfuscateBytes(byte[] data)
        {
            var key = data.Length + 5332;
            for (var i = 0; i < data.Length; i++)
            {
                data[i] ^= (byte)key;
                key *= i;
            }
            return data;
        }

        private string Encrypt(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            var obfuscatedData = ObfuscateBytes(bytes);
            return Convert.ToBase64String(obfuscatedData);
        }

        private string Decrypt(string data)
        {
            var bytes = Convert.FromBase64String(data);
            var decrypted = ObfuscateBytes(bytes);
            return Encoding.UTF8.GetString(decrypted);
        }

        public string Serialize()
        {
            var jsonString = JsonConvert.SerializeObject(datas);
            return Encrypt(jsonString);
        }

        public Dictionary<string, object> Deserialize(string data)
        {
            if (string.IsNullOrEmpty(data))
                return new Dictionary<string, object>();
            
            data = Decrypt(data);
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }
            
            return new Dictionary<string, object>();
        }

        public void AddData(string key, object value)
        {
            datas[key] = value;
        }

        public void SetDataDictionary(Dictionary<string, object> dic)
        {
            datas = dic;
        }
        
        public Dictionary<string, object> GetDataDictionary() => datas;

        public void Save(string mainKey)
        {
            var serialized = Serialize();
            var encryptedKey = Encrypt($"_{mainKey}_");
            PlayerPrefs.SetString(encryptedKey, serialized);
        }

        public void Load(string mainKey)
        {
            var encryptedKey = Encrypt($"_{mainKey}_");
            var serialized = PlayerPrefs.GetString(encryptedKey, string.Empty);
            if (string.IsNullOrEmpty(serialized))
            {
                datas.Clear();
                return;
            }

            var deserialized = Deserialize(serialized);
            if (deserialized != null)
                datas = deserialized;
            else
                datas.Clear();
        }
    }
}