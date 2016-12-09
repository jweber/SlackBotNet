using SlackBotNet.Messages.Subtypes;
using SlackBotNet.State;

namespace SlackBotNet.Messages
{
    public interface IRtmMessage
    {}

    public interface IHubJoined : IRtmMessage
    {
        HubType HubType { get; }
        Channel Channel { get; set; }
    }

    public interface IHubLeft : IRtmMessage
    {
        HubType HubType { get; }
        string Channel { get; set; }
    }
}