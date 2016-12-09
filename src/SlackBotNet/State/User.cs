using System.Diagnostics;

namespace SlackBotNet.State
{
    [DebuggerDisplay("{" + nameof(Username) + "}")]
    public class User
    {
        public User(string id, string username)
        {
            Id = id;
            Username = username;
        }

        public string Id { get; }
        public string Username { get; }
    }
}