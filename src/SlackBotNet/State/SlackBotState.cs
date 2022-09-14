using System.Collections.Concurrent;
using System.Linq;
using Newtonsoft.Json.Linq;
using System;

namespace SlackBotNet.State
{
    class SlackBotState : IReadOnlyState
    {
        private readonly ConcurrentDictionary<string, User> users;
        private readonly ConcurrentDictionary<string, Hub> hubs;

        private SlackBotState()
        {
            this.users = new ConcurrentDictionary<string, User>();
            this.hubs = new ConcurrentDictionary<string, Hub>();
        }

        public string BotUserId { get; private set; }
        public string BotUsername { get; private set; }

        public void AddUser(string id, string username)
            => this.users[id] = new User(id, username);

        public User GetUser(string id)
            => id != null && this.users.ContainsKey(id) ? this.users[id] : null;

        public User[] GetUsers()
            => this.users.Values.ToArray();

        public void AddHub(string id, string name, HubType hubType)
            => this.hubs[id] = new Hub(id, name, hubType);

        public void RemoveHub(string id)
        {
            if (!this.hubs.ContainsKey(id))
                return;

            this.hubs.TryRemove(id, out Hub hub);
        }

        public Hub GetHubById(string id)
            => this.hubs.ContainsKey(id) ? this.hubs[id] : null;

        public Hub GetHub(string commonName)
            => this.hubs
                .Where(h => h.Value.Name.Equals(commonName, StringComparison.OrdinalIgnoreCase))
                .Select(h => h.Value)
                .FirstOrDefault();

        public Hub[] GetHubs()
            => this.hubs.Values.ToArray();

        internal static SlackBotState Initialize(string botUserId, string botUsername)
        {
            var state = new SlackBotState();
            state.BotUserId = botUserId;
            state.BotUsername = botUsername;

            return state;
        }

        public static SlackBotState InitializeFromRtmConnect(JObject data)
        {
            var state = new SlackBotState();

            state.BotUserId = data["self"]["id"].Value<string>();
            state.BotUsername = data["self"]["name"].Value<string>();

            return state;
        }
    }
}