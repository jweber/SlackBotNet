using System;
using SlackBotNet.State;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SlackBotNet.Messages;
using SlackBotNet.Messages.WebApi;

namespace SlackBotNet.Drivers
{
    internal interface IDriver : IDisposable
    {
        Task<SlackBotState> ConnectAsync(IMessageBus bus, ILogger logger);
        Task DisconnectAsync();
        Task SendMessageAsync(IMessage message, ILogger logger);
    }
}
