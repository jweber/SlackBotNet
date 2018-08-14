using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SlackBotNet.Messages;
using SlackBotNet.Messages.WebApi;

namespace SlackBotNet
{
    public interface ISlackBotConfig
    {
        WhenHandlerMatchMode WhenHandlerMatchMode { get; set; }
        ILoggerFactory LoggerFactory { get; set; }
        Action<IThrottleQueue<IMessage>, IMessage, ILogger, Exception> OnSendMessageFailure { get; set; }
    }

    public enum WhenHandlerMatchMode
    {
        FirstMatch,
        BestMatch,
        AllMatches
    }

    internal class DefaultSlackBotConfig : ISlackBotConfig
    {
        public DefaultSlackBotConfig()
        {
            this.WhenHandlerMatchMode = WhenHandlerMatchMode.FirstMatch;

            this.LoggerFactory = new LoggerFactory();
            this.LoggerFactory.AddProvider(NullLoggerProvider.Instance);

            this.OnSendMessageFailure = (queue, msg, logger, ex) => { };
        }

        public WhenHandlerMatchMode WhenHandlerMatchMode { get; set; }
        public ILoggerFactory LoggerFactory { get; set; }
        public Action<IThrottleQueue<IMessage>, IMessage, ILogger, Exception> OnSendMessageFailure { get; set; }
    }
}
