using SlackBotNet.Messages.Subtypes;
using SlackBotNet.State;

namespace SlackBotNet.Messages
{
    [EventType("group_joined")]
    public class GroupJoined : IHubJoined
    {
        public HubType HubType => HubType.Group;

        public Channel Channel { get; set; }
    }
}