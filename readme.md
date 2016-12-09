# SlackBotNet

.NET Standard compatible client for the [Real Time Messaging API](https://api.slack.com/rtm).

NOTE: This library is not currently full-featured and may have unexpected issues. Please open issues when they are discovered. Pull requests are greatly appreciated.

## Getting Started

Installation:

    nuget> Install-Package SlackBotNet

Initialize a new instance of the bot:

    var bot = await SlackBot.InitializeAsync([slack authentication token]);

Listen for messages sent to the bot:

    using static SlackBotNet.MatchFactory;

    bot.When(Matches.TextContaining("hello"), async conv =>
    {
        await conv.ReplyAsync($"Hi {conv.From.Username}!");
    });

Or publish simple messages to a Slack channel/group/DM:

    var hub = bot.State.GetHub("@username");
    await bot.SendAsync(hub, "Hello!");


## Matching

Use MessageMatchers when you want your bot to listen for various message content.

Example:
    
    using static SlackBotNet.MatchFactory;

    bot.When(
        Matches.TextContaining("knock knock"),
        async conv =>
        {
            var who = await conv.ReplyAsync("Who's there?");
            var punchline = await conv.ReplyAsync($"{who.Text} who?");
            await conv.ReplyAsync($"{punchline.Text}, lol :laughing:");
        });


Multiple MessageMatchers can be combined by using the `Or` matcher or the double pipes:

    Matches.TextContaining("hello").Or(Matches.Regex("^world$"))

Equivalent:

    Matches.TextContaining("hello") || Matches.Regex("^world$")

When multiple MessageMatchers are used, they short-circuit, meaning if the first one matches the message then the second MessageMatcher will not be tried.

## Hubs

Bots can be configured to listen for messages on Channels they are joined to, groups they are a part of or when they receive direct messages. They will default to listening on all hub types.

    bot.When(
        Matches.TextContaining("hello"), 
        HubType.DirectMessage || HubType.Channel, 
        async conv =>
        {
            ...
        });

## Multiple Handlers

If there are multiple `.When(...)` setups configured for the bot, the bot will respect the `WhenHandlerMatchMode` as configured when incoming messages can be handled by more than one `.When(...)`.

    var bot = await SlackBot.InitializeAsync([slack authentication token], cfg =>
    {
        cfg.WhenHandlerMatchMode = WhenHandlerMatchMode.FirstMatch;
    });

### WhenHandlerMatchMode.FirstMatch

Only the first `When` handler that matches the message will be used. The position of the `When` handler is determined by the order it is registered with the bot.

### WhenHandlerMatchMode.BestMatch

`MessageMatchers` can assign a score when they match a message. In this mode, the `When` handler that scores the highest will be used.

### WhenHandlerMatchMode.AllMatches

All `When` handlers that match the incoming message will be fired.

# Available Message Matchers

### TextContaining

Matches a message that contains the given text.

### Regex

Matches a message based on a regular expression. Capture groups in the regular expression are present in the Conversation.

### Message

Takes a predicate that can inspect the raw instance of the `SlackBotNet.Messages.Message` object.

### LuisIntent

Hooks up to the [Language Understanding Intelligent Service (luis)](https://www.luis.ai) for processing natural language.

Installation:

    nuget> Install-Package SlackBotNet.Matchers.Luis

Configuration:

    LuisConfig.SubscriptionKey = "...";
    LuisConfig.AppKey = "...";

Using:

    Matches.LuisIntent(intentName: "Intent", confidenceThreshold: 0.9m)

# License

Apache 2.0
