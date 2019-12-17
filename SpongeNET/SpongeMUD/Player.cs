using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using static SpongeNET.PlayerStats;
using static SpongeNET.Helper;
using static SpongeNET.PlayerStats.Shtyle;
using static SpongeNET.PlayerStats.Status;
namespace SpongeNET {
    class PlayerTraits {
        public bool lightSleeper;
        public PlayerTraits(dynamic data) {
            lightSleeper = Try(data, "lightSleeper", false);
        }
    }
    class PlayerStats {
        static class Default {
            public static Shtyle shtyle = MUNDANE;
            public static int speed = 120;
            public static Status status = NORMAL;
            public static int hp = 40;
            public static int accessLevel = 10;
            public static HashSet<int> unlockedTitles => new HashSet<int> { 1 };
            public static int staminaMax = 200;
            public static int staminaPerTick = 40;
            public static int staminaMoveCost = 30;
            public static int hpMax = 40;
            public static int hpPerTick = 6;
            public static int gatherMax = 20;
            public static int xpMaxTicks = 60;
            public static int xpPerTick = 1;

        }
        public enum Shtyle {
            MUNDANE
        }
        public enum Status {
            NORMAL
        }
        public Shtyle shtyle;
        public int speed;
        public Status status;
        public int hp;
        public int accessLevel;
        public HashSet<int> unlockedTitles;
        public int maxStamina;
        public int stamina;
        public int staminaPerTick;
        public int maxHp;
        public int baseMaxHp;
        public int hpPerTick;
        public int moveCost;
        public int gatherPoints;

        public PlayerStats(dynamic data) {
            shtyle = TryEnum(data, "shtyle", Default.shtyle);
            speed = Try(data, "speed", Default.speed);
            status = Try(data, "status", Default.status);
            hp = Try(data, "hp", Default.hp);
            accessLevel = Try(data, "accessLevel", Default.accessLevel);
            unlockedTitles = Try(data, "unlockedTitles", Default.unlockedTitles);

            maxStamina = Try(data, "maxStamina", Default.staminaMax);
            stamina = Try(data, "stamina", maxStamina);
            staminaPerTick = Try(data, "staminaPerTick", Default.staminaPerTick);
            maxHp = Try(data, "maxHp", Default.hpMax);
            baseMaxHp = Try(data, "maxHp", maxHp);
            hpPerTick = Try(data, "hpPerTick", Default.hpPerTick);
            moveCost = Try(data, "moveCost", Default.staminaMoveCost);
            gatherPoints = Try(data, "gatherPoints", Default.gatherMax);
        }
    }
    class Idle {
        int ticks;
        int threshold;
        bool autolog;
        bool warn;
        public Idle(dynamic data) {
            ticks = Try(data, "ticks", 0);
            threshold = Try(data, "threshold", 45);
            autolog = Try(data, "autolog", true);
            warn = Try(data, "warn", true);
        }
    }
    class Player {
        string location;
        HashSet<ulong> inventory;
        string charName;
        PlayerStats stats;
        PlayerTraits traits;
        string server;
        bool isRepping;
        enum Posture {
            SLEEPING
        }
        Posture posture;
        string id;
        string title;
        int age;
        Idle idle;
        string description;
        Dictionary<string, int> timers;
        string recallPoint;
        //PrivacyFlags privacyFlags;
        public Player(dynamic data) {
            location = Try(data, "location", "airport");
            inventory = Try(data, "inventory", new HashSet<ulong>());

            charName = data.charName ?? "Anonymous";
            stats = new PlayerStats(data);
            traits = new PlayerTraits(data);
            server = Try(data, "server", "");
            isRepping = Try(data, "isRepping", false);
            posture = TryEnum<Posture>(data, "posture", Posture.SLEEPING);
            id = data.id;
            title = data.title;
            age = Try(data, "age", 0);
            idle = new Idle(data);
            description = Try(data, "description", "a brave MUD tester");
            timers = new Dictionary<string, int>
            {
                {"nextAttack", 0 },
                {"nextMove", 0 }
            };
            recallPoint = data.recallPoint;
            /*
            privacyFlags = data.privacyFlags;
            modFlags = data.modFlags;
            isMuted = data.isMuted;
            */
        }
    }
}
