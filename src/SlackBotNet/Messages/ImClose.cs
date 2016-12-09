using SlackBotNet.State;

namespace SlackBotNet.Messages
{
    [EventType("im_close")]
    public class ImClose : IHubLeft
    {
        public HubType HubType => HubType.DirectMessage;

        public string User { get; set; }
        public string Channel { get; set; }
    }
}