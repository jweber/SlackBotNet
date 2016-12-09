using System;
using Newtonsoft.Json;

namespace SlackBotNet.Messages
{
    [EventType("message")]
    public class Message : IRtmMessage
    {
        public string Channel { get; set; }
        public string User { get; set; }
        public string Text { get; set; }

        [JsonProperty("ts")]
        public string ChannelTimestamp { get; set; }

        public DateTimeOffset Timestamp 
            => this.ChannelTimestamp.FromChannelTimestamp();

        public bool Hidden { get; set; }

        public string SubType { get; set; }

        [JsonProperty("reply_to")]
        public string ReplyTo { get; set; }
    }
}