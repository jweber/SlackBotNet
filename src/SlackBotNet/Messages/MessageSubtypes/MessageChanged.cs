namespace SlackBotNet.Messages.MessageSubtypes
{
    [MessageSubType("message_changed")]
    public class MessageChanged : Message
    {
        public Message Message { get; set; }
    }
}