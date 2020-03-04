using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
namespace SpongeNET {
    class Markov {
        private DiscordClient discord;
        public Dictionary<ulong, MarkovDictList<string>> dict_channel;
        public MarkovDictList<string> dict_global;
        public HashSet<ulong> read;
        public Markov(DiscordClient discord) {
            this.discord = discord;
            dict_global = new MarkovDictList<string>();
            dict_channel = new Dictionary<ulong, MarkovDictList<string>>();
            read = new HashSet<ulong>();
        }
        public void Load(DiscordClient discord) {
            this.discord = discord;
        }
        public async void Handle(DiscordMessage m) {
            if (m.Content.Equals(".markov reset")) {
                dict_channel = new Dictionary<ulong, MarkovDictList<string>>();
                dict_global = new MarkovDictList<string>();
                read = new HashSet<ulong>();
            } else if(m.Content.StartsWith(".markov read")) {
                await m.DeleteAsync();
                if (m.MentionedChannels.Count > 0) {
                    foreach(var channel in m.MentionedChannels) {
                        if (!dict_channel.TryGetValue(channel.Id, out var _)) {
                            dict_channel[channel.Id] = new MarkovDictList<string>();
                        }
                        ReadChannel(channel);
                    }
                } else {
                    if (!dict_channel.TryGetValue(m.ChannelId, out var _)) {
                        dict_channel[m.ChannelId] = new MarkovDictList<string>();
                    }

                    /*
                    foreach (var guild in discord.Guilds.Values) {
                        foreach(var channel in guild.Channels) {

                        }
                    }
                    */
                    ReadChannel(m.Channel);
                }
                
                async void ReadChannel(DiscordChannel channel) {
                    var messages = await channel.GetMessagesAsync(100);
                    int readCount = 0;
                    Console.WriteLine($"Read {readCount} first messages");
                Read:
                    var last = messages.Last();
                    foreach(var message in messages) {
                        
                        if (read.Contains(message.Id)) {
                            goto Done;
                        }
                        readCount++;
                        Read(message);
                        Console.WriteLine($"Read: {message.Content}");
                    }
                    Console.WriteLine($"Read {messages.Count} messages -----------------------------------------------------------------------------");
                    messages = await channel.GetMessagesAsync(100, last.Id);
                    if(messages.Count > 0) {
                        goto Read;
                    }
                    Done:
                    Console.WriteLine($"Read {readCount} messages");
                }
            } else if(m.Content.StartsWith(".markov say channel")) {
                await m.DeleteAsync();
                if (m.MentionedChannels.Count > 0) {
                    foreach(var channel in m.MentionedChannels) {
                        if (dict_channel.TryGetValue(channel.Id, out MarkovDictList<string> dict)) {
                            Say(dict);
                        }
                    }
                } else if(dict_channel.TryGetValue(m.ChannelId, out MarkovDictList<string> dict)) {
                    Say(dict);
                }
                //await m.DeleteAsync();
                
            } else if(m.Content.StartsWith(".markov say global")) {
                await m.DeleteAsync();
                if (dict_global.dict.Count == 0) {
                    return;
                }
                Say(dict_global);
            }
            async void Say(MarkovDictList<string> dict) {
                var previous = dict.GetRandomKey();
                string result = previous;
            Append:

                int tries = 100;
                Try:
                string next = dict.Get(previous);
                if (next != null && result.Length + next.Length < 2000) {
                    result += $" {next}";
                    previous = next;
                    goto Append;
                } else if(tries > 0) {
                    tries--;
                    goto Try;
                }

                await m.Channel.SendMessageAsync(result);
            }
        }
        public void Read(DiscordMessage m) {
            read.Add(m.Id);
            string s = m.Content;
            var words = s.Split(' ');
            for (int i = 0; i < words.Length - 1; i++) {
                dict_global.Add(words[i], words[i + 1]);
                dict_channel[m.ChannelId].Add(words[i], words[i + 1]);
            }
            if(words.Length > 0) {
                dict_global.Add(words.Last(), null);
                dict_channel[m.ChannelId].Add(words.Last(), null);
            }
        }
    }
    public class MarkovDict<T> {
        public Dictionary<T, HashSet<T>> dict;
        public MarkovDict() {
            dict = new Dictionary<T, HashSet<T>>();
        }
        public void Add(T key, T value) {
            if(!dict.TryGetValue(key, out HashSet<T> set)) {
                dict[key] = set = new HashSet<T>();
            }
            set.Add(value);
        }
        public T Get(T key) {
            if(dict.TryGetValue(key, out HashSet<T> set)) {
                return set.ElementAt(new Random().Next(0, set.Count));
            } else {
                return default(T);
            }
        }
        public T GetRandomKey() {
            return dict.Keys.ElementAt(new Random().Next(0, dict.Count));
        }
    }
    public class MarkovDictList<T> {
        public Dictionary<T, List<T>> dict;
        public MarkovDictList() {
            dict = new Dictionary<T, List<T>>();
        }
        public void Add(T key, T value) {
            if (!dict.TryGetValue(key, out List<T> list)) {
                dict[key] = list = new List<T>();
            }
            list.Add(value);
        }
        public T Get(T key) {
            if (dict.TryGetValue(key, out List<T> list)) {
                return list.ElementAt(new Random().Next(0, list.Count));
            } else {
                return default(T);
            }
        }
        public T GetRandomKey() {
            return dict.Keys.ElementAt(new Random().Next(0, dict.Count));
        }
    }
}
