using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using Quipcord.SpongeLake;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
namespace Quipcord
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
                Token = File.ReadAllText("token.txt"),
                TokenType = TokenType.Bot,
                UseInternalLogHandler = true,
                LogLevel = LogLevel.Debug
            });
            discord.MessageCreated += async e => await Task.Run(() => Handle(e));
            lake = new Lake(discord);
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
            lake.Update();
            Console.WriteLine("Update");
            await Task.Run(async () => {
                await Task.Delay(1000);
                PeriodicUpdate();
            });
        }
        public void LoadModules() {
            Load(ref lake);
            lake.Load(discord);
        }
        public void SaveModules() {
            Save(lake);
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
        Lake lake;

        /*

            if commandList contains the command they typed...
                ...is .enabled? if FALSE, let them know and return
                ...is 

        */
        async void Handle(MessageCreateEventArgs e) {
            //Core commands
            //too complic8ed??
            switch(e.Message.Content) {
                case ".save":
                    SaveModules();
                    await e.Channel.SendMessageAsync("Saved");
                    break;
                case ".load":
                    LoadModules();
                    await e.Channel.SendMessageAsync("Loaded");
                    break;
                default:
                    Console.WriteLine(e.Message.Content);
                    if (e.Author.Id == discord.CurrentUser.Id && !e.Message.Content.StartsWith(".as")) {
                        return;
                    }
                    try {
                        await Task.Run(() => {
                            lake.Handle(e.Message);
                        });
                    } catch (Exception ex) {
                        await e.Channel.SendMessageAsync($"Damn, you got me there, {e.Author.Mention}. Says here that \"{ex.Message}\". I don't know what that means, but uh oh, I guess. I'll just wait here until someone comes to fix me.");
                        SaveModules();
                        throw ex;
                    }
                    break;
            }
        }
    }
    //bool[] known;
    //string answer;

    //We can use this class to manage permissions... and keep the code in a massive switch block
    //too many objects
    //idk
    //Print a giant string :p
    //yeahh
    /*
     
     
     */
    class CommandPermissions {
        // enabled: bool
        // minAcessLevel: int or something
        // noUseInBattle: bool
        // noUseWhileAsleep: bool   (will be TRUE for most commands except like topxp, topfame, help, etc.)
        // moderatorPowers (idk) (bitmasked flags?)
        // code: (a function to call or code to run)



        // just thinking:
        // MEHscript to run?
        // log? (can be toggled to control something like logging or counting invocations of the command)?
    }
}
