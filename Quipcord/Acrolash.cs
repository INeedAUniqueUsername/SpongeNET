using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Quipcord {
    public class Acrolash {
        private DiscordClient client;
        public Dictionary<ulong, Game> gameByChannel = new Dictionary<ulong, Game>();
        public Acrolash() {
            gameByChannel = new Dictionary<ulong, Game>();
        }
        public void OnLoad(Program p) {
            this.client = p.client;

            //Resume any games
            foreach ((var channel, var game) in gameByChannel) {
                Task.Run(() => {
                    Update(game);
                });
            }
        }
        public void OnSave() {

        }
        public async Task Handle(MessageCreateEventArgs e) {
            if (e.Author.IsCurrent) {
                return;
            }

            bool allowed = e.Channel.PermissionsFor(await e.Guild.GetMemberAsync(client.CurrentUser.Id)).HasPermission(Permissions.SendMessages);

            //See if a game is currently running
            if (gameByChannel.TryGetValue(e.Channel.Id, out Game game)) {
                game.Handle(e);
            } else {
                


                Regex splitter = new Regex($"({Regex.Escape("?")}|{Regex.Escape("!")}|{Regex.Escape(" ")}|{Regex.Escape(",")}|{Regex.Escape(".")}|{Regex.Escape("'")})");
                var content = splitter.Split(e.Message.Content);

                var acronym = content.FirstOrDefault(s => s.All(c => char.IsUpper(c)));
                if (!string.IsNullOrEmpty(acronym)) {
                    if (!allowed) {
                        await e.React(client, ":x:");
                        Task.Run(async () => {
                            await Task.Delay(2000);
                            await e.Unreact(client, ":x:");
                        });
                        return;
                    }
                    Game g = new Game() {
                        channelId = e.Channel.Id,
                        askerId = e.Author.Id,
                        acronym = acronym
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
                    return;
            }
            switch (g.state) {
                case Game.GameState.Start:
                    Update(g);
                    break;
                case Game.GameState.Submissions:
                    Task.Run(async () => {
                        await Task.Delay(60000);
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
        public class Game {
            public ulong channelId;
            public ulong askerId;
            public string acronym;

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
                if (responseByUser.ContainsKey(e.Author.Id)) {
                    return;
                }
                if (state == GameState.Submissions) {
                    if (new string(e.Message.Content.Split(' ').Where(s => s.Any()).Select(s => char.ToUpper(s[0])).ToArray()) == acronym ||
                        new string(e.Message.Content.Where(c => char.IsUpper(c)).ToArray()) == acronym) {
                        responseByUser[e.Author.Id] = e.Message.Content.Split("\n").First();
                    }
                }
            }
            public async void OpenSubmissions(DiscordClient e) {
                state = GameState.Submissions;
                var name = (await e.GetUserAsync(askerId)).Username;
                var channel = await e.GetChannelAsync(channelId);
                await e.SendMessageAsync(channel, $"{acronym}! What does it stand for?");
            }
            public async void OpenVoting(DiscordClient e) {
                var name = (await e.GetUserAsync(askerId)).Username;

                int index = 0;
                var digits = new List<string> { ":zero:", ":one:", ":two:", ":three:", ":four:", ":five:", ":six:", ":seven:", ":eight:", ":nine:" };
                var regional = Enumerable.Range(0, 26).Select(i => $":regional_indicator_{('a' + i)}:");
                List<string> reactions = digits.Concat(regional).ToList();

                if (responseByUser.Count > 1) {
                    var message = new StringBuilder($"{acronym}! What does it stand for? Here are your answers!\n");
                    foreach ((var userId, var response) in responseByUser) {
                        var username = (await e.GetUserAsync(userId)).Username;
                        var react = reactions[index++];
                        choiceByReact[react] = userId;
                        message.Append($"\n{react} {username}: \"{response.ToUpper()}\"");

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
                if (!responseByUser.Any()) {
                    return;
                }

                var name = (await e.GetUserAsync(askerId)).Username;
                var channel = await e.GetChannelAsync(channelId);

                int winningVotes = 2;
                List<ulong> winningUsers = new List<ulong>();

                var votingMessage = (await (await e.GetChannelAsync(channelId)).GetMessageAsync(votingMessageId));
                foreach (var react in votingMessage.Reactions) {
                    if (choiceByReact.TryGetValue(react.Emoji.GetDiscordName(), out var userId)) {
                        if (react.Count > winningVotes) {
                            winningUsers.Clear();
                            winningUsers.Add(userId);
                        } else if (react.Count == winningVotes) {
                            winningUsers.Add(userId);
                        }
                    }
                }
                if (winningUsers.Any()) {
                    var message = new StringBuilder($"{acronym}! What does it stand for? Here are the winning answers!");
                    foreach (var userId in winningUsers) {
                        var username = (await e.GetUserAsync(userId)).Username;
                        message.Append($"\n{username} said, \"{responseByUser[userId]}\"");
                    }
                    await e.SendMessageAsync(channel, message.ToString());
                } else {
                    var message = new StringBuilder($"{acronym}! What does it stand for? We don't know!");
                    await e.SendMessageAsync(channel, message.ToString());
                }
            }
        }
    }
}
