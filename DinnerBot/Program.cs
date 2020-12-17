using DSharpPlus;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
namespace Quipcord {
    class Program {
        DiscordClient discord;
        Dinner d;
        static void Main(string[] args) {
            Program p = new Program();
            p.MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        async Task MainAsync(string[] args) {
            discord = new DiscordClient(new DiscordConfiguration {
                Token = "NzAyNjU5OTEzMDgzNTE5MTI3.XqDRGg.FW-A81LX7xow29UrtUfKMxhiwu4",
                TokenType = TokenType.Bot,
                UseInternalLogHandler = true,
                LogLevel = LogLevel.Debug
            });
            discord.MessageCreated += e => Task.Run(() => {
                d.Handle(e);
            });
            if (Load(out d)) {
                d.Load(discord);
            } else {
                d = new Dinner(discord);
            }
            PeriodicSave();
            await discord.ConnectAsync();
            await Task.Delay(-1);
        }
        public async void PeriodicSave() {
            Console.WriteLine("Saved");
            await Task.Run(async () => {
                await Task.Delay(1000 * 60 * 60);
                PeriodicSave();
            });
        }
        public void Save<T>(T t) {
            var f = $"./{typeof(T).Name}.json";
            Console.WriteLine(Path.GetFullPath(f));

            File.WriteAllText(f, JsonConvert.SerializeObject(t, Formatting.Indented, new JsonSerializerSettings() {
                TypeNameHandling = TypeNameHandling.All
            }));
        }
        public bool Load<T>(out T t) {
            var f = $"{typeof(T).Name}.json";
            Console.WriteLine(Path.GetFullPath(f));
            if (File.Exists(f)) {
                t = JsonConvert.DeserializeObject<T>(File.ReadAllText(f));
                return true;
            }
            t = default(T);
            return false;
        }
    }
}
