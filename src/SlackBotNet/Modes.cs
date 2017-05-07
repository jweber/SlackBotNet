using System;

namespace SlackBotNet
{
    [Flags]
    public enum Modes
    {
        None = 0,
        StartThread = 1,
        ObserveAllMessages = 2
    }
}