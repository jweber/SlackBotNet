using System;

namespace SlackBotNet.Tests.Infrastructure
{

    public class TestConfig : ISlackBotConfig
    {
        public Action<string> TraceHandler { get; set; }

        public WhenHandlerMatchMode WhenHandlerMatchMode { get; set; }
    }
}
