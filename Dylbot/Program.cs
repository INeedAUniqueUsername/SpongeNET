using DSharpPlus;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Common;
using static Common.Common;
using System.Linq;
using System.Collections.Generic;

namespace Dylbot {
    public class Program {
        public DiscordClient discord;
        public Dylan dylan;
        static void Main(string[] args) {
            Program p = new Program();
            p.MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        async Task MainAsync(string[] args) {
            discord = new DiscordClient(new DiscordConfiguration {
                Token = Tokens.DYLBOT,
                TokenType = TokenType.Bot,
            });
            discord.MessageCreated += async (client, e) => {
                try {
                    dylan.Handle(e);
                } catch (Exception ex) {
                    PeriodicSave();
                    //e.Channel.SendMessageAsync("Oops");
                    throw ex;
                }
            };
            discord.VoiceStateUpdated += async (client, e) => dylan.Handle(e);
            dylan = Load<Dylan>() ?? new Dylan();

            dylan.OnLoad(this);
            
            PeriodicSave();
            await discord.ConnectAsync();
            await Task.Delay(-1);
        }
        public async void PeriodicSave() {
            Save(dylan);
            Console.WriteLine("Saved?");
            await Task.Run(async () => {
                await Task.Delay(1000 * 60 * 60);
                PeriodicSave();
            });
        }
    }
    public class Dylan {
        private DiscordClient discord;
        HashSet<Bet> bets = new HashSet<Bet>();
        public void OnLoad(Program program) {
            this.discord = program.discord;
        }
        public void DeleteOld() {
            var now = DateTime.Now;
            bets.RemoveWhere(b => b.done || (now - b.timeRequested).TotalMinutes > 60);
        }
        public async void Handle(MessageCreateEventArgs e) {
            const ulong dyl = 141388797861429248;
            if (e.Message.Content.StartsWith("!dyldead")) {
                DeleteOld();
                var targetId = e.MentionedUsers.FirstOrDefault()?.Id ?? dyl;
                var b = new Bet() {
                    playerId = e.Author.Id,
                    targetId = targetId,
                    timeRequested = DateTime.Now
                };
                bets.Add(b);
                var m = await e.Guild.GetMemberAsync(targetId);

                e.Channel.SendMessageAsync($"Okay, I'll see if {m.Nickname} gets sent to AFK within the next hour.");

                Task.Run(async () => {
                    
                    await Task.Delay(1000 * 60 * 60);
                    if(!b.done) {
                        await e.Channel.SendMessageAsync($"{m.Nickname} was not sent to AFK.");
                    }
                });
            }
        }
        public async void Handle(VoiceStateUpdateEventArgs e) {
            if (e.After.Channel != null
                && e.After.Channel.Id == 593301338650050561) {
                DeleteOld();

                var c = await discord.GetChannelAsync(795583425334738954);

                var bet = bets.Where(b => b.targetId == e.User.Id);
                if(bet.Any()) {
                    var names = string.Join(", ", bet.Select(async b => {
                        b.done = true;
                        var u = await discord.GetUserAsync(b.playerId);
                        var m = await e.Guild.GetMemberAsync(b.playerId);
                        return m.Nickname;
                    }).Select(t => t.Result));
                    var m = await e.Guild.GetMemberAsync(e.User.Id);
                    await c.SendMessageAsync($"{m.Nickname} got sent to AFK, just as {names} predicted");
                }
            }
        }
    }
    class Bet {
        public ulong playerId;
        public ulong targetId;
        public DateTime timeRequested;
        public bool done;
    }
}
