using DSharpPlus.Entities;

namespace Puck {
	class MentionRole {
		public enum Type {
			None = 0,
			Here, Everyone,
			Discord,
		};

		Type type;
		DiscordRole? discord_role;
		// DiscordRole should only be non-null if type is .Discord.

		// Can be constructed from either a MentionRole.Type,
		// or directly with a DiscordRole.
		public MentionRole(Type type, DiscordRole? role = null) {
			this.type = type;
			if (type == Type.Discord) {
				discord_role = role;
			} else {
				discord_role = null;
			}
		}
		public MentionRole(DiscordRole role) {
			type = Type.Discord;
			discord_role = role;
		}

		// Setters basically replicate constructor logic.
		// Can either set with a MentionRole.Type, or just a DiscordRole.
		public void Set(Type type, DiscordRole? role = null) {
			this.type = type;
			if (type == Type.Discord) {
				discord_role = role;
			} else {
				discord_role = null;
			}
		}
		public void Set(DiscordRole role) {
			type = Type.Discord;
			discord_role = role;
		}

		// Member variable getters.
		public Type RoleType() {
			return type;
		}
		public DiscordRole? GetDiscordRole() {
			return discord_role;
		}

		// Equivalents for DiscordRole properties.
		// Name() is the role's text equivalent.
		public string Name() {
			// default case will only happen if type was casted
			return type switch {
				Type.None		=> "none",
				Type.Here		=> "here",
				Type.Everyone	=> "everyone",
				Type.Discord	=> discord_role!.Name,
				_ => "none",
			};
		}

		// Mention() is the string used to ping the role.
		public string? Mention() {
			// default case will only happen if type was casted
			return type switch {
				Type.None		=> null,
				Type.Here		=> "@here",
				Type.Everyone	=> "@everyone",
				Type.Discord	=> discord_role!.Mention,
				_ => null,
			};
		}
	}
}
