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

        public static SlackBotState InitializeFromRtmStart(JObject data)
        {
            var state = new SlackBotState();

            state.BotUserId = data["self"]["id"].Value<string>();
            state.BotUsername = data["self"]["name"].Value<string>();

            foreach (var user in data["users"])
            {
                if (user["deleted"].Value<bool>())
                    continue;
                
                state.AddUser(user["id"].Value<string>(), user["name"].Value<string>());
            }

            foreach (var channel in data["channels"])
            {
                bool isArchived = channel["is_archived"].Value<bool>();
                bool isMember = channel["is_member"].Value<bool>();

                if (isArchived || !isMember)
                    continue;

                state.AddHub(
                    channel["id"].Value<string>(),
                    channel["name"].Value<string>(),
                    HubType.Channel);
            }

            foreach (var group in data["groups"])
            {
                bool isArchived = group["is_archived"].Value<bool>();
                bool isMember = group["is_member"].Value<bool>();

                if (isArchived || !isMember)
                    continue;

                state.AddHub(
                    group["id"].Value<string>(),
                    group["name"].Value<string>(),
                    HubType.Group);
            }

            foreach (var im in data["ims"])
            {
                string userId = im["user"].Value<string>();

                state.AddHub(
                    im["id"].Value<string>(),
                    state.GetUser(userId)?.Username ?? userId,
                    HubType.DirectMessage
                );
            }

            return state;
        }
    }
}