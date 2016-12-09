using System;

namespace SlackBotNet
{
    internal class EventTypeAttribute : Attribute
    {
        public EventTypeAttribute(string messageType)
        {
            this.MessageType = messageType;
        }

        public string MessageType { get; }
    }

    internal class MessageSubTypeAttribute : Attribute
    {
        public MessageSubTypeAttribute(string messageSubType)
        {
            this.MessageSubType = messageSubType;
        }

        public string MessageSubType { get; }
    }
}