using DSharpPlus.Entities;

using System;

namespace Puck {
	class MentionRole {
		public enum Type {
			None = 0,
			Here, Everyone,
			Discord,
		};
		
		// Static methods for serialization & deserialization.
		public static string ToString(MentionRole role) {
			return role.type switch {
				Type.None		=> "none",
				Type.Here		=> "here",
				Type.Everyone	=> "everyone",
				Type.Discord	=> role.discord_role!.Id.ToString(),
				_ => throw new ArgumentException(),
			};
		}
		public static MentionRole? FromID(string id, DiscordGuild guild) {
			Type type = id switch {
				"none"		=> Type.None,
				"here"		=> Type.Here,
				"everyone"	=> Type.Everyone,
				_			=> Type.Discord,
			};
			DiscordRole? role = null;
			if (type == Type.Discord) {
				role = guild.GetRole(Convert.ToUInt64(id));
			}
			return new MentionRole(type, role);
		}
		public static MentionRole? FromName(string name, DiscordGuild guild) {
			Type type = name switch {
				"none"		=> Type.None,
				"here"		=> Type.Here,
				"everyone"	=> Type.Everyone,
				_			=> Type.Discord,
			};
			DiscordRole? role = null;
			if (type == Type.Discord) {
				foreach (DiscordRole role_i in guild.Roles.Values) {
					if (role_i.Name == name) {
						role = role_i;
						break;
					}
				}
			}
			return new MentionRole(type, role);
		}



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

		// Serialization.
		public override string ToString() {
			return ToString(this);
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
			return type switch {
				Type.None		=> "none",
				Type.Here		=> "here",
				Type.Everyone	=> "everyone",
				Type.Discord	=> discord_role!.Name,
				_ => throw new InvalidOperationException(),
			};
		}

		// Mention() is the string used to ping the role.
		public string? Mention() {
			return type switch {
				Type.None		=> null,
				Type.Here		=> "@here",
				Type.Everyone	=> "@everyone",
				Type.Discord	=> discord_role!.Mention,
				_ => throw new InvalidOperationException(),
			};
		}
	}
}
