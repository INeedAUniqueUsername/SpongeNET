using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static SpongeNET.Helper;
using System.Linq;
using DSharpPlus;
using Newtonsoft.Json;

namespace SpongeNET {
    public class Acro {
        private DiscordClient client;
        
        private Random r;

        [JsonProperty]
        GameRecord history;
        [JsonProperty]
        Dictionary<ulong, AcroGame> channelGames;
        public Acro(DiscordClient client) {
            this.client = client;
            r = new Random();
            history = new GameRecord();
            channelGames = new Dictionary<ulong, AcroGame>();
        }
        public void Load(DiscordClient client) {
            this.client = client;
            r = new Random();
        }
        public void SendMessage(DiscordChannel channel, string message) {
            channel.SendMessageAsync("`.acro`: " + message);
        }
        public bool IsValid(string letters) => letters.All(c => char.IsLetter(c));
        public string biglet(string letters) => string.Join(' ', letters.ToLower().Select(c => $":regional_indicator_{c}:"));
        public async void Handle(DiscordMessage m) {
            DiscordChannel c = m.Channel;
            string command = m.Content;

            if(Subsplit(ref command, ' ') != ".acro") {
                return;
            }
            switch(Subsplit(ref command, ' ').ToLower()) {
                case "start":
                    if(m.Channel.IsPrivate) {

                    } else {
                        if (channelGames.TryGetValue(m.ChannelId, out AcroGame current)) {
                            SendMessage(c, "Game already active");
                        } else {
                            Start();
                        }
                    }
                    break;
                case "skip":
                    if (m.Channel.IsPrivate) {

                    } else {
                        if (channelGames.TryGetValue(m.ChannelId, out AcroGame current)) {
                            if(current.entriesOpen) {
                                SendMessage(c, $"closing entries early");
                                current.entriesOpen = false;
                            } else if(current.votingOpen) {
                                SendMessage(c, $"closing votes early");
                                current.votingOpen = false;
                            }
                        } else {
                            SendMessage(c, $"there is no acro game current running in this channel!");
                        }
                    }
                    break;
                case "stop":
                    if (m.Channel.IsPrivate) {

                    } else {
                        if (channelGames.TryGetValue(m.ChannelId, out AcroGame current)) {
                            current.stopped = true;
                        } else {
                            SendMessage(c, $"there is no acro game current running in this channel!");
                        }
                    }
                    break;
                case "extend":
                    if (m.Channel.IsPrivate) {

                    } else {
                        if (channelGames.TryGetValue(m.ChannelId, out AcroGame current)) {
                            if (current.entriesOpen) {
                                current.entryTime += 60;
                                SendMessage(c, $"added 60 seconds to the submission time!");
                            } else if (current.votingOpen) {
                                current.voteTime += 30;
                                SendMessage(c, $"added 30 seconds to the voting time!");
                            } else if(current.tiebreaker) {
                                current.tiebreakerTime += 30;
                                SendMessage(c, $"added 30 seconds to the tiebreaker time!");
                            }
                        } else {
                            SendMessage(c, $"there is no acro game current running in this channel!");
                        }
                    }
                    break;
                case "enter":
                    if (m.Channel.IsPrivate) {

                    } else {
                        if (channelGames.TryGetValue(m.ChannelId, out AcroGame current)) {
                            if(current.entriesOpen) {
                                string letters = new string(command.Where(ch => char.IsUpper(ch)).ToArray());
                                if(letters.Equals(current.letters)) {
                                    if (!current.authors.Contains(m.Author.Id) || current.multipleEntries) {
                                        SendMessage(c, $"{m.Author.Mention}, your entry is in!");
                                    } else {
                                        SendMessage(c, $"{m.Author.Mention}, your entry is in, replacing your previous entry.");
                                        current.entries.RemoveAll(entry => entry.author == m.Author.Id);
                                    }
                                    current.entries.Add(new AcroEntry() { author = m.Author.Id, entry = command });
                                } else {
                                    SendMessage(c, $"{m.Author.Mention}, your entry {letters} does not fit the acronym {current.letters}!");
                                }
                            }
                        } else {
                            SendMessage(c, $"there is no acro game current running in this channel!");
                        }
                    }
                    break;
                case "vote":
                    if (m.Channel.IsPrivate) {

                    } else {
                        if (channelGames.TryGetValue(m.ChannelId, out AcroGame current)) {
                            await Task.Run(() => Vote(current));
                        } else {
                            SendMessage(c, $"there is no acro game current running in this channel!");
                        }
                    }
                    break;
                case "history":
                    var recent = history.games.Count > 3 ? history.games.TakeLast(3) : history.games;

                    var best = new List<AcroGame>(history.games);
                    best.Sort((g1, g2) => {
                        return (g1.votes.Count > g2.votes.Count) ? 1 :
                                (g1.votes.Count < g2.votes.Count) ? -1:
                                0;
                    });

                    best = best.Count > 3 ? best.GetRange(0, 3) : best;

                    SendMessage(c, $"Games played: {history.games.Count}\nRecent games\n{string.Join('\n',recent.Select(g => $"Letters: {biglet(g.letters)}\nWinners: {string.Join('\n', g.winners.Select(w => $@"""{w.entry}"" by {w.author}"))}"))}\n\nBest games\n{string.Join('\n', best.Select(g => $"Letters: {biglet(g.letters)}\nWinners: {string.Join('\n', g.winners.Select(w => $@"""{w.entry}"" by {w.author}"))}"))}");
                    break;
                case ".acro":
                    break;
                case var unknown:
                    Console.WriteLine(unknown);
                    break;
            }

            async void Start() {
                string table = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                int length = r.Next(3, 5);
                string letters = "";
                int entryTime = 0;
                string category = null;
                if (!command.ToLower().Equals("start"))
                    foreach (string parameter in command.Split(' ')) {
                        switch (parameter.SplitFirst(out string arg, ':').ToLower()) {
                            case "length":
                                try {
                                    length = int.Parse(arg);
                                } catch {
                                    SendMessage(c, "Invalid `length` parameter: expected integer");
                                }
                                break;
                            case "table":
                                if (arg.Length > 2) {
                                    if (IsValid(arg)) {
                                        table = arg;
                                    } else {
                                        SendMessage(c, "Invalid `table` parameter: expected strictly letters");
                                    }

                                } else {
                                    SendMessage(c, "Invalid `table` parameter: expected at least two letters");
                                }
                                break;
                            case "letters":
                                if (arg.Length > 1) {
                                    if (IsValid(arg)) {
                                        letters = arg.ToUpper();
                                    } else {
                                        SendMessage(c, "Invalid `letters` parameter: expected strictly letters");
                                    }
                                } else {
                                    SendMessage(c, "Invalid `letters` parameter: expected at least two letters");
                                }
                                break;
                            case "time":
                                try {
                                    entryTime = int.Parse(arg);
                                } catch {
                                    SendMessage(c, "Invalid `time` parameter: expected integer");
                                }
                                break;
                            case "category":
                                category = arg;
                                break;
                            case var unknownParameter:
                                SendMessage(c, $"Unknown parameter {unknownParameter}");
                                break;
                        }
                    }
                if (string.IsNullOrEmpty(letters)) {
                    for (int i = 0; i < length; i++) {
                        letters += table[r.Next(0, table.Length)];
                    }
                }
                entryTime = entryTime > 0 ? entryTime : (int)(50 + Math.Pow(length, 1.5) * 10);
                string[] categories = {
                                "news & world events", "movies & tv", "literature", "food & drink", "science fiction & fantasy"
                            };
                category = category ?? categories[r.Next(categories.Length)];
                SendMessage(c,
$@"Let\'s play the `.acro` game!
Letters: {biglet(letters)}
Category: {category}
You have {entryTime} seconds to make an acronym with them and submit it with `.acro enter`
");
                AcroGame game = new AcroGame() {
                    spectatorVoting = true,
                    showAuthors = false,
                    multipleEntries = false,
                    sortEntries = false,
                    letters = letters,
                    entryTime = entryTime,
                    voteTime = 0,
                    stopped = false,
                    category = category,
                    votingOpen = false,
                    entriesOpen = true,
                };

                channelGames[m.ChannelId] = game;
                for (int i = 0; i < game.entryTime && game.entriesOpen; i++) {
                    var timeLeft = game.entryTime - i;
                    if (timeLeft%30 == 0) {
                        SendMessage(c, $"{timeLeft} seconds left to enter!");
                    }

                    await Task.Delay(1000);
                    if (game.stopped) {
                        SendMessage(c, "The current game was stopped. No contest!");
                        endGame();
                        return;
                    }
                }
                await Task.Run(RunVotes);
                async void RunVotes() {
                    game.entriesOpen = false;
                    game.votingOpen = true;

                    if (game.entries.Count < 1) {
                        SendMessage(c, "Nobody's participating! Sad!");
                        endGame();
                        return;
                    }

                    game.voteTime = game.entries.Count * 10;
                    SendMessage(c,
$@"Time to vote! :stopwatch:
-----------------------------
{string.Join('\n', game.entries.Select((e, i) => $"`.acro vote {i,2}`: \"{e.entry}\""))}
-----------------------------
Vote for your favorite!
You have {game.voteTime} seconds to vote!"
                        );
                    for (int i = 0; i < game.voteTime && game.votingOpen; i++) {
                        var timeLeft = game.voteTime - i;
                        if (timeLeft % 30 == 0) {
                            SendMessage(c, $"{timeLeft} seconds left for voting!");
                        }

                        await Task.Delay(1000);
                        if (game.stopped) {
                            SendMessage(c, "The current game was stopped. No contest!");
                            endGame();
                            return;
                        }
                    }
                    await Task.Run(CountVotes);
                }
                async void CountVotes() {
                    game.votesByEntry = new int[game.entries.Count];
                    foreach(var voter in game.votes.Keys) {
                        var vote = game.votes[voter];
                        game.votesByEntry[vote]++;
                        game.entries[vote].votes.Add(voter);
                    }
                    SendMessage(c,
$@"Voting time is up! :stopwatch:
Here are the results:
-----------------------------
{string.Join('\n', game.entries.Select((e, i) => $"{game.votesByEntry[i]} votes for \"{e.entry}\""))}
-----------------------------
There were {game.votes.Count} votes total!
"
                    );
                    int winnerVotes = 0;
                    var winners = game.winners = new List<AcroEntry>();
                    for (int i = 0; i < game.votesByEntry.Length; i++) {
                        int votes = game.votesByEntry[i];
                        if (votes > winnerVotes) {
                            winnerVotes = votes;
                            winners.Clear();
                            winners.Add(game.entries[i]);
                        } else if (votes == winnerVotes) {
                            winners.Add(game.entries[i]);
                        }
                    }
                    await Task.Delay(3000);
                    if (winnerVotes > 0) {
                        if (winners.Count > 1) {
                            game.tiebreaker = true;
                            game.tiebreakerTime = winners.Count * 5 + 10;
                            SendMessage(c,
$@"But wait, we have a tie!
-----------------------------
{string.Join('\n', winners.Select((e, i) => $"`.acro vote {i,2}`: \"{e.entry}\""))}
-----------------------------
Vote for your MOST favorite entry within the next {game.tiebreakerTime} seconds!
");
                            for (int i = 0; i < game.tiebreakerTime; i++) {
                                await Task.Delay(1000);
                            }
                            await Task.Run(() => OpenTiebreaker(winners));
                        } else {
                            var winner = winners[0];
                            SendMessage(c, $"The winner is {(await client.GetUserAsync(winner.author)).Mention}'s \"{winner.entry}\" with {winnerVotes} votes!");
                            endGame();
                            archiveGame();
                        }
                    } else {
                        SendMessage(c, $"Nobody won! Sad!");
                        endGame();
                    }
                }
                async void OpenTiebreaker(List<AcroEntry> winners) {
                    game.votingOpen = false;

                    int[] votesByTiebreaker = new int[winners.Count];
                    foreach (var v in game.tiebreakerVotes.Values) {
                        votesByTiebreaker[v]++;
                    }

                    SendMessage(c,
$@"Tiebreaker time is up! :stopwatch:
Here are the FINAL results:
-----------------------------
{string.Join('\n', winners.Select((e, i) => $"{votesByTiebreaker[i]} votes for \"{e.entry}\""))}
-----------------------------"
);
                    int tiebreaker = 0;
                    var tiebreakers = new List<AcroEntry>();
                    for (int i = 0; i < votesByTiebreaker.Length; i++) {
                        int votes = votesByTiebreaker[i];
                        if (votes > tiebreaker) {
                            tiebreaker = votes;
                            tiebreakers.Clear();
                            tiebreakers.Add(winners[i]);
                        } else if (votes == tiebreaker) {
                            tiebreakers.Add(winners[i]);
                        }
                    }
                    await Task.Delay(3000);
                    if (tiebreakers.Count > 1) {
                        SendMessage(c, $@"The winners are...
{string.Join('\n', tiebreakers.Select(async e => $"{(await client.GetUserAsync(e.author)).Mention}'s \"{e.entry}\"").Select(t => t.Result))}
...each with {tiebreaker} votes!");
                    } else {
                        var winner = tiebreakers[0];
                        SendMessage(c, $"The winner is {(await client.GetUserAsync(winner.author)).Mention}'s \"{winner.entry}\" with {tiebreaker} votes!");
                    }
                    game.winners = tiebreakers;
                    endGame();
                    archiveGame();
                }
                void endGame() {
                    channelGames.Remove(m.Channel.Id);
                }
                void archiveGame() {
                    game.votingOpen = false;
                    int index = history.games.Count;
                    history.games.Add(game);
                    foreach(var entry in game.entries) {
                        if(!history.stats.TryGetValue(entry.author, out AcroPlayer player)) {
                            player = new AcroPlayer();
                            history.stats[entry.author] = player;
                        }

                        if(!player.entries.TryGetValue(game.letters, out HashSet<AcroEntry> entries)) {
                            entries = new HashSet<AcroEntry>();
                            player.entries[game.letters] = entries;
                        }
                        entries.Add(entry);
                        player.votesWon += entry.votes.Count;

                        foreach(var voter in entry.votes) {
                            if (!history.stats.TryGetValue(voter, out AcroPlayer v)) {
                                v = new AcroPlayer();
                                history.stats[voter] = v;
                            }


                            history.stats[voter].votedFor.Add(entry);

                        }
                    }

                    if (!history.bestOf.TryGetValue(game.letters, out HashSet<AcroEntry> best)) {
                        best = new HashSet<AcroEntry>(game.winners);
                        history.bestOf[game.letters] = best;
                    } else {
                        best.UnionWith(game.winners);
                    }
                    foreach (var winner in game.winners) {
                        var id = winner.author;
                        if (!history.stats.TryGetValue(id, out AcroPlayer player)) {
                            player = new AcroPlayer();
                            history.stats[id] = player;
                        }
                        player.gamesWon.Add(index);
                    }
                }
            }
            void Vote(AcroGame current) {
                if (current.authors.Contains(m.Author.Id) || current.spectatorVoting) {
                    if (int.TryParse(command, out int index) && index > -1) {
                        if(current.votingOpen) {
                            if (index < current.entries.Count) {
                                current.votes[m.Author.Id] = index;
                                SendMessage(c, $"{m.Author.Mention}, your vote is in!");
                            } else {
                                SendMessage(c, $"there aren't that many entries");
                            }
                        } else if (current.tiebreaker) {
                            if (index < current.winners.Count) {
                                current.tiebreakerVotes[m.Author.Id] = index;
                                SendMessage(c, $"{m.Author.Mention}, your vote is in!");
                            } else {
                                SendMessage(c, $"there aren't that many entries");
                            }
                        } else {
                            SendMessage(c, $"sorry, voting is not open right now.");
                        }
                    } else {
                        SendMessage(c, $"wait, which entry is that?");
                    }
                } else {
                    SendMessage(c, $"sorry, only authors can vote this game.");
                }
            }
        }
    }
    public class AcroGame {
        public string acronym;
        public bool spectatorVoting;
        public bool showAuthors;
        public bool multipleEntries;
        public bool sortEntries;
        public string letters;
        public int entryTime;
        public int voteTime;
        public int tiebreakerTime;
        public bool stopped;
        public string category;
        public bool votingOpen;
        public bool entriesOpen;
        public List<AcroEntry> winners;
        public bool tiebreaker;

        public HashSet<ulong> authors = new HashSet<ulong>();
        public List<AcroEntry> entries = new List<AcroEntry>();
        public Dictionary<ulong, int> votes = new Dictionary<ulong, int>();
        public Dictionary<ulong, int> tiebreakerVotes = new Dictionary<ulong, int>();
        public int[] votesByEntry = new int[0];
    }
    public class AcroEntry {
        public string entry;
        public ulong author;
        public HashSet<ulong> votes = new HashSet<ulong>();
    }
    public class AcroPlayer {
        public Dictionary<string, HashSet<AcroEntry>> entries = new Dictionary<string, HashSet<AcroEntry>>();
        public int votesWon = 0;
        public HashSet<int> gamesWon = new HashSet<int>();  //Set of indices in history.games
        public HashSet<AcroEntry> votedFor = new HashSet<AcroEntry>();
    }
    public class GameRecord {
        public Dictionary<string, HashSet<AcroEntry>> bestOf = new Dictionary<string, HashSet<AcroEntry>>();
        public List<AcroGame> games = new List<AcroGame>();
        public Dictionary<ulong, AcroPlayer> stats = new Dictionary<ulong, AcroPlayer>();
    }
}
