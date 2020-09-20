using DSharpPlus;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;

namespace SpongeLake {

    public class Dinner {
        private DiscordClient client;
        public Dictionary<ulong, Game> gameByGuild = new Dictionary<ulong, Game>();
        public Dinner(DiscordClient client) {
            this.client = client;
        }
        public void Load(DiscordClient client) {
            this.client = client;
        }
        public async void Handle(MessageCreateEventArgs e) {
            if (e.Author.IsCurrent) {
                return;
            }
            //See if a game is currently running
            if (gameByGuild.TryGetValue(e.Guild.Id, out Game game)) {
                game.Handle(e);
            } else {
                var g = new Game();
                gameByGuild[e.Guild.Id] = g;
                g.Handle(e);
            }
        }
        public class Game {
            Dictionary<ulong, int> seatByUser = new Dictionary<ulong, int>();
            Dictionary<int, ulong> seats = new Dictionary<int, ulong>();


            HashSet<ulong> thunk = new HashSet<ulong>();
            Dictionary<int, ulong> forks = new Dictionary<int, ulong>();
            HashSet<ulong> done = new HashSet<ulong>();
            public async void Handle(MessageCreateEventArgs e) {
                var author = e.Author.Id;
                var s = e.Message.Content;
                if (s.StartsWith("!seat")) {
                    if (seatByUser.TryGetValue(author, out int seat)) {
                        await e.Channel.SendMessageAsync($"{e.Author.Mention}, you leave seat {seat}");

                        seats.Remove(seat);
                        seatByUser.Remove(author);
                        thunk.Remove(author);
                        if (forks.TryGetValue(seat, out ulong user) && user == author) {
                            forks.Remove(seat);
                        }
                        if (forks.TryGetValue((seat + 1) % 5, out user) && user == author) {
                            forks.Remove((seat + 1) % 5);
                        }
                        done.Remove(author);
                        return;
                    } else if (int.TryParse(s.Substring("!seat".Length), out seat)) {

                        if (seat > -1 && seat < 5) {
                            if (seats.TryGetValue(seat, out ulong user)) {
                                if (user == author) {
                                    await e.Channel.SendMessageAsync($"{e.Author.Mention}, you are already seated at {seat}");
                                } else {
                                    await e.Channel.SendMessageAsync($"{e.Author.Mention}, that seat is already occupied by {(await e.Guild.GetMemberAsync(user)).Mention}");
                                }
                            } else {
                                seatByUser[author] = seat;
                                seats[seat] = author;
                                await e.Channel.SendMessageAsync($"{e.Author.Mention}, you sit at seat {seat}");
                            }
                        } else {
                            await e.Channel.SendMessageAsync($"{e.Author.Mention}, please enter a seat number between 0 and 4");
                            return;
                        }
                    } else {
                        await e.Channel.SendMessageAsync($"{e.Author.Mention}, please enter a seat number between 0 and 4");
                        return;
                    }
                } else if (s.Equals("!fork left")) {
                    if (!seatByUser.TryGetValue(author, out int seat)) {
                        await e.Channel.SendMessageAsync($"{e.Author.Mention}, you are not seated");
                    } else if (!thunk.Contains(author)) {
                        await e.Channel.SendMessageAsync($"{e.Author.Mention}, you must think first");
                    } else {
                        Fork(seat);
                    }

                } else if (s.Equals("!fork right")) {
                    if (!seatByUser.TryGetValue(author, out int seat)) {
                        await e.Channel.SendMessageAsync($"{e.Author.Mention}, you are not seated");
                    } else if (!thunk.Contains(author)) {
                        await e.Channel.SendMessageAsync($"{e.Author.Mention}, you must think first");
                    } else {
                        Fork((seat + 1) % 5);
                    }
                } else if(s.Equals("!think")) {
                    await e.Channel.SendMessageAsync($"{e.Author.Mention}, you think about eating food");
                    thunk.Add(author);
                } else if(s.Equals("!eat")) {
                    if (!seatByUser.TryGetValue(author, out int seat)) {
                        await e.Channel.SendMessageAsync($"{e.Author.Mention}, you are not seated");
                    } else if (!thunk.Contains(author)) {
                        await e.Channel.SendMessageAsync($"{e.Author.Mention}, you must think first");
                    } else if (!forks.TryGetValue(seat, out ulong user) || user != author) {
                        await e.Channel.SendMessageAsync($"{e.Author.Mention}, you need your left fork!");
                    } else if (!forks.TryGetValue((seat + 1) % 5, out user) || user != author) {
                        await e.Channel.SendMessageAsync($"{e.Author.Mention}, you need your right fork!");
                    } else if(done.Contains(author)) {
                        await e.Channel.SendMessageAsync($"{e.Author.Mention}, you have already eaten your dinner!");
                    } else {
                        done.Add(author);
                        await e.Channel.SendMessageAsync($"{e.Author.Mention}, you eat a theoretical meal of philosophical food");
                    }
                }
                async void Fork(int fork) {
                    if(forks.TryGetValue(fork, out ulong user)) {
                        if(user == e.Author.Id) {
                            await e.Channel.SendMessageAsync($"{e.Author.Mention}, you drop the fork");
                            forks.Remove(fork);
                        } else {
                            await e.Channel.SendMessageAsync($"{e.Author.Mention}, that fork is currently used by {(await e.Guild.GetMemberAsync(user)).Mention}");
                        }
                    } else {
                        forks[fork] = author;
                        await e.Channel.SendMessageAsync($"{e.Author.Mention}, you take the fork");
                    }
                }
            }
        }
    }
}
