using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using SpongeLake;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quipcord {
    public class Quote {
        public HashSet<DiscordAttachment> attachments;
        public ulong author;
        public ulong channel;
        public ulong server;
        public ulong id;
        //public HashSet<DiscordEmbed> embeds;
        public HashSet<ulong> mentioned;
        public string message;
        public HashSet<DiscordReaction> reactions;
        public DateTimeOffset timestamp;
        public Quote() { }
        public Quote(DiscordMessage m) {
            attachments = m.Attachments.ToHashSet();
            author = m.Author.Id;
            channel = m.ChannelId;
            //embeds = m.Embeds.ToHashSet();
            id = m.Id;
            mentioned = m.MentionedUsers.Where(u => u != null).Select(u => u.Id).ToHashSet();
            message = m.Content;
            reactions = m.Reactions.ToHashSet();
            server = m.Channel.GuildId;
            timestamp = m.Timestamp;
        }
    }
    public struct Context {
        public ulong authorId;
        public ulong serverId;
        public ulong channelId;
        public static implicit operator string(Context c) => $"{c.authorId}_{c.serverId}_{c.channelId}";
    }
    public class Quotelash {

        private DiscordClient client;
        public Dictionary<ulong, Quote> history;
        public Dictionary<string, HashSet<ulong>> quotes;
        public Quotelash() {
            history = new Dictionary<ulong, Quote>();
            quotes = new Dictionary<string, HashSet<ulong>>();
        }
        public void OnLoad(Program p) {
            this.client = p.client;
        }
        public async void Handle(MessageCreateEventArgs e) {
            Read(e.Message);
            if (e.Author.IsCurrent) {
                return;
            }
            switch (e.Message.Content) {
                case "!read":
                    Console.WriteLine("!read");
                    foreach(var ch in (await e.Guild.GetChannelsAsync()).Where(ch => ch.Type == ChannelType.Text)) {
                        await ReadChannel(ch);
                    }
                    //await e.Message.Channel.SendMessageAsync("OK, I've seen everything now.");
                    break;
                case "!update":
                    foreach (var q in history.Values) {
                        AddQuote(q);
                    }
                    break;
                case var s when s.StartsWith("!quote random"):
                    if (e.Message.MentionedUsers.Any()) {
                        Quote(new Context() {
                            authorId = e.Message.MentionedUsers.First().Id,
                            serverId = e.Message.Channel.GuildId
                        });
                    } else {
                        Quote(new Context() {
                            authorId = 0,
                            serverId = e.Message.Channel.GuildId
                        });
                    }
                    async void Quote(Context c) {
                        if (quotes.TryGetValue(c, out var listing)) {
                            var id = listing.ElementAt(new Random().Next(listing.Count));
                            var q = history[id];
                            var author = (await client.GetUserAsync(q.author)).Username;
                            await e.Channel.SendMessageAsync($"> {q.message}\n- **{author}** on {q.timestamp.UtcDateTime.ToString()}");
                        }
                    }
                    break;
            }
        }
        public async Task ReadChannel(DiscordChannel channel) => await Task.Run(async () => {
            var messages = await channel.GetMessagesAsync(100);
            int readCount = 0;
            Console.WriteLine($"Read {readCount} first messages");

            do {
                foreach (var message in messages) {

                    if (!history.ContainsKey(message.Id)) {
                        readCount++;
                        Read(message);
                        Console.WriteLine($"Read: {message.Content}");
                    }
                }
                Console.WriteLine($"Read {messages.Count} messages -----------------------------------------------------------------------------");
                messages = (await channel.GetMessagesAsync(100, messages.Last().Id));
            } while (messages.Count > 0);
        Done:
            Console.WriteLine($"Read {readCount} messages");
        });
        public void Read(DiscordMessage m) {
            var q = new Quote(m);
            history[m.Id] = q;
            AddQuote(q);

        }
        public void AddQuote(Quote m) {
            if(!m.reactions.Any()) {
                return;
            }
            if (m.reactions.Any(r => r.Emoji.Name == "⏺️")) {
                Add(new Context() {
                    authorId = m.author,
                    serverId = m.server
                }, m.id);
                Add(new Context() {
                    authorId = m.author,
                    serverId = 0
                }, m.id);
                Add(new Context() {
                    authorId = 0,
                    serverId = m.server
                }, m.id);
                void Add(Context c, ulong id) {
                    if (!quotes.TryGetValue(c, out var listing)) {
                        listing = quotes[c] = new HashSet<ulong>();
                    }
                    listing.Add(id);
                }
            }

        }
    }
}
