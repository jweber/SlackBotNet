using Newtonsoft.Json;

namespace SlackBotNet.Messages
{
    internal class Ping
    {
        [JsonConstructor]
        private Ping()
        { }
        
        public Ping(long id)
        {
            this.Id = id;
        }
        
        public long Id { get; private set; }

        public string Type => "ping";
    }

    [EventType("pong")]
    internal class Pong
    {
        [JsonProperty("reply_to")]
        public long ReplyTo { get; private set; }
    }
}