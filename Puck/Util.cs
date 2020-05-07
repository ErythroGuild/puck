using DSharpPlus;
using DSharpPlus.Entities;

using System.Threading.Tasks;

namespace Puck {
	static class Util {
		static readonly Logger log = Program.GetLogger();

		// Formats the DiscordUser as "Name#0000".
		public static string Userstring(this DiscordUser u) {
			return u.Username + "#" + u.Discriminator;
		}

		// Formats the DiscordChannel as "#name".
		public static string Channelstring(this DiscordChannel ch) {
			return "#" + ch.Name;
		}

		// Formats the DiscordGuild as "<Name>".
		public static string Guildstring(this DiscordGuild g) {
			return "<" + g.Name + ">";
		}

		// Formats the DiscordRole as "@Name".
		public static string Rolestring(this DiscordRole r) {
			return "@" + r.Name;
		}

		// Casts DiscordUser to DiscordMember w/ a given guild.
		// May fail (return null) if the guild doesn't contain the user.
		public static DiscordMember? ToDiscordMember(
			this DiscordUser user,
			DiscordGuild guild
		) {
			foreach (DiscordMember member in guild.Members.Values) {
				if (member.Id == user.Id)
					return member;
			}
			string warning =
				"DiscordMember conversion failed: " +
				user.Userstring() + " - " + guild.Name;
			log.Warning(warning);
			return null;
		}

		// Returns a private channel to the sender of a DiscordMessage.
		// Will fail (return null) if the bot does not share a guild with the user.
		public static async Task<DiscordChannel?> GetPrivateChannel(DiscordMessage message) {
			DiscordChannel channel = message.Channel;
			if (!channel.IsPrivate) {
				// if message was not from a DM channel, then we share a server:
				// a guild exists and casting to DiscordMember should work
				DiscordGuild guild = message.Channel.Guild;
				DiscordMember? member = message.Author.ToDiscordMember(guild);
				if (member == null) {
					log.Warning("Could not create a DM channel.");
					return null;
				}
				channel = await member.CreateDmChannelAsync();
			}
			return channel;
		}

		public static bool CanMention(
			DiscordRole? role,
			DiscordMember? member,
			DiscordChannel? channel
		) {
			if (role == null)
				return true;
			if (member == null || channel == null)
				return false;
			Permissions permissions = member.PermissionsIn(channel);
			bool can_mention = permissions.HasPermission(Permissions.MentionEveryone);
			if (!can_mention && role.IsMentionable) {
				can_mention = true;
			}
			return can_mention;
		}
	}
}
