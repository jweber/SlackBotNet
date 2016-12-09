using SlackBotNet.Messages.Subtypes;

namespace SlackBotNet.Messages
{
    [EventType("presence_change")]
    public class PresenceChange : IRtmMessage
    {
        public string User { get; set; }
        public Presence Presence { get; set; }
    }
}