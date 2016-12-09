using SlackBotNet.Drivers;
using System;
using SlackBotNet.State;
using System.Threading.Tasks;

namespace SlackBotNet.Tests.Infrastructure
{

    internal class TestDriver : IDriver
    {
        private readonly SlackBotState state;

        public TestDriver(SlackBotState state)
        {
            this.state = state;
        }

        public Task<SlackBotState> ConnectAsync(string slackToken, IMessageBus bus)
        {
            return Task.FromResult(this.state);
        }

        public Task DisconnectAsync()
        {
            return Task.CompletedTask;
        }

        public Task SendMessageAsync(string message)
        {
            return Task.CompletedTask;
        }
    }
}
