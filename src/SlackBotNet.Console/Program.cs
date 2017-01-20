using System;
using System.Threading.Tasks;
using SlackBotNet;
using SlackBotNet.State;
using static SlackBotNet.MatchFactory;

class Program
{
    static void Main(string[] args)
    {
        MainAsync().Wait();

        Console.WriteLine("Press <enter> to exit");
        Console.ReadLine();
    }

    static async Task MainAsync()
    {
        var bot = await SlackBot.InitializeAsync("token");
    }
}