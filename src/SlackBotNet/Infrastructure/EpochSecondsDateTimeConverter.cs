using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SlackBotNet.Infrastructure
{
    internal class EpochSecondsDateTimeConverter : DateTimeConverterBase
    {
        private static readonly DateTimeOffset Epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {}

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
                return null;

            return Epoch.AddSeconds((long) reader.Value);
        }
    }
}