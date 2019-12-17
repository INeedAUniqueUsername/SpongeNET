using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using static SpongeNET.PlayerStats;

namespace SpongeNET
{
    class PlayerStats
    {
        public enum Shtyle
        {
            MUNDANE
        }
        public Shtyle shtyle;
        public int speed;
        public enum Status
        {
            NORMAL
        }
        public Status status;
        public int hp;
        public int accessLevel;
        public HashSet<int> unlockedTitles;

        public PlayerStats(dynamic data)
        {
            try { shtyle = Enum.Parse<Shtyle>(data.shtyle); }
            catch { shtyle = Shtyle.MUNDANE; }
            speed = 120;
            status = Status.NORMAL;
            hp = 40;
            accessLevel = 10;
            unlockedTitles = new HashSet<int> { 1 };

            maxStamina = this.stats.maxStamina || cons.DEFAULTS.stamina.max;
            stamina = this.stats.stamina || this.stats.maxStamina;
            staminaPerTick = this.stats.staminaPerTick || cons.DEFAULTS.stamina.perTick;
            maxHp = this.stats.maxHp || cons.DEFAULTS.hp.max;
            baseMaxHp = this.stats.maxHp || cons.DEFAULTS.hp.max;
            hpPerTick = this.stats.hpPerTick || cons.DEFAULTS.hp.perTick;
            this.stats.moveCost = this.stats.moveCost || cons.DEFAULTS.stamina.moveCost;
            this.stats.gatherPoints = this.stats.gatherPoints || cons.DEFAULTS.gather.maxPts;

        }

        public void FromData(dynamic data)
        {
            
            speed = int.Parse(data.speed);
            status = Enum.Parse<Status>(data.status);
            hp = int.Parse(data.hp);
            accessLevel = int.Parse(data.accessLevel);
            unlockedTitles = JArray.Parse(data.unlockedTitles);
        }
    }
    class Player
    {
        string location;
        HashSet<ulong> inventory;
        string charName;
        PlayerStats stats;
        public Player(dynamic data)
        {
            this.location = data.location || "airport";

	        this.inventory = data.inventory ?? new HashSet<ulong>();
	
	        this.charName = data.charName ?? "Anonymous";
	        this.stats = new PlayerStats(
		        shtyle: Shtyle.MUNDANE,
		        speed: 120,
		        status: Status.NORMAL,
		        hp: 40,
		        accessLevel: 10,
		        unlockedTitles: new HashSet<int> { 1 }
	        );
            this.stats.FromData(data.stats);
	        this.server = data.server;
	        this.isRepping = data.isRepping;
	        this.stats.maxStamina = this.stats.maxStamina || cons.DEFAULTS.stamina.max;
	        this.stats.stamina = this.stats.stamina || this.stats.maxStamina;
	        this.stats.staminaPerTick = this.stats.staminaPerTick || cons.DEFAULTS.stamina.perTick;
	        this.stats.maxHp = this.stats.maxHp || cons.DEFAULTS.hp.max;
	        this.stats.baseMaxHp = this.stats.maxHp || cons.DEFAULTS.hp.max;
	        this.stats.hpPerTick = this.stats.hpPerTick || cons.DEFAULTS.hp.perTick;
	        this.stats.moveCost = this.stats.moveCost || cons.DEFAULTS.stamina.moveCost;
	        this.stats.gatherPoints = this.stats.gatherPoints || cons.DEFAULTS.gather.maxPts;
	        this.traits = data.traits || {
		        lightSleeper: false
	        };
	        this.stats.unlockedTitles = this.stats.unlockedTitles || [1];
	        this.posture = data.posture || "asleep";
	        this.id = data.id;
	        this.title = data.title;
	        this.age = data.age || 0;
	        this.idle = data.idle || {
		        ticks: 0,
		        threshhold: 45,
		        autolog: true,
		        warn: true
		        };
	        this.description = data.description || "a brave MUD tester";
	        this.timers = {
		        "nextAttack": 0,
		        "nextMove": 0
	        };
	        this.recallPoint = data.recallPoint;
	        this.privacyFlags = data.privacyFlags;
	        this.modFlags = data.modFlags;
	        this.isMuted = data.isMuted;
        }
    }
}
