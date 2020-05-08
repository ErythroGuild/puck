using System;
using System.Collections.Generic;

namespace Puck {
	class Group {
		public enum Type {
			Dungeon,
			Raid, Warfront,
			Arenas,
			RBG, Battleground,
			Vision,
			Scenario, Island,
			Other = -1,
		};

		public enum Role {
			Tank, Heal, Dps,
		};

		public const Type default_type = Type.Dungeon;

		// lookup table for command parsing of group type
		static readonly Dictionary<string, Type> dict_commands_cache;
		static readonly Dictionary<Type, List<string>> dict_commands =
			new Dictionary<Type, List<string>> {
				{ Type.Dungeon, new List<string> {
					"dungeon",
					"dungeons",
					"mythics",
					"m0",
					"key",
					"keys",
					"keystone",
					"keystones",
					"m+",
					"ksm",
				} },
				{ Type.Raid, new List<string> {
					"raid",
					"raids",
				} },
				{ Type.Warfront, new List<string> {
					"warfront",
					"warfronts",
					"wf",
				} },
				{ Type.Arenas, new List<string> {
					"arenas",
					"arena",
					"2v2",
					"2v2s",
					"2vs2",
					"2vs2s",
					"2s",
					"3v3",
					"3v3s",
					"3vs3",
					"3vs3s",
					"3s",
				} },
				{ Type.RBG, new List<string> {
					"rbg",
					"rbgs",
					"ratedbg",
					"ratedbgs",
					"ratedbattleground",
					"ratedbattlegrounds",
					"10v10",
					"10v10s",
					"10s",
				} },
				{ Type.Battleground, new List<string> {
					"battleground",
					"battlegrounds",
					"bg",
					"bgs",
					"brawl",
				} },
				{ Type.Vision, new List<string> {
					"vision",
					"visions",
					"hv",
				} },
				{ Type.Scenario, new List<string> {
					"scenario",
					"scenarios",
				} },
				{ Type.Island, new List<string> {
					"island",
					"islands",
				} },
				{ Type.Other, new List<string> {
					"other",
					"miscellaneous",
					"misc",
				} },
			};

		// Parse a command to a strongly typed Type.
		public static Type ParseType(string command) { return dict_commands_cache[command]; }
		// Initialize the (sparse) lookup table for accepted commands.
		static Group() {
			Dictionary<string, Type> dict = new Dictionary<string, Type>();
			foreach (Type type in dict_commands.Keys) {
				foreach (string command in dict_commands[type]) {
					dict.Add(command, type);
				}
			}
			dict_commands_cache = dict;
		}



		public readonly Type type;
		public int tank, heal, dps; // "any" is treated as dps

		// Constructors. Group.Type *must* always be specified.
		public Group(Type type) :
			this(0, 0, 0, type) { }
		public Group(int tank, int heal, int dps, Type type) {
			this.type = type;
			this.tank = tank;
			this.heal = heal;
			this.dps = dps;
		}

		// Getters and setters for data members.
		public void Set(Role role, int n) {
			switch (role) {
			case Role.Tank:
				tank = n;
				break;
			case Role.Heal:
				heal = n;
				break;
			case Role.Dps:
				dps = n;
				break;
			}
		}
		public int Get(Role role) {
			return role switch {
				Role.Tank => tank,
				Role.Heal => heal,
				Role.Dps  => dps,
				_ => throw new ArgumentException(),
			};
		}

		// Returns the total size of the group.
		public int members() {
			return tank + heal + dps;
		}

		// Doubles as the DiscordMessage string used for the bulletin.
		public override string ToString() {
			string str = "";
			string box_empty = "\u2610";
			string box_checked = "\u2611\uFE0E";
			string separator = "\u2003";
			string emoji_tank = Emoji.From(Role.Tank).ToString();
			string emoji_heal = Emoji.From(Role.Heal).ToString();
			string emoji_dps  = Emoji.From(Role.Dps ).ToString();

			int total = members();
			int counted = 0;
			switch (type) {
			case Type.Dungeon:
				str += (tank == 0) ? box_empty : box_checked;
				str += emoji_tank;

				str += separator;
				str += (heal == 0) ? box_empty : box_checked;
				str += emoji_heal;

				for (int i=1; i<=3; i++) {
					str += separator;
					str += (dps < i) ? box_empty : box_checked;
					str += emoji_dps;
				}
				break;
			case Type.Raid:
			case Type.Warfront:
			case Type.RBG:
			case Type.Battleground:
				str += emoji_tank + ": ";
				str += tank.ToString();

				str += separator;
				str += emoji_heal + ": ";
				str += heal.ToString();

				str += separator;
				str += emoji_dps + ": ";
				str += dps.ToString();
				break;
			case Type.Arenas:
				for (int i = 0; i < tank && counted < 3; i++, counted++) {
					if (counted > 0)
						str += separator;
					str += emoji_tank;
				}

				for (int i = 0; i < heal && counted < 3; i++, counted++) {
					if (counted > 0)
						str += separator;
					str += emoji_heal;
				}

				for (int i = 0; i < dps && counted < 3; i++, counted++) {
					if (counted > 0)
						str += separator;
					str += emoji_dps;
				}
				break;
			case Type.Vision:
				for (int i = 0; i < tank && counted < 5; i++, counted++) {
					if (counted > 0)
						str += separator;
					str += emoji_tank;
				}

				for (int i = 0; i < heal && counted < 5; i++, counted++) {
					if (counted > 0)
						str += separator;
					str += emoji_heal;
				}

				for (int i = 0; i < dps && counted < 5; i++, counted++) {
					if (counted > 0)
						str += separator;
					str += emoji_dps;
				}
				break;
			case Type.Scenario:
			case Type.Island:
				for (int i = 1; i <= 3; i++) {
					if (i > 1)
						str += separator;
					str += (total < i) ? box_empty : box_checked;
					str += emoji_dps;
				}
				break;
			case Type.Other:
				str += "group size: ";
				str += members();
				break;
			}

			return str;
		}
	}
}
