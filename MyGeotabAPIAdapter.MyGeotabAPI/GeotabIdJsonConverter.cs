using Geotab.Checkmate.ObjectModel;
using Newtonsoft.Json;
using System;

namespace MyGeotabAPIAdapter.MyGeotabAPI
{
    /// <summary>
    /// A class that uses a <see cref="JsonConverter"/> to convert Geotab <see cref="Id"/>s to JSON.
    /// </summary>
    public class GeotabIdJsonConverter : JsonConverter<Id>
    {
        public override bool CanRead => false;

        public override Id ReadJson(JsonReader reader, Type objectType, Id existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, Id value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            string actualValue = value.ToString();
            writer.WriteValue(actualValue);
        }
    }
}
