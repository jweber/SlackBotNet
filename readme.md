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

    bot.When(Matches.Text("hello"), async conv =>
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

    bot.When("knock knock", Modes.StartThread, async conv =>
    {
        await conv.PostMessage("Who's there?");

        var who = await conv.WaitForReply();
        await conv.PostMessage($"{who.Text} who?");

        var punchline = await conv.WaitForReply();
        await conv.PostMessage($"{punchline.Text}, lol :laughing:");
    });


Multiple MessageMatchers can be combined by using the `Or` or `And` matcher or the double pipes/double ampersand operators:

    Matches.Text("hello").Or(Matches.Regex("^world$"))
    Matches.Text("text").And(Matches.Message(m => m.RawThreadTimestamp == null))

Equivalent:

    Matches.Text("hello") || Matches.Regex("^world$")
    Matches.Text("text") && Matches.Message(m => m.RawThreadTimestamp == null)

When multiple MessageMatchers are used, they short-circuit, meaning if the first one matches the message then the second MessageMatcher will not be tried.

## Hubs

_Note_: Private channels are considered to be Groups. If your bot needs to listen on a private channel, make sure that the HubType.Group is included.

Bots can be configured to listen for messages on Channels they are joined to, groups they are a part of or when they receive direct messages. They will default to listening on all hub types.

    bot.When(
        Matches.Text("hello"), 
        HubType.DirectMessage | HubType.Channel, 
        async conv =>
        {
            ...
        });

## Observing all messages in a channel

When the bot is observing a channel or group, by default it will only listen for messages that contain the name of the bot in the message text. If you want the bot to listen to all messages in a channel, the `Modes.ObserveAllMessages` flag can be set.

    bot.When(
        Matches.Text("hello"),
        Modes.ObserveAllMessages,
        async conv =>
        {
            ...
        });

## Replying with a thread

By default the bot will send its reply to the channel. It can also start a thread by using the `Modes.StartThread` flag.

    bot.When(
        Matches.Text("hello"),
        Modes.StartThread,
        async conv =>
        {
            ...
        });

If the `StartThread` mode is not set and a Slack user replies to a bot message with a thread, the bot will automatically swith to the thread mode.

## Error Handling

Callbacks can be registered with the bot that will be fired in the event of an unhandled exception occuring in either the MessageMatcher(s) or the conversation delegate:

Example:

    using static SlackBotNet.MatchFactory;

    bot
        .When(Matches.Text("hello"),
            async conv =>
        {
                // exception thrown
        })
        .OnException((msg, ex) =>
        {
                // log exception
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

### Text

Matches a message that contains the given text. 

The `string` type is implicitly converted to the text matcher.

Example:

    bot.When("hello", async conv =>
    {
        ...
    })


### Regex

Matches a message based on a regular expression. Capture groups in the regular expression are present in the Conversation.

### Message

Takes a predicate that can inspect the raw instance of the `SlackBotNet.Messages.Message` object.

### LuisIntent

Hooks up to the [Language Understanding Intelligent Service (luis)](https://www.luis.ai) for processing natural language. Up to `LuisConfig.CacheSize` results will be maintained in local cache (defaults to 100).

Installation:

    nuget> Install-Package SlackBotNet.Matchers.Luis

Configuration:

    LuisConfig.SubscriptionKey = "...";
    LuisConfig.AppKey = "...";

    LuisConfig.CacheSize = 100;

Using:

    Matches.LuisIntent(intentName: "Intent", confidenceThreshold: 0.9m)

# Examples

## Tell a Joke

Tells a knock-knock joke. The conversation can be ended by the user typing _nevermind_ at any point.

    bot.When(
        Matches.Text("tell me a joke"),
        Modes.StartThread,
        async conv =>
        {
            await conv.PostMessage("okay! knock knock");

            async Task Continue()
            {
                await conv.WaitForReply("who's there?");

                await conv.PostMessage("broken pencil");

                await conv.WaitForReply(
                    "broken pencil who?", 
                    async _ => await conv.PostMessage("nope, try again"));

                await conv.PostMessage("nevermind, it's pointless");

                await Task.Delay(TimeSpan.FromSeconds(2));
                await conv.PostMessage(":joy:");
            }

            async Task Quit()
            {
                await conv.WaitForReply("nevermind");
                await conv.PostMessage("okay then :expressionless:");
            }

            await Task.WhenAny(Continue(), Quit());
        });

# License

Apache 2.0
