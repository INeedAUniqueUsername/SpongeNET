using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using SpongeNET.SpongeNET;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
namespace SpongeNET
{
    class Program
    {
        DiscordClient discord;

        static void Main(string[] args)
        {
            Program p = new Program();
            p.MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        async Task MainAsync(string[] args)
        {
            discord = new DiscordClient(new DiscordConfiguration {
                Token = "NDE3NTQyNDQ0MzUyMDEyMjg4.XgHF6A.sWSYkN9uGWQsnt62sYnAKJ4WKNk",
                TokenType = TokenType.Bot,
                UseInternalLogHandler = true,
                LogLevel = LogLevel.Debug
            });
            discord.MessageCreated += async e => await Task.Run(() => Handle(e));
            a = new Acro(discord);
            m = new Markov(discord);
            net = new Net(discord);
            LoadModules();
            PeriodicSave();
            PeriodicUpdate();
            await discord.ConnectAsync();
            await Task.Delay(-1);
        }
        public async void PeriodicSave() {
            SaveModules();
            Console.WriteLine("Saved");
            await Task.Run(async () => {
                await Task.Delay(1000 * 60 * 60);
                PeriodicSave();
            });
        }
        public async void PeriodicUpdate() {
            net.Update();
            Console.WriteLine("Update");
            await Task.Run(async () => {
                await Task.Delay(1000);
                PeriodicUpdate();
            });
        }
        public void LoadModules() {
            Load(ref a);
            Load(ref net);
            Load(ref m);
            a.Load(discord);
            net.Load(discord);
            m.Load(discord);
        }
        public void SaveModules() {
            Save(a);
            Save(net);
            Save(m);
        }
        public string GetFile<T>() {
            return $"./{typeof(T).Name}.xml";
        }
        public bool CanLoad<T>() {
            return File.Exists(GetFile<T>());
        }
        public void Save<T>(T t) {
            var f = $"./{typeof(T).Name}.json";
            Console.WriteLine(Path.GetFullPath(f));

            File.WriteAllText(f, JsonConvert.SerializeObject(t, Formatting.Indented, new JsonSerializerSettings() {
                TypeNameHandling = TypeNameHandling.All
            }));
        }
        public void Load<T>(ref T t) {
            var f = $"{typeof(T).Name}.json";
            Console.WriteLine(Path.GetFullPath(f));
            if (File.Exists(f))
                t = JsonConvert.DeserializeObject<T>(File.ReadAllText(f));
        }
        Acro a;
        Markov m;
        Net net;

        async void Handle(MessageCreateEventArgs e) {
            if (e.Message.Content.Equals(".save")) {
                SaveModules();
                await e.Channel.SendMessageAsync("Saved");
            } else if(e.Message.Content.Equals(".load")) {
                LoadModules();
                await e.Channel.SendMessageAsync("Loaded");
            }
            Console.WriteLine(e.Message.Content);
            if(e.Author.Id == discord.CurrentUser.Id && !e.Message.Content.StartsWith(".as")) {
                return;
            }

            try {
                await Task.Run(() => {
                    a.Handle(e.Message);
                    net.Handle(e.Message);
                    m.Handle(e.Message);
                });
            } catch(Exception ex) {
                await e.Channel.SendMessageAsync($"Damn, you got me there, {e.Author.Mention}. Says here that \"{ex.Message}\". I don't know what that means, but uh oh, I guess. I'll just wait here until someone comes to fix me.");
                SaveModules();
                throw ex;
            }
            
        }
    }
}
