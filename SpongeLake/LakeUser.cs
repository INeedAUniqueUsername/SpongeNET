using System.Collections.Generic;
using System.Linq;

namespace SpongeLake.SpongeLake {
    public class LakeUser {
        public ulong userId;
        public ulong channelId;
        public HashSet<ulong> playerCharacters = new HashSet<ulong>();
        public ulong currentPlayerGuid;
        public LakeUser(ulong userId) {
            this.userId = userId;
        }
        public IEnumerable<LakePlayer> GetPlayers(Lake lake) => playerCharacters.Select(playerId => lake.playerEntities[playerId]);
    }
    public class LakePlayer : NetEntity {
        public ulong userId;
        public ulong guid;
        public string name;
        public string roomId;
        public string homeRoomId;
    }
}
