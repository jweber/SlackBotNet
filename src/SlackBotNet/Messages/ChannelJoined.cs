using System.Security.Cryptography.X509Certificates;
using SlackBotNet.Messages.Subtypes;
using SlackBotNet.State;

namespace SlackBotNet.Messages
{
    [EventType("channel_joined")]
    public class ChannelJoined : IHubJoined
    {
        public HubType HubType => HubType.Channel;
        public Channel Channel { get; set; }
    }
}