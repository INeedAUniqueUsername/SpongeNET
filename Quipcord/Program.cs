﻿using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
namespace SpongeNET {
    class Program {
        DiscordClient discord;
        Quiplash q;
        static void Main(string[] args) {
            Program p = new Program();
            p.MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        async Task MainAsync(string[] args) {
            discord = new DiscordClient(new DiscordConfiguration {
                Token = "Njg0NTk5MTYyNzA4MTY0NjEx.Xl8csQ.SGPJxb1BHMcXNvYo-ECLxyu0soY",
                TokenType = TokenType.Bot,
                UseInternalLogHandler = true,
                LogLevel = LogLevel.Debug
            });
            discord.MessageCreated += e => Task.Run(() => {
                q.Handle(e);
            });
            if(Load(out q)) {
                q.Load(discord);
            } else {
                q = new Quiplash(discord);
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
    class Quiplash {
        private DiscordClient client;
        public Dictionary<ulong, Game> gameByChannel = new Dictionary<ulong, Game>();
        public Quiplash(DiscordClient client) {
            this.client = client;
        }
        public void Load(DiscordClient client) {
            this.client = client;

            //Resume any games
            foreach((var channel, var game) in gameByChannel) {
                Task.Run(() => {
                    Update(game);
                });
            }
        }
        public async void Handle(MessageCreateEventArgs e) {
            if(e.Author.IsCurrent) {
                return;
            }
            //See if a game is currently running
            if (gameByChannel.TryGetValue(e.Channel.Id, out Game game)) {
                game.Handle(e);
            } else {
                Regex splitter = new Regex($"({Regex.Escape("?")})");
                var content = splitter.Split(e.Message.Content);
                if(content.Contains("?")) {
                    var question = string.Join("", content.Reverse().SkipWhile(s => s != "?").Reverse());
                    var prefixes = new string[] { "Who", "What", "When", "Where", "Why", "How" };
                    if(!prefixes.Any(p => question.StartsWith(p))) {
                        //return;
                    }
                    Game g = new Game() {
                        channelId = e.Channel.Id,
                        askerId = e.Author.Id,
                        question = question
                    };
                    gameByChannel[e.Channel.Id] = g;
                    Update(g);
                }
            }
        }
        public void Update(Game g) {
            switch (g.state) {
                case Game.GameState.Start:
                    g.OpenSubmissions(client);
                    break;
                case Game.GameState.Submissions:
                    g.OpenVoting(client);
                    break;
                case Game.GameState.Voting:
                    g.CloseGame(client);
                    break;
                case Game.GameState.Done:
                    gameByChannel.Remove(g.channelId);
                    break;
            }
            switch(g.state) {
                case Game.GameState.Start:
                    Update(g);
                    break;
                case Game.GameState.Submissions:
                    Task.Run(async () => {
                        await Task.Delay(30000);
                        Update(g);
                    });
                    break;
                case Game.GameState.Voting:
                    Task.Run(async () => {
                        await Task.Delay(15000);
                        Update(g);
                    });
                    break;
                case Game.GameState.Done:
                    Update(g);
                    break;

            }
        }
    }
    class Game {
        public ulong channelId;
        public ulong askerId;
        public string question;
        
        public enum GameState {
            Start,
            Submissions,
            Voting,
            Done
        }
        public GameState state = GameState.Start;

        public ulong votingMessageId;
        public Dictionary<ulong, string> responseByUser = new Dictionary<ulong, string>();
        public Dictionary<string, ulong> choiceByReact = new Dictionary<string, ulong>();

        public void Handle(MessageCreateEventArgs e) {
            //If the user already responded, don't update the response
            if(responseByUser.ContainsKey(e.Author.Id)) {
                return;
            }
            if(state == GameState.Submissions) {
                responseByUser[e.Author.Id] = e.Message.Content.Split("\n").First();
            }
        }
        public async void OpenSubmissions(DiscordClient e) {
            state = GameState.Submissions;
            var name = (await e.GetUserAsync(askerId)).Username;
            var channel = await e.GetChannelAsync(channelId);
            //await e.SendMessageAsync(channel, $"{name} asks, \"{question}\" Answer below!");
        }
        public async void OpenVoting(DiscordClient e) {
            var name = (await e.GetUserAsync(askerId)).Username;

            int index = 0;
            var digits = new List<string> { ":zero:", ":one:", ":two:", ":three:", ":four:", ":five:", ":six:", ":seven:", ":eight:", ":nine:" };
            var regional = Enumerable.Range(0, 26).Select(i => $":regional_indicator_{('a' + i)}:");
            List<string> reactions = digits.Concat(regional).ToList();

            if(responseByUser.Count > 1) {
                var message = new StringBuilder($"{name} asked, \"{question}\" Here are your answers!\n");
                foreach ((var userId, var response) in responseByUser) {
                    var username = (await e.GetUserAsync(userId)).Username;
                    var react = reactions[index++];
                    choiceByReact[react] = userId;
                    message.Append($"\n{react} {username} said, \"{response.ToUpper()}\"");

                }
                message.Append("\n\nVote for your favorites by reacting below!");
                var channel = await e.GetChannelAsync(channelId);
                var votingMessage = (await e.SendMessageAsync(channel, message.ToString()));
                foreach ((var react, var userId) in choiceByReact) {
                    await votingMessage.CreateReactionAsync(DiscordEmoji.FromName(e, react));
                }
                votingMessageId = votingMessage.Id;
                state = GameState.Voting;
            } else {
                state = GameState.Submissions;
            }
        }
        public async void CloseGame(DiscordClient e) {
            state = GameState.Done;
            if(!responseByUser.Any()) {
                return;
            }

            var name = (await e.GetUserAsync(askerId)).Username;
            var channel = await e.GetChannelAsync(channelId);

            int winningVotes = 2;
            List<ulong> winningUsers = new List<ulong>();

            var votingMessage = (await (await e.GetChannelAsync(channelId)).GetMessageAsync(votingMessageId));
            foreach (var react in votingMessage.Reactions) {
                if(choiceByReact.TryGetValue(react.Emoji.GetDiscordName(), out var userId)) {
                    if(react.Count > winningVotes) {
                        winningUsers.Clear();
                        winningUsers.Add(userId);
                    } else if(react.Count == winningVotes) {
                        winningUsers.Add(userId);
                    }
                }
            }
            if(winningUsers.Any()) {
                var message = new StringBuilder($"{name} asked, \"{question}\" Here are the winning answers!");
                foreach (var userId in winningUsers) {
                    var username = (await e.GetUserAsync(userId)).Username;
                    message.Append($"\n{username} said, \"{responseByUser[userId]}\"");
                }
                await e.SendMessageAsync(channel, message.ToString());
            } else {
                var message = new StringBuilder($"{name} asked, \"{question}\" But looks like we'll never know!");
                await e.SendMessageAsync(channel, message.ToString());
            }
        }
    }
}