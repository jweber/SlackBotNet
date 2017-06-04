using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SlackBotNet.Messages;

namespace SlackBotNet.Matcher
{
    internal class LuisIntentMatcher : MessageMatcher
    {
        private static readonly ConcurrentDictionary<(string message, bool spellCheck), Task<(DateTimeOffset added, LuisResponse response)>> LuisResponseCache
            = new ConcurrentDictionary<(string message, bool spellCheck), Task<(DateTimeOffset added, LuisResponse response)>>();

        private readonly string intentName;
        private readonly decimal confidenceThreshold;
        private readonly bool spellCheck;

        private static readonly HttpClient HttpClient = new HttpClient();

        private const string Url =
            "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/{0}?subscription-key={1}&timezoneOffset=0&verbose=false&spellCheck={2}&q={3}";

        public LuisIntentMatcher(string intentName, decimal confidenceThreshold = 0.9m, bool spellCheck = true)
        {
            this.intentName = intentName;
            this.confidenceThreshold = confidenceThreshold;
            this.spellCheck = spellCheck;
        }

        private LuisResponse GetLuisResponse(string message)
        {
            async Task<(DateTimeOffset, LuisResponse)> MakeLuisRequest()
            {
                var requestUrl = string.Format(Url,
                    LuisConfig.AppKey,
                    LuisConfig.SubscriptionKey,
                    this.spellCheck.ToString().ToLowerInvariant(),
                    message);

                var response = await HttpClient.GetAsync(new Uri(requestUrl));
                
                this.Logger.LogInformation($"Request to {requestUrl} resulted in status code of {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"LUIS call failed. Response code was {response.StatusCode}: {response.ReasonPhrase}");

                var result = await response.Content.ReadAsStringAsync();

                var dto = JsonConvert.DeserializeObject<LuisResponse>(result);

                if (dto.TopScoringIntent == null || dto.Entities == null)
                    throw new Exception($"Response from LUIS did not deserialize as expected. Response was: {result}");

                return (DateTimeOffset.UtcNow, dto);
            }

            // trim cache
            if (LuisResponseCache.Count >= LuisConfig.CacheSize)
            {   
                this.Logger.LogInformation($"LUIS reponse cache exceeded {LuisConfig.CacheSize}. Trimming the cache");
                
                var expiredKeys = LuisResponseCache
                    .Where(m => !m.Key.message.Equals(message, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(m => m.Value.Result.added)
                    .Take(LuisResponseCache.Count - LuisConfig.CacheSize + 1)
                    .Select(m => m.Key);

                foreach (var key in expiredKeys)
                    LuisResponseCache.TryRemove(key, out var _);
            }

            return LuisResponseCache.GetOrAdd((message, this.spellCheck), _ => MakeLuisRequest()).Result.response;
        }

        public override Task<Match[]> GetMatches(Message message)
        {          
            if (!LuisConfig.Configured || string.IsNullOrWhiteSpace(message.Text))
                return NoMatch;

            var response = this.GetLuisResponse(message.Text);
            
            this.Logger.LogDebug($"Top scoring intent was '{response.TopScoringIntent.Intent}' (score: {response.TopScoringIntent.Score}) for message '{message.Text}'");
            
            var passesThreshold = response.TopScoringIntent.Score >= this.confidenceThreshold;
            var matchesIntent = response.TopScoringIntent.Intent.Equals(this.intentName, StringComparison.OrdinalIgnoreCase);

            this.Logger.LogDebug($"Message: {message.Text}; Passes threshold of {this.confidenceThreshold}? {passesThreshold}; Matches intent of {this.intentName}? {matchesIntent}");
            
            if (passesThreshold && matchesIntent)
            {
                return Task.FromResult(
                    response.Entities
                        .Select(m => new Match(m.Entity, m.Type, m.Score))
                        .ToArray());
            }

            return NoMatch;
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