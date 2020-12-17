using DSharpPlus;
using System;
using System.Threading.Tasks;
using Common;
using static Common.Common;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System.Linq;
using System.Collections.Generic;

namespace Crewmate {
    public class Program {
        public DiscordClient client;
        public Crewmate c;
        static void Main(string[] args) {
            Program p = new Program();
            p.MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        async Task MainAsync(string[] args) {
            client = new DiscordClient(new DiscordConfiguration {
                Token = "NzY5NTAxNjg3NjgxMTIyMzM0.X5P8Sg.jP_j0kE1PpV5z4Tk4k6mCKINCnM",
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
                    await Task.Run(() => c.Handle(e));
                } catch (Exception ex) {
                    PeriodicSave();
                    //e.Channel.SendMessageAsync("Oops");
                    throw ex;
                }
            };
            c = Load<Crewmate>() ?? new Crewmate();

            c.OnLoad(this);

            PeriodicSave();
            await client.ConnectAsync();
            await Task.Delay(-1);
        }
        public async void PeriodicSave() {
            Save(c);
            Console.WriteLine("Saved");
            await Task.Run(async () => {
                await Task.Delay(1000 * 60 * 60);
                PeriodicSave();
            });
        }
    }
    public class Crewmate {
        DiscordClient client;
        Dictionary<ulong, Dictionary<ulong, DateTime>> serverMap;
        public Crewmate() {
            serverMap = new Dictionary<ulong, Dictionary<ulong, DateTime>>();
        } 
        public void OnLoad(Program p) {
            this.client = p.client;
        }
        public async Task Handle(MessageCreateEventArgs m) {
            var s = m.Message.Content.ToLower();
            if (m.MentionedUsers.Select(m => m.Id).Contains(client.CurrentUser.Id) || s == "among us") {
                var listing = GetListing();
                if (listing.Any()) {
                    var info = $"There are currently {listing.Count} people waiting to play Among Us";
                    foreach ((var userId, var time) in listing) {
                        var username = (await client.GetUserAsync(userId)).Username;
                        info += $"\n{username} ({(int)(DateTime.Now - time + TimeSpan.FromMinutes(30)).TotalMinutes} minutes ago)";
                    }
                    info += "\nWhen two people have called `!sus`, I will ping the Among Us role.";

                    m.Channel.SendMessageAsync(info);
                } else {
                    m.Channel.SendMessageAsync($"There is nobody waiting to play Among Us. When twp people have called `!sus`, I will ping the Among Us role.");
                }
            } else if(s == "!sus") {
                var listing = GetListing();

                m.Message.DeleteAsync();
                if (listing.ContainsKey(m.Author.Id)) {
                    m.Channel.SendMessageAsync($"{m.Author.Mention} is no longer waiting to play Among Us.");
                    listing.Remove(m.Author.Id);
                } else {
                    listing[m.Author.Id] = DateTime.Now + TimeSpan.FromMinutes(30);
                    if (listing.Count > 0) {
                        listing.Clear();

                        string str = "**EMERGENCY MEETING** @Among Us";
                        foreach ((var userId, var time) in listing) {
                            str += $"\n{(await client.GetUserAsync(userId)).Mention}";
                        }

                        m.Channel.SendMessageAsync(str);
                    } else {
                        m.Channel.SendMessageAsync($"{m.Author.Mention} is now waiting to play Among Us. This vote will automatically expire in 30 minutes.");
                    }
                }
            }
            Dictionary<ulong, DateTime> GetListing() {
                if (!serverMap.TryGetValue(m.Guild.Id, out var listing)) {
                    listing = serverMap[m.Guild.Id] = new Dictionary<ulong, DateTime>();
                }
                UpdateListing(listing);
                void UpdateListing(Dictionary<ulong, DateTime> d) {
                    var now = DateTime.Now;
                    foreach ((var userId, var expireTime) in d) {
                        if (now > expireTime) {
                            d.Remove(userId);
                        }
                    }
                }
                return listing;
            }
            
        }
    }
}
