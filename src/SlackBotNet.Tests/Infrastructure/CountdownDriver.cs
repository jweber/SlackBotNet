using SlackBotNet.Drivers;
using SlackBotNet.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SlackBotNet.Messages;
using SlackBotNet.Messages.WebApi;

namespace SlackBotNet.Tests.Infrastructure
{
    class CountdownDriver : IDriver
    {
        private readonly List<(DateTimeOffset time, IMessage message)> timings
            = new List<(DateTimeOffset, IMessage)>();

        private readonly CountdownEvent countdown;

        public CountdownDriver(CountdownEvent countdown)
        {
            this.countdown = countdown;
        }

        public IReadOnlyList<DateTimeOffset> RecordedTimings
            => this.timings.Select(m => m.time).ToList();

        public IReadOnlyList<IMessage> RecordedMessages
            => this.timings.Select(m => m.message).ToList();

        public Task<SlackBotState> ConnectAsync(IMessageBus bus, ILogger logger)
            => Task.FromResult(SlackBotState.Initialize("1", "testbot"));

        public Task DisconnectAsync() => Task.CompletedTask;

        public Task SendMessageAsync(IMessage message, ILogger logger)
        {
            this.timings.Add((DateTimeOffset.UtcNow, message));
            this.countdown.Signal();
            return Task.CompletedTask;
        }

        public Task UploadFileAsync(File file, ILogger logger) => Task.CompletedTask;

        public void Dispose() => this.countdown.Dispose();
    }
}
