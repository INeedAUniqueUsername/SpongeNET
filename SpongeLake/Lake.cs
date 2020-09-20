using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using static SpongeLake.Helper;
using System.Threading.Tasks;
using DSharpPlus.EventArgs;

namespace SpongeLake.SpongeLake {
    public partial class Lake {
        public const string prefix = ".lake";
        private DiscordClient discord;
        public Dictionary<ulong, LakeUser> users = new Dictionary<ulong, LakeUser>();
        public Dictionary<string, LakeRoom> rooms = new Dictionary<string, LakeRoom>();
        public Dictionary<ulong, LakePlayer> playerEntities = new Dictionary<ulong, LakePlayer>();

        public ulong guidCounter = 0;
        public ulong CreateGuid() {
            guidCounter++;
            return guidCounter;
        }
        public Lake(DiscordClient discord) {
            this.discord = discord;
        }
        public void Load(DiscordClient discord) {
            this.discord = discord;
        }
        public void Update() {
            foreach (var room in rooms.Values) {
                room.Update();
                foreach (var e in room.events) {
                    e(this);
                }
            }
        }



        public LakeUser GetUser(ulong playerId) {
            return users[playerEntities[playerId].userId];
        }
        public DiscordChannel GetChannel(ulong playerId) {
            return discord.GetChannelAsync(GetUser(playerId).channelId).Result;
        }
        public async void Handle(DiscordMessage m) {
            string command = m.Content;

            string localPrefix = ".lake ";
            if (StartsWithRemove(ref command, ".lake")) {

            } else if (m.Channel.IsPrivate) {
                localPrefix = "";
            } else {
                if (m.MentionedUsers.Select(u => u.Id).Contains(discord.CurrentUser.Id)) {
                    m.Channel.SendMessageAsync($"{m.Author.Mention}, my prefix is `{prefix}`");
                }
                return;
            }



            Console.WriteLine($"{prefix}: " + command);
            var id = m.Author.Id;

            {
                if (users.TryGetValue(id, out LakeUser user)) {
                    user.channelId = m.ChannelId;
                }
            }

            string strLoginNow = $"You are not currently logged in. Use `{prefix} login {{characterName}}` to log in as a character, or use `{prefix} register {{characterName}}` to create a new one";
            string strRegisterNow = $"You do not have an account yet. Create your first character with `{localPrefix}register {{characterName}}`";

            switch (Subsplit(ref command, ' ')) {
                case "":
                case prefix: {
                        SendMessage("Welcome to SpongeLake! " +
                            (users.TryGetValue(id, out LakeUser user) && user.currentPlayerGuid != 0 ? $"You are currently logged in as {playerEntities[user.currentPlayerGuid].name}" :
                            user != null ? strLoginNow :
                            strRegisterNow));
                        break;
                    }
                case "help": {
                        SendMessage($"{m.Author.Mention}: `help` General");
                        break;
                    }
                case "login": {
                        if (IsRegistered(out LakeUser user)) {
                            if (command.Any()) {
                                var player = user.playerCharacters.Select(playerId => playerEntities[playerId]).FirstOrDefault(np => np.name == command);
                                if (player != null) {
                                    user.currentPlayerGuid = player.guid;
                                    SendMessage($@"{m.Author.Mention}: You are now logged in as `{player.name}`");
                                } else {
                                    SendMessage($@"{m.Author.Mention}: You have no character named `{command}`");
                                }
                            } else {
                                var loginList = string.Join(", ", user.playerCharacters.Select(playerId => $"`{playerEntities[playerId].name}`"));
                                var strLoggedIn = $"You are currently logged in as {playerEntities[user.currentPlayerGuid].name}";
                                SendMessage($"{m.Author.Mention}: Your logins:\n{loginList}\n{(user.currentPlayerGuid == 0 ? strLoginNow : strLoggedIn)}");
                            }
                        } else {
                            SendMessage($@"{m.Author.Mention}: {strRegisterNow}");
                        }
                        break;
                    }
                case "logout": {
                        if (IsLoggedIn(out LakePlayer player)) {
                            users[player.userId].currentPlayerGuid = 0;
                            SendMessage($@"{m.Author.Mention}, you are now logged out as character `{player.name}`");
                        }
                        break;
                    }
                case "look": {
                        if (IsRegistered(out LakeUser user)) {
                            if (user.currentPlayerGuid == 0) {
                                SendMessage($@"{m.Author.Mention}, you are a disembodied consciousness outside of the realm of SpongeLake. Take control of one of your player characters using `login`!");
                            } else {
                                var player = playerEntities[user.currentPlayerGuid];
                                if (rooms.TryGetValue(player.roomId, out var room)) {
                                    Describe(room);
                                } else {
                                    SendMessage($@"You appear to be in Null Space. Since you have no idea where you are, there's pretty much no other answer.");
                                }
                            }
                        }
                        break;

                    }
                case "elevator": {
                        if (IsLoggedIn(out LakePlayer player)) {
                            var room = rooms[player.roomId];
                            switch (Subsplit(ref command, ' ')) {
                                case "add": {
                                        if (room.elevator == null) {
                                            SendMessage($"Added elevator in room {room.id}");
                                            room.elevator = new LakeElevator();
                                        } else {
                                            SendMessage($"There is already an elevator in room {room.id}");
                                        }
                                        break;
                                    }
                                case "addfloor": {
                                        if (room.elevator == null) {
                                            SendMessage($"There is no elevator in room {room.id}");
                                        } else {
                                            string exitDest = Subsplit(ref command, ' ');
                                            room.elevator.floors.Add(new LakeElevator.LakeElevatorFloor() {
                                                name = $"Floor #{room.elevator.floors.Count + 1}",
                                                desc = $"The elevator is at floor #{room.elevator.floors.Count + 1}",
                                                exit = new NetExit() {
                                                    desc = "You exit the elevator.",
                                                    destRoomId = exitDest
                                                }
                                            });
                                            SendMessage($"Added floor leading to `{exitDest}` in elevator of room `{room.id}`");
                                        }
                                        break;
                                    }
                                case "push": {
                                        if (room.elevator == null) {
                                            SendMessage($"There is no elevator in room {room.id}");
                                        } else {
                                            if (int.TryParse(Subsplit(ref command, ' '), out int i)) {
                                                room.elevator.dest.Add(i);
                                            }
                                        }
                                        break;
                                    }
                                case "removefloor": {
                                        if (room.elevator == null) {
                                            SendMessage($"There is no elevator in room {room.id}");
                                        } else {
                                            string exitDest = Subsplit(ref command, ' ');
                                            if (room.elevator.floors.RemoveAll(f => f.exit.destRoomId == exitDest) > 0) {
                                                SendMessage($"Removed floor leading to `{exitDest}` in elevator of room {room.id}");
                                            } else {
                                                SendMessage($"There is no floor leading to `{exitDest}` in elevator of room {room.id}");
                                            }
                                        }
                                        break;
                                    }
                                case "floors": {
                                        if (room.elevator == null) {
                                            SendMessage($"There is no elevator in room {room.id}");
                                        } else {
                                            SendMessage(string.Join('\n', room.elevator.floors.Select(floor => $"**{floor.name}** - {floor.exit.destRoomId}")));
                                        }
                                        break;
                                    }
                                case "remove": {
                                        if (room.elevator == null) {
                                            SendMessage($"There is no elevator in room {room.id}");
                                        } else {
                                            SendMessage($"Removed elevator in room {room.id}");
                                            room.elevator = null;
                                        }
                                        break;
                                    }
                            }
                        }
                        break;
                    }
                case "exit": {
                        if (IsLoggedIn(out LakePlayer player)) {
                            switch (Subsplit(ref command, ' ')) {
                                case "add": {
                                        var room = rooms[player.roomId];
                                        string exitName = Subsplit(ref command, ' ');
                                        string destRoomId = Subsplit(ref command, ' ');
                                        string desc = command == destRoomId ? "" : command;
                                        room.exits[exitName] = new NetExit() {
                                            destRoomId = destRoomId,
                                            desc = desc
                                        };
                                        SendMessage($"Added exit `{exitName}` to room `{room.id}` leading to `{destRoomId}` with description `{desc}`");
                                        break;
                                    }
                                case "remove": {
                                        var room = rooms[player.roomId];
                                        string exitName = Subsplit(ref command, ' ');
                                        if (room.exits.TryGetValue(exitName, out NetExit e)) {
                                            room.exits.Remove(exitName);
                                            SendMessage($"Removed exit `{exitName}` from room `{room.id}` leading to `{e.destRoomId}`");
                                        } else {
                                            SendMessage($"There is no exit `{exitName}` in room `{room.id}`");
                                        }
                                        break;
                                    }
                            }
                        }
                        break;
                    }
                case "go": {
                        if (IsLoggedIn(out LakePlayer player)) {
                            string exit = command;
                            var room = rooms[player.roomId];
                            if (room.exits.TryGetValue(exit, out NetExit e)) {
                                player.roomId = e.destRoomId;
                                room.players.Remove(player.guid);
                                if (rooms.TryGetValue(e.destRoomId, out LakeRoom dest)) {
                                    dest.players.Add(player.guid);

                                    if (e.desc.Count() > 0) {
                                        SendMessage(e.desc);
                                    }
                                    Describe(dest);
                                } else {
                                    SendMessage($"Actually that doesn't lead anywhere?");
                                }
                            } else {
                                SendMessage($"Where do you think you're going???");
                            }
                        }
                        break;
                    }
                case "home": {
                        if (IsLoggedIn(out LakePlayer player)) {
                            var home = player.homeRoomId;
                            if (rooms.TryGetValue(home, out LakeRoom room)) {
                                player.roomId = home;
                                SendMessage($"{player.name} warps home to room `{home}`!");
                            } else {
                                SendMessage($"{player.name} tries to warp home to room `{home}`, but they are homeless!");
                            }
                        }
                        break;
                    }
                case "register": {
                        string autoLogin = $"Use `{localPrefix}login {{characterName}}` or click :ok: to login now.";

                        Task<DiscordMessage> msgTask;
                        if (users.TryGetValue(id, out LakeUser user)) {

                            if(playerEntities[user.currentPlayerGuid].name == command) {
                                msgTask = SendMessage($@"{m.Author.Mention}, you are already logged in as `{command}`.");
                                return;
                            } else if (user.GetPlayers(this).Any(p => p.name == command)) {
                                msgTask = SendMessage($@"{m.Author.Mention}, you already have a new character named `{command}`. {autoLogin}");
                                AutoLogin();
                                return;
                            } else {
                                msgTask = SendMessage($@"OK, {m.Author.Mention}, you have created a new character named `{command}`. {autoLogin}");
                                AutoLogin();
                            }
                        } else {
                            users[id] = user = new LakeUser(m.Author.Id);
                            msgTask = SendMessage($@"{m.Author.Mention}, welcome to SpongeLake! You have created a new character named `{command}`. {autoLogin}");
                            AutoLogin();

                        }

                        async Task AutoLogin() {
                            var msg = await msgTask;

                            await msg.CreateReactionAsync(DiscordEmoji.FromName(discord, ":ok:"));
                            discord.MessageReactionAdded += Login;

                            async Task Login(MessageReactionAddEventArgs args) {
                                if (args.Message.Id != msg.Id) {
                                    return;
                                } else if (args.Emoji.GetDiscordName() != ":ok:") {
                                    return;
                                } else if (args.User.Id != user.userId) {
                                    return;
                                }

                                var player = user.playerCharacters.Select(playerId => playerEntities[playerId]).FirstOrDefault(np => np.name == command);
                                if (player != null) {
                                    user.currentPlayerGuid = player.guid;
                                    SendMessage($@"{m.Author.Mention}: You are now logged in as `{player.name}`");
                                }
                                discord.MessageReactionAdded -= Login;
                            }
                        }

                        {
                            //Get a GUID for this player entity
                            ulong guid = CreateGuid();
                            //Create the player
                            var player = new LakePlayer() {
                                guid = guid,
                                name = command,
                                userId = id,
                                roomId = $"home_{guid}",
                            };
                            player.homeRoomId = player.roomId;

                            //Add this player to the player map
                            playerEntities[guid] = player;

                            //Add this player to the user's players
                            user.playerCharacters.Add(guid);

                            //Create a home for this player
                            CreateHome(player);
                        }
                        break;
                    }

                case "room": {
                        if (IsLoggedIn(out LakePlayer player)) {
                            switch (Subsplit(ref command, ' ')) {
                                case "create": {
                                        if (rooms.TryGetValue(command, out LakeRoom room)) {
                                            SendMessage($"Room `{room.id}` already exists");
                                        } else {
                                            rooms[command] = room = new LakeRoom() {
                                                id = command,
                                                name = "New Room",
                                                description = "There's not much to be said about this room, but at least it exists."
                                            };
                                            SendMessage($"Created room `{room.id}`");
                                        }
                                        break;
                                    }
                                case "name": {
                                        var room = rooms[player.roomId];
                                        room.name = command;
                                        SendMessage($"Set the name of room `{room.id}` to {command}");
                                        break;
                                    }
                                case "desc": {
                                        var room = rooms[player.roomId];
                                        room.name = command;
                                        SendMessage($"Set the description of room `{room.id}` to {command}");
                                        break;
                                    }
                                case "delete":
                                    rooms.Remove(player.roomId);
                                    break;
                            }
                            bool RoomExists(string roomId, out LakeRoom room) {
                                return rooms.TryGetValue(roomId, out room);
                            }
                        }

                        break;
                    }
                case "warp": {
                        if (IsLoggedIn(out LakePlayer player)) {
                            if (rooms.TryGetValue(command, out LakeRoom room)) {
                                player.roomId = command;
                                SendMessage($"{player.name} warps to room `{room.id}`!");
                            } else {
                                SendMessage($"{player.name} daydreams about warping to a room known as `{command}`!");
                            }
                        }
                        break;
                    }

            }

            void Describe(LakeRoom room) {
                SendMessage($"**{room.name}** (`{room.id}`)\n\n{room.description}\n\nObvious exits:{string.Join(' ', room.exits.Keys.Select(e => $"`{e}`"))}");
            }
            bool IsRegistered(out LakeUser user) {
                if (users.TryGetValue(id, out user)) {
                    return true;
                } else {
                    return false;
                }
            }
            bool IsLoggedIn(out LakePlayer player) {
                bool result = IsRegistered(out LakeUser user) && user.currentPlayerGuid != 0;
                if(result) {
                    player = playerEntities[user.currentPlayerGuid];
                } else {
                    player = null;
                    SendMessage($@"{m.Author.Mention}, you are not logged in." + user != null ? strLoginNow : strRegisterNow);
                }
                return result;
            }
            Task<DiscordMessage> SendMessage(string message) {
                return m.Channel.SendMessageAsync($"`{prefix}`: " + message);
            }
            void CreateHome(LakePlayer player) {
                rooms[player.roomId] = new LakeRoom() {
                    id = player.roomId,
                    name = $"Headspace of {player.name}",
                    description = $"This is a pocket dimension created by the mind of the player known as {player.name}."
                };
                rooms[player.roomId].players.Add(player.guid);
            }
        }
    }
}
