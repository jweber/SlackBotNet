using System;
using Newtonsoft.Json;

namespace SlackBotNet.Messages.Subtypes
{
    public class Channel
    {
        public string Id { get; set; }
        public string Name { get; set; }

        [JsonProperty("is_channel")]
        public bool IsChannel { get; set; }

        public DateTimeOffset Created { get; set; }

        public string Creator { get; set; }

        public bool IsArchived { get; set; }
        public bool IsGeneral { get; set; }
        public bool IsMember { get; set; }

        [JsonProperty("last_read")]
        public string LastRead { get; set; }

        public DateTimeOffset LastReadTimestamp => this.LastRead.FromChannelTimestamp();

        public Message Latest { get; set; }

        [JsonProperty("unread_count")]
        public int UnreadCount { get; set; }

        [JsonProperty("unread_count_display")]
        public int UnreadCountDisplay { get; set; }

        public string[] Members { get; set; }

        public ChannelMessage Topic { get; set; }
        public ChannelMessage Purpose { get; set; }
    }
}