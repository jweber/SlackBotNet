using SlackBotNet.Messages.Subtypes;
using SlackBotNet.State;

namespace SlackBotNet.Messages
{
    [EventType("im_created")]
    public class ImCreated : IHubJoined
    {
        public HubType HubType => HubType.DirectMessage;

        public string User { get; set; }
        public Channel Channel { get; set; }
    }
}