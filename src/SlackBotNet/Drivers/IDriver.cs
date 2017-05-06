using System;
using SlackBotNet.State;
using System.Threading.Tasks;
using SlackBotNet.Messages.WebApi;

namespace SlackBotNet.Drivers
{
    internal interface IDriver : IDisposable
    {
        Task<SlackBotState> ConnectAsync(IMessageBus bus);
        Task DisconnectAsync();
        Task SendMessageAsync(PostMessage message);
    }
}
