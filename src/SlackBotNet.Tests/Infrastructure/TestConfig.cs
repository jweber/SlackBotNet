using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SlackBotNet.Messages;
using SlackBotNet.Messages.WebApi;

namespace SlackBotNet.Tests.Infrastructure
{
    public class TestConfig : ISlackBotConfig
    {
        public WhenHandlerMatchMode WhenHandlerMatchMode { get; set; }
        public ILoggerFactory LoggerFactory { get; set; }
        public Action<IThrottleQueue<IMessage>, IMessage, ILogger, Exception> OnSendMessageFailure { get; set; }
    }
}
