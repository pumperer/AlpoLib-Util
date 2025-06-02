using System;
using System.Collections.Generic;
using System.ComponentModel;
using alpoLib.Core.Foundation;
using Newtonsoft.Json;

namespace alpoLib.Util
{
    public class DefaultJsonSerializer : JsonConverter
    {
        private List<Type> allowedTypes = new()
        {
            typeof(CustomBoolean),
        };
        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
                return;
            
            switch (value)
            {
                case CustomBoolean customBoolean:
                    writer.WriteValue(customBoolean ? "1" : "0");
                    break;
                
                default:
                    writer.WriteValue(value.ToString());
                    break;
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (objectType.IsArray)
            {
                
            }
            else if (reader.Value != null)
            {
                if (objectType == typeof(CustomBoolean))
                {
                    var v = reader.Value.ToString();
                    var c = TypeDescriptor.GetConverter(objectType);
                    var canConvertFrom = c.CanConvertFrom(typeof(string));
                    if (canConvertFrom)
                    {
                        return (CustomBoolean)c.ConvertFrom(v);
                    }
                }
            }

            return null;
        }

        public override bool CanConvert(Type objectType)
        {
            return allowedTypes.Contains(objectType);
        }
    }
}