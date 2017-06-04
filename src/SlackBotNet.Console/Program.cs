using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using SlackBotNet;

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
        var loggerFactory = new LoggerFactory();
        loggerFactory.AddProvider(new ConsoleLoggerProvider((s, level) => true, true));

        var bot = await SlackBot.InitializeAsync("token", cfg =>
        {
            cfg.LoggerFactory = loggerFactory;
        });
    }
}