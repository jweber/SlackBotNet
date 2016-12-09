using Newtonsoft.Json;

namespace SlackBotNet.Messages.Subtypes
{
    public class ChannelMessage
    {
        public string Value { get; set; }
        public string Creator { get; set; }

        [JsonProperty("last_set")]
        public string LastSet { get; set; }
    }
}