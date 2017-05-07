using System;

namespace SlackBotNet.State
{
    [Flags]
    public enum HubType
    {
        Channel = 1,
        Group = 2,
        DirectMessage = 4,

        All = Channel | Group | DirectMessage
    }
}