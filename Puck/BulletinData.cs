using DSharpPlus;
using DSharpPlus.Entities;

using System;
using System.Text.RegularExpressions;

namespace Puck {
	class BulletinData {
		public DiscordMember owner;
		public string title;
		public DiscordRole mention;
		public DateTimeOffset expiry;
		public Group group;

		private BulletinData() { }

		public static BulletinData Parse(DiscordMessage message) {
			string command = message.Content;
			DiscordGuild guild = message.Channel.Guild;
			Settings settings = Program.GetSettings(guild.Id);

			// Strip @mentions
			Regex.Replace(command, @"<@!\d+>", "");
			command = command.Trim();

			// Separate command into component parts
			Regex regex_command = new Regex(@"(?:-(\S+)\s+)?(?:!(\S+)\s+)?(?:(.+))");
			Match match = regex_command.Match(command);
			string command_option = match.Groups[1].Value.ToLower();
			string command_mention = match.Groups[2].Value;
			string command_title = match.Groups[3].Value;

			// Get DiscordRole to mention
			DiscordRole mention = null;
			if (command_mention == string.Empty) {
				command_mention = settings.default_mention.Name;
			}
			if (command_mention != Settings.mention_none) {
				foreach (DiscordRole role in guild.Roles.Values) {
					if (role.Name == command_mention) {
						mention = role;
						break;
					}
				}
				if (command_mention == "everyone") {
					Permissions permissions =
						GetDiscordMember(message.Author, guild)
						.PermissionsIn(settings.bulletin);
					bool can_mention =
						permissions.HasPermission(Permissions.MentionEveryone);
					if (can_mention)
						mention = message.Channel.Guild.EveryoneRole;
				}
			}

			// Construct BulletinData
			BulletinData data = new BulletinData();
			data.owner = GetDiscordMember(message.Author, guild);
			data.title = command_title;
			data.mention = mention;
			TimeSpan duration = settings.duration;
			data.expiry = message.Timestamp + duration;
			data.group = new Group(Group.ParseType(command_option));

			return data;
		}

		private static DiscordMember GetDiscordMember(DiscordUser user, DiscordGuild guild) {
			foreach (DiscordMember member in guild.Members.Values) {
				if (member.Id == user.Id)
					return member;
			}
			return null;
		}
	}
}
