using DSharpPlus.Entities;

using System.Collections.Generic;

namespace Puck {
	class Group {
		public readonly Type type;
		public int tank, heal, dps; // "any" is treated as dps

		public const Type default_type = Type.Dungeon;

		public enum Type {
			Dungeon,
			Raid, Warfront,
			Scenario, Island,
			Vision,
			Other = -1,
		};

		private static readonly Dictionary<string, Type> dict_commands_cache;
		private static readonly Dictionary<Type, List<string>> dict_commands =
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

		public Group(Type type) {
			this.type = type;
			tank = 0;
			heal = 0;
			dps = 0;
		}
		public Group(int tank, int heal, int dps, Type type) {
			this.type = type;
			this.tank = tank;
			this.heal = heal;
			this.dps = dps;
		}

		public int members() {
			return tank + heal + dps;
		}

		override public string ToString() {
			string str = "";
			string box_empty = "\u2610";
			string box_checked = "\u2611\uFE0E";
			string separator = "\u2003";

			switch (type) {
			case Type.Dungeon:
				str += (tank == 0) ? box_empty : box_checked;
				str += emoji_tank().ToString();

				str += separator;
				str += (heal == 0) ? box_empty : box_checked;
				str += emoji_heal().ToString();

				for (int i=1; i<=3; i++) {
					str += separator;
					str += (dps < i) ? box_empty : box_checked;
					str += emoji_dps().ToString();
				}
				break;
			case Type.Raid:
			case Type.Warfront:
				str += emoji_tank().ToString() + ": ";
				str += tank.ToString();

				str += separator;
				str += emoji_heal().ToString() + ": ";
				str += heal.ToString();

				str += separator;
				str += emoji_dps().ToString() + ": ";
				str += dps.ToString();
				break;
			case Type.Scenario:
			case Type.Island:
				int total = members();
				for (int i = 1; i <= 3; i++) {
					if (i > 1)
						str += separator;
					str += (total < i) ? box_empty : box_checked;
					str += emoji_dps().ToString();
				}
				break;
			case Type.Vision:
				int counted = 0;

				for (int i = 0; i < tank && counted < 5; i++, counted++) {
					if (counted > 1)
						str += separator;
					str += emoji_tank().ToString();
				}

				for (int i = 0; i < heal && counted < 5; i++, counted++) {
					if (counted > 1)
						str += separator;
					str += emoji_heal().ToString();
				}

				for (int i = 0; i < dps && counted < 5; i++, counted++) {
					if (counted > 1)
						str += separator;
					str += emoji_dps().ToString();
				}
				break;
			case Type.Other:
				str += "group size: ";
				str += members();
				break;
			}

			return str;
		}

		public static Type ParseType(string command) {
			return dict_commands_cache[command];
		}

		static Group() {
			Dictionary<string, Type> dict = new Dictionary<string, Type>();
			foreach (Type type in dict_commands.Keys) {
				foreach (string command in dict_commands[type]) {
					dict.Add(command, type);
				}
			}
			dict_commands_cache = dict;
		}

		private static DiscordEmoji emoji_tank() { return Program.getEmojiTank(); }
		private static DiscordEmoji emoji_heal() { return Program.getEmojiHeal(); }
		private static DiscordEmoji emoji_dps()  { return Program.getEmojiDps();  }
	}
}
