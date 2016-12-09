using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using SlackBotNet.Infrastructure;
using SlackBotNet.Messages;

namespace SlackBotNet
{
    internal static class Serialization
    {
        private static readonly ConcurrentDictionary<string, Type> MessageTypes = new ConcurrentDictionary<string, Type>();
        private static readonly ConcurrentDictionary<string, Type> MessageSubTypes = new ConcurrentDictionary<string, Type>();

        static Serialization()
        {
            var eventTypeMap = from t in typeof(Hello).GetTypeInfo().Assembly.GetTypes()
                let ti = t.GetTypeInfo()
                let attr = ti.GetCustomAttribute<EventTypeAttribute>()
                where attr != null
                      && ti.IsClass
                select new { attr.MessageType, Type = t };

            foreach (var e in eventTypeMap)
                MessageTypes.TryAdd(e.MessageType, e.Type);


            var messageSubTypeMap = from t in typeof(Hello).GetTypeInfo().Assembly.GetTypes()
                let ti = t.GetTypeInfo()
                let attr = ti.GetCustomAttribute<MessageSubTypeAttribute>()
                where attr != null
                      && ti.IsClass
                select new { attr.MessageSubType, Type = t };

            foreach (var e in messageSubTypeMap)
                MessageSubTypes.TryAdd(e.MessageSubType, e.Type);
        }

        public static object Deserialize(string message)
        {
            var messageType = GetMessageType(message);
            if (messageType == null)
                return null;

            return JsonConvert.DeserializeObject(message, messageType, new EpochSecondsDateTimeConverter());
        }

        private static Type GetMessageType(string message)
        {
            string slackEventType = GetSlackEventType(message);
            if (string.IsNullOrEmpty(slackEventType))
                return null;

            if (!MessageTypes.ContainsKey(slackEventType))
                return null;


            if (slackEventType.Equals("message", StringComparison.OrdinalIgnoreCase))
            {
                var messageSubType = GetMessageSubType(message);

                if (messageSubType == null)
                    return typeof(Message);

                if (!MessageSubTypes.ContainsKey(messageSubType))
                    return typeof(Message);

                return MessageSubTypes[messageSubType];
            }

            return MessageTypes[slackEventType];
        }

        private static string GetSlackEventType(string json)
        {
            var reader = new JsonTextReader(new StringReader(json));

            string currentProperty = string.Empty;
            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                        currentProperty = reader.Value.ToString();

                    if (reader.TokenType == JsonToken.String && currentProperty == "type")
                        return reader.Value.ToString();
                }
            }

            return null;
        }

        private static string GetMessageSubType(string json)
        {
            var reader = new JsonTextReader(new StringReader(json));

            string currentProperty = string.Empty;
            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                        currentProperty = reader.Value.ToString();

                    if (reader.TokenType == JsonToken.String && currentProperty == "subtype")
                        return reader.Value.ToString();
                }
            }

            return null;
        }
    }
}