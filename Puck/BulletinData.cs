using DSharpPlus;
using DSharpPlus.Entities;

using System;

namespace Puck {
	class BulletinData {
		public DiscordMember owner;
		public string title;
		public MentionRole? mention;
		public DateTimeOffset expiry;
		public Group group;

		private BulletinData(
			DiscordMember owner,
			string title,
			MentionRole? mention,
			DateTimeOffset expiry,
			Group group
		) {
			this.owner = owner;
			this.title = title;
			this.mention = mention;
			this.expiry = expiry;
			this.group = group;
		}

		public override string ToString() {
			string post = "";
			bool is_expired = (expiry <= DateTimeOffset.Now);

			// @mention + group title
			if (mention != null && !is_expired) {
				post += mention.Mention() + " ";
			}
			string title_str = title.Bold();
			if (is_expired)
				title_str = title_str.Strike();
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
			post += delist_str.Italics();

			return post;
		}

		public static BulletinData Parse(
			string command_option,
			string command_mention,
			string command_title,
			DiscordMessage message,
			Settings settings
		) {
			DiscordGuild guild = message.Channel.Guild;

			// Get DiscordRole to mention
			MentionRole? mention = null;
			if (command_mention == string.Empty) {
				command_mention = settings.default_mention?.Name()
					?? Settings.mention_none;
			}
			if (command_mention != Settings.mention_none) {
				mention = MentionRole.FromName(command_mention, guild);
			}
			bool can_mention = Util.CanMention(
				mention,
				message.Author.ToDiscordMember(guild),
				settings.bulletin
			);
			if (!can_mention)
				mention = null;

			Group.Type group_type;
			if (command_option == "")
				group_type = Group.default_type;
			else
				group_type = Group.ParseType(command_option);

			// Instantiate BulletinData
			BulletinData data = new BulletinData(
				// DiscordUser -> DiscordMember is guaranteed to succeed here
				message.Author.ToDiscordMember(guild)!,
				command_title,
				mention,
				message.Timestamp + settings.duration,
				new Group(group_type)
			);

			return data;
		}
	}
}
