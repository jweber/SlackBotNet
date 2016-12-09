using SlackBotNet.State;
using System.Threading.Tasks;

namespace SlackBotNet.Drivers
{
    internal interface IDriver
    {
        Task<SlackBotState> ConnectAsync(string slackToken, IMessageBus bus);
        Task DisconnectAsync();
        Task SendMessageAsync(string message);
    }
}
