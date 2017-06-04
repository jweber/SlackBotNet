using System;
using Microsoft.Extensions.Logging;

namespace SlackBotNet.Tests.Infrastructure
{

    public class TestConfig : ISlackBotConfig
    {
        public WhenHandlerMatchMode WhenHandlerMatchMode { get; set; }
        public ILoggerFactory LoggerFactory { get; set; }
    }
}
