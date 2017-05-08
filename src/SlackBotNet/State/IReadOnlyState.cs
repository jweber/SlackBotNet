namespace SlackBotNet.State
{
    public interface IReadOnlyState
    {
        /// <summary>
        /// The given Slack Id for the bot.
        /// </summary>
        string BotUserId { get; }

        /// <summary>
        /// The username that the bot is running under.
        /// </summary>
        string BotUsername { get; }

        /// <summary>
        /// Returns a <see cref="User"/> by its Slack Id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        User GetUser(string id);

        /// <summary>
        /// Returns all <see cref="User"/> records that the bot
        /// is aware of.
        /// </summary>
        /// <returns></returns>
        User[] GetUsers();

        /// <summary>
        /// Returns a <see cref="Hub"/> by its Slack Id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Hub GetHubById(string id);
        
        /// <summary>
        /// Find a hub by its common name (e.g. @user, #channel)
        /// </summary>
        /// <param name="commonName"></param>
        /// <returns></returns>
        Hub GetHub(string commonName);

        /// <summary>
        /// Returns all <see cref="Hub"/> records that the Bot is a member of.
        /// </summary>
        /// <returns></returns>
        Hub[] GetHubs();
    }
}