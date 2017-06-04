using SlackBotNet.Drivers;
using System;
using SlackBotNet.State;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SlackBotNet.Messages.WebApi;

namespace SlackBotNet.Tests.Infrastructure
{

    internal class TestDriver : IDriver
    {
        private readonly SlackBotState state;

        public TestDriver(SlackBotState state)
        {
            this.state = state;
        }

        public Task<SlackBotState> ConnectAsync(IMessageBus bus, ILogger logger)
        {
            return Task.FromResult(this.state);
        }

        public Task DisconnectAsync()
        {
            return Task.CompletedTask;
        }

        public Task SendMessageAsync(PostMessage message)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
