using SlackBotNet.State;

namespace SlackBotNet.Messages
{
    [EventType("group_left")]
    public class GroupLeft : IHubLeft
    {
        public HubType HubType => HubType.Group;

        public string Channel { get; set; }
    }
}