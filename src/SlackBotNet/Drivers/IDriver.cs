using System;
using SlackBotNet.State;
using System.Threading.Tasks;

namespace SlackBotNet.Drivers
{
    internal interface IDriver : IDisposable
    {
        Task<SlackBotState> ConnectAsync(string slackToken, IMessageBus bus);
        Task DisconnectAsync();
        Task SendMessageAsync(string message);
    }
}
