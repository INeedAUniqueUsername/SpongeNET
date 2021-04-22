using Common;
using static Common.Common;
using DSharpPlus;
using Newtonsoft.Json;
using Quipcord;
using System;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace Quipcord {
    public static class SClient {
        public static async Task React(this MessageCreateEventArgs e, DiscordClient client, string name) {
            e.Message.CreateReactionAsync(DiscordEmoji.FromName(client, name));
        }
        public static async Task Unreact(this MessageCreateEventArgs e, DiscordClient client, string name) {
            e.Message.DeleteReactionAsync(DiscordEmoji.FromName(client, name), client.CurrentUser);
        }
    }
    public class Program {
        public DiscordClient client;
        public Acrolash acro;
        public Markovlash markov;
        public Paintlash paint;
        public Quiplash quip;
        public Quotelash quote;
        static void Main(string[] args) {
            Program p = new Program();
            p.MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        async Task MainAsync(string[] args) {
            client = new DiscordClient(new DiscordConfiguration {
                Token = Tokens.QUIPCORD,
                TokenType = TokenType.Bot,
                UseInternalLogHandler = true,
                LogLevel = LogLevel.Debug
            });
            client.MessageCreated += async e => {
                try {
                    switch (e.Message.Content) {
                        case "!save":
                            PeriodicSave();
                            break;
                        case "!exit":
                            PeriodicSave();
                            Environment.Exit(0);
                            break;
                    }
                    await acro.Handle(e);
                    await markov.Handle(e);
                    await paint.Handle(e);
                    await quip.Handle(e);
                    await quote.Handle(e);
                } catch(Exception ex) {
                    PeriodicSave();
                    //e.Channel.SendMessageAsync("Oops");
                    throw ex;
                }
            };
            acro = Load<Acrolash>() ?? new Acrolash();
            markov = Load<Markovlash>() ?? new Markovlash();
            paint = Load<Paintlash>() ?? new Paintlash();
            quip = Load<Quiplash>() ?? new Quiplash();
            quote = Load<Quotelash>() ?? new Quotelash();

            acro.OnLoad(this);
            markov.OnLoad(this);
            paint.OnLoad(this);
            quip.OnLoad(this);
            quote.OnLoad(this);
            
            PeriodicSave();
            await client.ConnectAsync();
            await Task.Delay(-1);
        }
        public async void PeriodicSave() {
            Save(acro);
            Save(markov);
            Save(paint);
            Save(quip);
            Save(quote);
            Console.WriteLine("Saved");
            await Task.Run(async () => {
                await Task.Delay(1000 * 60 * 60);
                PeriodicSave();
            });
        }
    }
}
