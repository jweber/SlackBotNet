namespace SlackBotNet.State
{
    public interface IReadOnlyState
    {
        string BotUserId { get; }
        string BotUsername { get; }

        User GetUser(string id);
        User[] GetUsers();

        Hub GetHubById(string id);
        
        /// <summary>
        /// Find a hub by its common name (e.g. @user, #channel)
        /// </summary>
        /// <param name="commonName"></param>
        /// <returns></returns>
        Hub GetHub(string commonName);

        Hub[] GetHubs();
    }
}