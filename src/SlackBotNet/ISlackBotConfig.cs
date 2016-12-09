using System;

namespace SlackBotNet
{
    public interface ISlackBotConfig
    {
        WhenHandlerMatchMode WhenHandlerMatchMode { get; set; }
        Action<string> TraceHandler { get; set; }
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
            this.TraceHandler = _ => { };
        }

        public WhenHandlerMatchMode WhenHandlerMatchMode { get; set; }
        public Action<string> TraceHandler { get; set; }
    }
}
