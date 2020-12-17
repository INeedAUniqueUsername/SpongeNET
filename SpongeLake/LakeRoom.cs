using System;
using System.Collections.Generic;
namespace Quipcord.SpongeLake {
    public class LakeRoom {
        public string id;
        public string name;
        public string description;
        public HashSet<ulong> players = new HashSet<ulong>();
        public HashSet<ulong> items = new HashSet<ulong>();
        public HashSet<ulong> entities = new HashSet<ulong>();
        public Dictionary<string, NetExit> exits = new Dictionary<string, NetExit>();
        public HashSet<Action<Lake>> events = new HashSet<Action<Lake>>();
        public LakeElevator elevator;

        public void Update() {
            events.Clear();
            elevator?.Update(this);
            if (elevator != null) {

            }
        }
    }
}
