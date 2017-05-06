using System;
using Newtonsoft.Json;
using SlackBotNet.Infrastructure;
using SlackBotNet.State;

namespace SlackBotNet.Messages
{
    [EventType("channel_left")]
    public class ChannelLeft : IHubLeft
    {
        public HubType HubType => HubType.Channel;

        public string Channel { get; set; }

        [JsonProperty("event_ts")]
        public string ChanelTimestamp { get; set; }

        public DateTimeOffset EventTimestamp => this.ChanelTimestamp.FromRawTimestamp();
    }
}