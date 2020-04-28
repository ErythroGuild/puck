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

		public override string ToString() {
			string post = "";
			bool is_expired = (expiry <= DateTimeOffset.Now);

			// @mention + group title
			if (mention != null && !is_expired) {
				post += mention.Mention + " ";
			}
			string title_str = Format.Bold(title);
			if (is_expired)
				title_str = Format.Strikethrough(title_str);
			post += title_str + "\n";

			// group
			post += "group lead: " + owner.Mention + "\n";
			post += group.ToString() + "\n";

			// expiry time
			TimeSpan expiry_round = expiry - DateTimeOffset.Now;
			static double RoundToFive(double x) { return Math.Round(x / 5.0) * 5.0; }
			double seconds_round = RoundToFive(expiry_round.TotalSeconds);
			expiry_round = TimeSpan.FromSeconds(seconds_round);

			string delist_str =
				"this group will be delisted in ~" +
				expiry_round.ToString(@"mm\:ss");
			if (is_expired)
				delist_str = "this group has been delisted";
			post += Format.Italicize(delist_str);

			return post;
		}

		public static BulletinData Parse(DiscordMessage message) {
			string command = message.Content;
			DiscordGuild guild = message.Channel.Guild;
			Settings settings = Program.GetSettings(guild.Id);

			// Strip @mentions
			command = Regex.Replace(command, @"<@!\d+>", "");
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
				command_mention = settings.default_mention?.Name
					?? Settings.mention_none;
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

			// Instantiate BulletinData
			BulletinData data = new BulletinData {
				owner	= GetDiscordMember(message.Author, guild),
				title	= command_title,
				mention	= mention,
				expiry	= message.Timestamp + settings.duration,
				group	= new Group(Group.ParseType(command_option))
			};

			return data;
		}

		// TODO: move to Puck.Program?
		private static DiscordMember GetDiscordMember(DiscordUser user, DiscordGuild guild) {
			foreach (DiscordMember member in guild.Members.Values) {
				if (member.Id == user.Id)
					return member;
			}
			return null;
		}
	}
}
