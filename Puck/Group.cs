using DSharpPlus.Entities;

using System;
using System.Collections.Generic;

namespace Puck {
	class Group {
		public readonly Type type;
		public int tank, heal, dps; // "any" is treated as dps

		public const Type default_type = Type.Dungeon;

		public enum Role {
			Tank, Heal, Dps,
		};

		public enum Type {
			Dungeon,
			Raid, Warfront,
			Scenario, Island,
			Vision,
			Other = -1,
		};

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
				{ Type.Scenario, new List<string> {
					"scenario",
					"scenarios",
				} },
				{ Type.Island, new List<string> {
					"island",
					"islands",
				} },
				{ Type.Vision, new List<string> {
					"vision",
					"visions",
					"hv",
				} },
				{ Type.Other, new List<string> {
					"other",
					"miscellaneous",
					"misc",
				} },
			};

		public Group(Type type) :
			this(0, 0, 0, type) { }
		public Group(int tank, int heal, int dps, Type type) {
			this.type = type;
			this.tank = tank;
			this.heal = heal;
			this.dps = dps;
		}

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

		public int members() {
			return tank + heal + dps;
		}

		public override string ToString() {
			string str = "";
			string box_empty = "\u2610";
			string box_checked = "\u2611\uFE0E";
			string separator = "\u2003";
			string emoji_tank = Program.GetEmoji(Role.Tank)?.ToString() ?? "";
			string emoji_heal = Program.GetEmoji(Role.Heal)?.ToString() ?? "";
			string emoji_dps  = Program.GetEmoji(Role.Dps )?.ToString() ?? "";

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
				str += emoji_tank + ": ";
				str += tank.ToString();

				str += separator;
				str += emoji_heal + ": ";
				str += heal.ToString();

				str += separator;
				str += emoji_dps + ": ";
				str += dps.ToString();
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
			case Type.Other:
				str += "group size: ";
				str += members();
				break;
			}

			return str;
		}

		public static Type ParseType(string command)
			{ return dict_commands_cache[command]; }

		static Group() {
			Dictionary<string, Type> dict = new Dictionary<string, Type>();
			foreach (Type type in dict_commands.Keys) {
				foreach (string command in dict_commands[type]) {
					dict.Add(command, type);
				}
			}
			dict_commands_cache = dict;
		}
	}
}
