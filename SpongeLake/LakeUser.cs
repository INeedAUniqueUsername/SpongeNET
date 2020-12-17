using System.Collections.Generic;
using System.Linq;

namespace Quipcord.SpongeLake {
    public class LakeUser {
        public ulong userId;
        public ulong channelId;
        public HashSet<LakePlayer> playerCharacters = new HashSet<LakePlayer>();
        public LakePlayer currentPlayer;
        public LakeUser(ulong userId) {
            this.userId = userId;
        }
    }
    public class LakePlayer : NetEntity {
        public ulong userId;
        public ulong guid;
        public string name;
        public string roomId;
        public string homeRoomId;
    }
}
