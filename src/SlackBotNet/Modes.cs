using System;

namespace SlackBotNet
{
    [Flags]
    public enum Modes
    {
        None = 0,

        /// <summary>
        /// The bot will create a thread for its first response. 
        /// It will only listen for replies within that thread.
        /// </summary>
        /// <remarks>
        /// If a user replies to one of the bot's messages with a thread,
        /// the conversation will switch to this threaded mode.
        /// </remarks>
        StartThread = 1,

        /// <summary>
        /// The bot will listen for all messages on <see cref="State.HubType.Channel"/>
        /// and <see cref="State.HubType.Group"/> channels even if the messsage does
        /// not indicate the bot name.
        /// </summary>
        ObserveAllMessages = 2
    }
}