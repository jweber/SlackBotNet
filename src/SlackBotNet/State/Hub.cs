using System.Diagnostics;

namespace SlackBotNet.State
{
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class Hub
    {
        public Hub(string id, string name, HubType hubType)
        {
            this.Id = id;
            this.HubType = hubType;

            switch (hubType)
            {
                case HubType.Channel:
                    this.Name = "#" + name;
                    break;
                case HubType.DirectMessage:
                    this.Name = "@" + name;
                    break;
                default:
                    this.Name = name;
                    break;
            }
        }

        public string Id { get; }
        public string Name { get; }
        public HubType HubType { get; }
    }
}