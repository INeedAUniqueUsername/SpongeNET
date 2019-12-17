using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using static SpongeNET.Helper;
namespace SpongeNET.SpongeNET {
    public class Net {
        private DiscordClient discord;
        public Dictionary<ulong, NetUser> users = new Dictionary<ulong, NetUser>();
        public Dictionary<string, NetRoom> rooms = new Dictionary<string, NetRoom>();
        public Dictionary<ulong, NetPlayer> playerEntities = new Dictionary<ulong, NetPlayer>();

        public ulong guidCounter = 0;
        public ulong getGuid() {
            guidCounter++;
            return guidCounter;
        }
        public Net(DiscordClient discord) {
            this.discord = discord;
        }
        public void Load(DiscordClient discord) {
            this.discord = discord;
        }
        public void Update() {
            foreach(var room in rooms.Values) {
                room.Update();
                foreach(var e in room.events) {
                    e(this);
                }
            }
        }
        public NetUser GetUser(ulong playerId) {
            return users[playerEntities[playerId].userId];
        }
        public DiscordChannel GetChannel(ulong playerId) {
            return discord.GetChannelAsync(GetUser(playerId).channelId).Result;
        }
        public async void Handle(DiscordMessage m) {
            string command = m.Content;
            if (!m.Channel.IsPrivate && Subsplit(ref command, ' ') != ".net") {
                return;
            }
            Console.WriteLine(".net: " + command);
            var id = m.Author.Id;
            {
                if (users.TryGetValue(id, out NetUser user)) {
                    user.channelId = m.ChannelId;
                }
            }

            switch (Subsplit(ref command, ' ')) {
                case ".net": {
                        SendMessage("Welcome to SpongeNET!" +
                            (users.TryGetValue(id, out NetUser user) && user.currentPlayerGuid != 0  ? $"You are currently logged in as {playerEntities[user.currentPlayerGuid].name}" :
                            user != null ? "You are not currently logged in" :
                            "You do not have an account yet."));
                        break;
                    }
                case "register": {
                        if (users.TryGetValue(id, out NetUser user)) {
                            SendMessage($@"OK, {m.Author.Mention}, you have created a new character named ""{command}""");
                        } else {
                            SendMessage($@"{m.Author.Mention}, welcome to SpongeNET! You have created a new character named ""{command}""");
                            users[id] = user = new NetUser(m.Author.Id);
                        }
                        //Get a GUID for this player entity
                        ulong guid = getGuid();
                        //Create the player
                        var player = new NetPlayer() {
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
                        break;
                    }
                case "login": {
                        if (IsRegistered(out NetUser user)) {
                            var player = user.playerCharacters.Select(playerId => playerEntities[playerId]).FirstOrDefault(np => np.name == command);
                            user.currentPlayerGuid = player.guid;

                            SendMessage($@"{m.Author.Mention}, you are now logged in as ""{player.name}""");
                        }
                        break;
                    }
                case "logout": {
                        if (IsLoggedIn(out NetPlayer player)) {
                            users[player.userId].currentPlayerGuid = 0;
                            SendMessage($@"{m.Author.Mention}, you are now logged out of ""{player.name}""");
                        }
                        break;
                    }
                case "look": {
                        if(IsRegistered(out NetUser user)) {
                            if(user.currentPlayerGuid == 0) {
                                SendMessage($@"{m.Author.Mention}, you are a disembodied consciousness outside of the realm of SpongeNET. Take control of one of your player characters using `login`!");
                            } else {
                                var player = playerEntities[user.currentPlayerGuid];
                                var room = rooms[player.roomId];
                                Describe(room);
                            }
                        }
                        break;

                    }
                case "warp": {
                        if(IsLoggedIn(out NetPlayer player)) {
                            if(rooms.TryGetValue(command, out NetRoom room)) {
                                player.roomId = command;
                                SendMessage($"{player.name} warps to room `{room.id}`!");
                            } else {
                                SendMessage($"{player.name} daydreams about warping to a room known as `{room.id}`!");
                            }
                        }
                        break;
                    }
                case "elevator": {
                        if(IsLoggedIn(out NetPlayer player)) {
                            var room = rooms[player.roomId];
                            switch (Subsplit(ref command, ' ')) {
                                case "add": {
                                        if(room.elevator == null) {
                                            SendMessage($"Added elevator in room {room.id}");
                                            room.elevator = new NetElevator();
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
                                            room.elevator.floors.Add(new NetElevator.NetElevatorFloor() {
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
                                            if(int.TryParse(Subsplit(ref command, ' '), out int i)) {
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
                                            if(room.elevator.floors.RemoveAll(f => f.exit.destRoomId == exitDest) > 0) {
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
                case "go": {
                        if (IsLoggedIn(out NetPlayer player)) {
                            string exit = command;
                            var room = rooms[player.roomId];
                            if (room.exits.TryGetValue(exit, out NetExit e)) {
                                player.roomId = e.destRoomId;
                                room.players.Remove(player.guid);
                                rooms[e.destRoomId].players.Add(player.guid);

                                if(e.desc.Count() > 0) {
                                    SendMessage(e.desc);
                                }
                                Describe(rooms[e.destRoomId]);
                            }
                        }
                        break;
                    }
                case "exit": {
                        if(IsLoggedIn(out NetPlayer player)) {
                            switch(Subsplit(ref command, ' ')) {
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
                                        if(room.exits.TryGetValue(exitName, out NetExit e)) {
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
                case "room": {
                        if(IsLoggedIn(out NetPlayer player)) {
                            switch (Subsplit(ref command, ' ')) {
                                case "create": {
                                        if (rooms.TryGetValue(command, out NetRoom room)) {
                                            SendMessage($"Room `{room.id}` already exists");
                                        } else {
                                            rooms[command] = room = new NetRoom() {
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
                            bool RoomExists(string roomId, out NetRoom room) {
                                return rooms.TryGetValue(roomId, out room);
                            }
                        }
                        
                        break;
                    }
            }
            void Describe(NetRoom room) {
                SendMessage($"**{room.name}** (`{room.id}`)\n\n{room.description}\n\nObvious exits:{string.Join(' ', room.exits.Keys.Select(e => $"`{e}`"))}");
            }
            bool IsRegistered(out NetUser user) {
                if (users.TryGetValue(id, out user)) {
                    return true;
                } else {
                    SendMessage($@"{m.Author.Mention}, you do not have a SpongeNET account yet.");
                    return false;
                }
            }
            bool IsLoggedIn(out NetPlayer player) {
                bool result = IsRegistered(out NetUser user) && user.currentPlayerGuid != 0;
                player = result ? playerEntities[user.currentPlayerGuid] : null;
                if(!result) {
                    SendMessage($@"{m.Author.Mention}, you are not logged in.");
                }
                return result;
            }
            void SendMessage(string message) {
                m.Channel.SendMessageAsync("`.net`: " + message);
            }
            void CreateHome(NetPlayer player) {
                rooms[player.roomId] = new NetRoom() {
                    id = player.roomId,
                    name = $"Headspace of {player.name}",
                    description = $"This is a pocket dimension created by the mind of the player known as {player.name}."
                };
                rooms[player.roomId].players.Add(player.guid);
            }
        }
    }
    public interface NetEntity {

    }
    public class NetUser {
        public ulong userId;
        public ulong channelId;
        public HashSet<ulong> playerCharacters = new HashSet<ulong>();
        public ulong currentPlayerGuid;
        public NetUser(ulong userId) {
            this.userId = userId;
        }
    }
    public class NetPlayer : NetEntity {
        public ulong userId;
        public ulong guid;
        public string name;
        public string roomId;
        public string homeRoomId;
    }
    public class NetRoom {
        public string id;
        public string name;
        public string description;
        public HashSet<ulong> players = new HashSet<ulong>();
        public HashSet<ulong> items = new HashSet<ulong>();
        public HashSet<ulong> entities = new HashSet<ulong>();
        public Dictionary<string, NetExit> exits = new Dictionary<string, NetExit>();
        public NetElevator elevator;
        public HashSet<Action<Net>> events = new HashSet<Action<Net>>();

        public void Update() {
            events.Clear();
            elevator?.Update();
            if(elevator != null) {
                if (elevator.moved) {
                    var floor = elevator.floors[elevator.currentFloor];
                    events.Add(net => {
                        foreach(var playerId in players) {
                            net.GetChannel(playerId).SendMessageAsync($"The elevator moves to **{floor.name}**");
                        }
                    });
                }
                if(elevator.stopped) {
                    events.Add(net => {
                        foreach (var playerId in players) {
                            net.GetChannel(playerId).SendMessageAsync($"The elevator stops. The elevator door opens.");
                        }
                    });
                    var floor = elevator.floors[elevator.currentFloor];
                    exits["out"] = floor.exit;
                    description = floor.desc;
                }
                if (elevator.started) {
                    events.Add(net => {
                        foreach (var playerId in players) {
                            net.GetChannel(playerId).SendMessageAsync($"The elevator door closes. The elevator starts going {(elevator.goingUp ? "up" : "down")}");
                        }
                    });
                    exits.Remove("out");
                }

            }
        }
    }
    public class NetElevator {
        public List<NetElevatorFloor> floors = new List<NetElevatorFloor>();
        public int currentFloor = 0;
        public HashSet<int> dest = new HashSet<int>();
        public bool goingUp = true;
        public HashSet<int> requestsUp = new HashSet<int>();
        public HashSet<int> requestsDown = new HashSet<int>();
        public bool moved = false;
        public bool stopped = false;
        public bool started = false;
        public ulong timeUntilMove = 6;

        public void Update() {
            moved = false;
            started = false;

            if (dest.Count() == 0 && requestsUp.Count() == 0 && requestsDown.Count() == 0) {
                stopped = false;
            } else if (timeUntilMove > 0) {
                timeUntilMove--;
                stopped = false;
            } else if(stopped) {
                started = true;
                stopped = false;
                timeUntilMove = 2;

                if(goingUp && !ShouldGoUp() && ShouldGoDown()) {
                    goingUp = false;
                } else if(ShouldGoUp() && !ShouldGoDown()) {
                    goingUp = true;
                }
            } else if(goingUp) {

                if (requestsUp.Contains(currentFloor)) {
                    requestsUp.Remove(currentFloor);
                    //Stop here
                    stopped = true;
                    timeUntilMove = 6;
                }
                if (dest.Contains(currentFloor)) {
                    dest.Remove(currentFloor);
                    //Stop here
                    stopped = true;
                    timeUntilMove = 6;
                } else if (ShouldGoUp()) {
                    //Keep going up
                    currentFloor++;
                    moved = true;
                    timeUntilMove = 2;
                } else {
                    goingUp = false;
                }
            } else {
                if (requestsDown.Contains(currentFloor)) {
                    requestsDown.Remove(currentFloor);
                    //Stop here
                    timeUntilMove = 6;
                }
                if (dest.Contains(currentFloor)) {
                    dest.Remove(currentFloor);
                    //Stop here
                    stopped = true;
                    timeUntilMove = 6;
                } else if (ShouldGoDown()) {
                    //Keep going down
                    currentFloor--;
                    moved = true;
                    timeUntilMove = 2;
                } else {
                    goingUp = true;
                    if(requestsUp.Contains(currentFloor)) {
                        //Stop here
                        stopped = true;
                        timeUntilMove = 6;
                    }
                }
            }
            bool ShouldGoUp() => dest.Any(f => f > currentFloor) || requestsUp.Any(f => f > currentFloor) || requestsDown.Any(f => f > currentFloor);
            bool ShouldGoDown() => dest.Any(f => f < currentFloor) || requestsUp.Any(f => f < currentFloor) || requestsDown.Any(f => f < currentFloor);
        }
        public class NetElevatorFloor {
            public NetExit exit;
            public string name;
            public string desc;
        }
    }
    
    public class NetExit {
        public string destRoomId;
        public string desc;
    }
}
