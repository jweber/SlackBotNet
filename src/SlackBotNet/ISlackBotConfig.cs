using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SlackBotNet
{
    public interface ISlackBotConfig
    {
        WhenHandlerMatchMode WhenHandlerMatchMode { get; set; }
        ILoggerFactory LoggerFactory { get; set; }
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
        }

        public WhenHandlerMatchMode WhenHandlerMatchMode { get; set; }
        public ILoggerFactory LoggerFactory { get; set; }
    }
}
