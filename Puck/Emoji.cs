using DSharpPlus;
using DSharpPlus.Entities;

using System;
using System.Collections.Generic;

namespace Puck {
	static class Emoji {
		public enum Type {
			Tank, Heal, Dps,
			Refresh, Delist,
		};

		static readonly Dictionary<Type, DiscordEmoji> type_to_emoji =
			new Dictionary<Type, DiscordEmoji>();
		// Default emojis if custom emojis could not be found.
		// Tank:	Shield
		// Heal:	Flag: Switzerland
		// Dps:		Bow and Arrow
		// Refresh:	Counterclockwise Arrows Button
		// Delist:	Check Mark Button
		static readonly Dictionary<Type, string> type_to_unicode =
			new Dictionary<Type, string>() {
				{ Type.Tank,	"\uD83D\uDEE1" },
				{ Type.Heal,	"\uD83C\uDDE8\uD83C\uDDED" },
				{ Type.Dps,		"\uD83C\uDFF9" },
				{ Type.Refresh,	"\uD83D\uDD04" },
				{ Type.Delist,	"\u2705" },
			};
		// Custom emojis (requires a connection to Erythro server).
		static readonly Dictionary<Type, string> type_to_custom =
			new Dictionary<Type, string> {
				{ Type.Tank,	":r_tank:" },
				{ Type.Heal,	":r_heal:" },
				{ Type.Dps,		":r_dps:" },
			};

		// Various different ways to fetch a DiscordEmoji.
		public static DiscordEmoji Get(this Type type) => From(type);
		public static DiscordEmoji From(Type type) {
			return type_to_emoji[type];
		}
		public static DiscordEmoji From(Group.Role role) {
			return role switch {
				Group.Role.Tank => From(Type.Tank),
				Group.Role.Heal => From(Type.Heal),
				Group.Role.Dps  => From(Type.Dps),
				_ => throw new ArgumentException(),
			};
		}

		// Convert a DiscordEmoji back into an Emoji.Type.
		// Throws ArgumentException on fail.
		public static Type? GetType(DiscordEmoji emoji) {
			foreach (Type type in type_to_emoji.Keys) {
				if (type_to_emoji[type] == emoji) {
					return type;
				}
			}
			return null;
		}

		// Custom emoji initialization (requires a connected client).
		public static void Init(DiscordClient client) {
			foreach (Type type in type_to_custom.Keys) {
				DiscordEmoji emoji =
					DiscordEmoji.FromName(client, type_to_custom[type]);
				if (emoji != null) {
					type_to_emoji[type] = emoji;
				}
			}
		}

		// Static initializer to populate emojis with default ones.
		// (From unicode since guild emojis require a connected client.)
		static Emoji() {
			foreach (Type type in type_to_unicode.Keys) {
				string unicode = type_to_unicode[type];
				DiscordEmoji emoji = DiscordEmoji.FromUnicode(unicode);
				type_to_emoji.Add(type, emoji);
			}
		}
	}
}
