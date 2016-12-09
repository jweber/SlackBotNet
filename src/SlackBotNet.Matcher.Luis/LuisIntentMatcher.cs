using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SlackBotNet.Messages;

namespace SlackBotNet.Matcher
{
    internal class LuisIntentMatcher : MessageMatcher
    {
        private readonly string intentName;
        private readonly decimal confidenceThreshold;

        private static readonly HttpClient HttpClient = new HttpClient();

        private const string Url =
            "https://api.projectoxford.ai/luis/v2.0/apps/{0}?subscription-key={1}&verbose=false&q={2}";

        public LuisIntentMatcher(string intentName, decimal confidenceThreshold = 0.9m)
        {
            this.intentName = intentName;
            this.confidenceThreshold = confidenceThreshold;
        }

        public override async Task<Match[]> GetMatches(Message message)
        {
            if (!LuisConfig.Configured)
                return null;

            var requestUrl = string.Format(Url,
                LuisConfig.AppKey,
                LuisConfig.SubscriptionKey,
                message.Text);

            var result = await HttpClient.GetStringAsync(new Uri(requestUrl));
            var dtoResponse = JsonConvert.DeserializeObject<LuisResponse>(result);

            var passesThreshold = dtoResponse.TopScoringIntent.Score >= this.confidenceThreshold;
            var matchesIntent = dtoResponse.TopScoringIntent.Intent.Equals(this.intentName,
                StringComparison.OrdinalIgnoreCase);

            if (passesThreshold && matchesIntent)
            {
                return dtoResponse.Entities.Select(m => new Match(m.Entity, m.Type, m.Score)).ToArray();
            }

            return null;
        }

        public class LuisResponse
        {
            public string Query { get; set; }
            public IntentResponse TopScoringIntent { get; set; }
            public EntityResponse[] Entities { get; set; }

            public class IntentResponse
            {
                public string Intent { get; set; }
                public decimal Score { get; set; }
            }

            //    {
            //      "entity": "membership",
            //      "type": "ServiceName",
            //      "startIndex": 25,
            //      "endIndex": 34,
            //      "score": 0.9798528
            //    }
            public class EntityResponse
            {
                public string Entity { get; set; }
                public string Type { get; set; }
                public int StartIndex { get; set; }
                public int EndIndex { get; set; }
                public decimal Score { get; set; }
            }
        }
    }
}