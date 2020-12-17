using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Quipcord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quipcord {
    public class Markovlash {
        private DiscordClient client;
        private Quotelash quote;
        public string currentModel;
        public Dictionary<string, MarkovModel> models;

        public MarkovModel current => models[currentModel];
        public Markovlash() {
            currentModel = "default";
            models = new Dictionary<string, MarkovModel>();
            models[currentModel] = new MarkovModel();
        }
        public void OnLoad(Program p) {
            this.client = p.client;
            quote = p.quote;
        }
        public async Task Handle(MessageCreateEventArgs e) {
            var m = e.Message;

            switch(m.Content) {
                case "!markov reset":
                    current.words.dict.Clear();
                    current.read.Clear();
                    break;
                case var s when s.StartsWith("!markov read"):
                    //await m.DeleteAsync();
                    if (m.MentionedChannels.Count > 0) {
                        foreach (var channel in m.MentionedChannels) {
                            ReadChannel(channel);
                        }
                    } else {
                        ReadAll();
                    }
                    void ReadAll() {
                        var history = quote.history.Values;
                        foreach (var message in history) {
                            current.read.Add(message.id);
                            current.Read(message);
                        }
                    }
                    void ReadChannel(DiscordChannel channel) {
                        var history = quote.history.Values.Where(q => q.channel == channel.Id);
                        foreach (var message in history) {
                            if (!current.read.Contains(message.id)) {
                                current.read.Add(message.id);
                                current.Read(message);
                            }
                        }
                    }
                    break;
                case var s when s.StartsWith("!markov say"):
                    await m.DeleteAsync();
                    if (m.MentionedChannels.Count > 0) {
                        foreach (var channel in m.MentionedChannels) {

                        }
                        Say(current);
                    } else {
                        Say(current);
                    }
                    //await m.DeleteAsync();
                    break;
            }
            async void Say(MarkovModel model) {
                var dict = model.words;
                if(!dict.dict.Any()) {
                    return;
                }

                var previous = dict.GetRandomKey();
                string result = previous;

                int tries = 100;
                while(tries > 0) {
                    string next = dict.Get(previous);
                    if (next != null && result.Length + next.Length < 2000) {
                        result += $" {next}";
                        previous = next;
                        tries = 100;
                    } else {
                        tries--;
                    }
                }
                await m.Channel.SendMessageAsync(result);
            }
        }
    }
    public class MarkovModel {
        public DictSet<string> words;
        public HashSet<ulong> read;
        public MarkovModel() {
            words = new DictSet<string>();
            read = new HashSet<ulong>();
        }
        public void Read(Quote m) {
            string s = m.message;
            var words = s.Split(' ');
            for (int i = 0; i < words.Length - 1; i++) {
                this.words.Add(words[i], words[i + 1]);
            }
            if (words.Length > 0) {
                this.words.Add(words.Last(), null);
            }
        }
    }
    public class DictSet<T> {
        public Dictionary<T, HashSet<T>> dict;
        public DictSet() {
            dict = new Dictionary<T, HashSet<T>>();
        }
        public void Add(T key, T value) {
            if (!dict.TryGetValue(key, out HashSet<T> set)) {
                dict[key] = set = new HashSet<T>();
            }
            set.Add(value);
        }
        public T Get(T key) {
            if (dict.TryGetValue(key, out HashSet<T> set)) {
                return set.ElementAt(new Random().Next(0, set.Count));
            } else {
                return default(T);
            }
        }
        public T GetRandomKey() {
            var keys = dict.Keys;
            var index = new Random().Next(keys.Count);
            return keys.ElementAt(index);
        }
    }
    public class DictList<T> {
        public Dictionary<T, List<T>> dict;
        public DictList() {
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
