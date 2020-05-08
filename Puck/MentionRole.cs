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

		public Type RoleType() {
			return type;
		}
		public DiscordRole? GetDiscordRole() {
			return discord_role;
		}

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
