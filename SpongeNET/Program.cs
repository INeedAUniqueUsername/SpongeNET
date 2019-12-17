using DSharpPlus;
using System;
using System.Threading.Tasks;

namespace SpongeNET
{
    class Program
    {
        static DiscordClient discord;

        static void Main(string[] args)
        {
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            discord = new DiscordClient(new DiscordConfiguration {
                Token = "<your token here>",
                TokenType = TokenType.Bot,
                UseInternalLogHandler = true,
                LogLevel = LogLevel.Debug
            });

            discord.MessageCreated += async e => {
                if (e.Message.Content.ToLower().StartsWith("ping"))
                    await e.Message.RespondAsync("pong!");
            };

            await discord.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}
