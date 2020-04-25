using DSharpPlus;
using DSharpPlus.Entities;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Puck {
	class Program {
		private static DiscordClient discord;
		private static DiscordChannel ch_lfg;

		private static DiscordEmoji emoji_tank, emoji_heal, emoji_dps;
		private static DiscordEmoji emoji_refresh, emoji_delist;

		private const string path_token = @"token.txt";
		//private const ulong ch_lfg_id = 542093438238326804;
		private const ulong ch_lfg_id = 489274692255875091;	// #test
		private const int color_embed = 0x45800C;

		private enum GroupType {
			Other = -1,
			Dungeon, Raid,
			Island, Warfront, Vision
		};

		private struct PuckOptions {
			public DiscordUser owner;
			public bool isHelp;
			public GroupType type;
			public string mention;
			public string title;
			public DateTimeOffset expiry;
			public bool isConfig;
		}

		public struct GroupDungeon {
			public int tank, heal, dps;
		};

		static void Main() {
			const string title_ascii =
				@"  ______           _    " + "\n" +
				@"  | ___ \         | |   " + "\n" +
				@"  | |_/ /   _  ___| | __" + "\n" +
				@"  |  __/ | | |/ __| |/ /" + "\n" +
				@"  | |  | |_| | (__|   < " + "\n" +
				@"  \_|   \__,_|\___|_|\_\" + "\n";
			Console.WriteLine(title_ascii);
			MainAsync().ConfigureAwait(false).GetAwaiter().GetResult();
		}

		static async Task MainAsync() {
			Console.WriteLine("Starting up...");
			InitBot();

			discord.MessageCreated += async e => {
				if (e.Message.Author.Username == discord.CurrentUser.Username) {
					return;	// never respond to self
				}

				bool isMentioned = false;
				foreach (DiscordUser mention in e.Message.MentionedUsers) {
					if (mention.IsCurrent) {
						isMentioned = true;
						break;
					}
				}

				if (isMentioned) {
					_ = e.Message.Channel.TriggerTypingAsync();	// we don't want to await

					Console.WriteLine("Raw message:\n" + e.Message.Content);
					PuckOptions command = ParseCommand(e.Message);
					GroupDungeon group = new GroupDungeon() { tank = 1, heal = 0, dps = 1 };

					string bulletin = "";
					if (command.mention != "")
						bulletin += "@" + command.mention + " ";
					bulletin += Format.Bold(command.title) + "\n";
					bulletin += "group lead: " + command.owner.Mention + "\n";
					bulletin += ToString(group) + "\n";
					bulletin += Format.Italicize("this group will be delisted in ~");

					DiscordMessage message_sent = await discord.SendMessageAsync(ch_lfg, bulletin);
					await message_sent.CreateReactionAsync(emoji_tank);
					await message_sent.CreateReactionAsync(emoji_heal);
					await message_sent.CreateReactionAsync(emoji_dps);
					await message_sent.CreateReactionAsync(emoji_refresh);
					await message_sent.CreateReactionAsync(emoji_delist);
				}
			};

			discord.Ready += async e => {
				ch_lfg = await discord.GetChannelAsync(ch_lfg_id);

				emoji_tank = DiscordEmoji.FromName(discord, ":shield:");
				emoji_heal = DiscordEmoji.FromName(discord, ":candle:");
				emoji_dps  = DiscordEmoji.FromName(discord, ":bow_and_arrow:");
				emoji_refresh = DiscordEmoji.FromName(discord, ":arrows_counterclockwise:");
				emoji_delist  = DiscordEmoji.FromName(discord, ":white_check_mark:");

				DiscordActivity helptext =
					new DiscordActivity(@"#lfg for pings", ActivityType.Watching);
				await discord.UpdateStatusAsync(helptext);

				Console.WriteLine("Startup complete.\n");	// extra newline
				Console.WriteLine("Monitoring messages...\n");
			};

			await discord.ConnectAsync();
			await Task.Delay(-1);
		}

		// Init discord client with token from text file.
		// This allows the token to be separated from source code.
		static void InitBot() {
			Console.WriteLine("  Reading auth token...");
			string bot_token = "";
			using (StreamReader file = File.OpenText(path_token)) {
				bot_token = file.ReadLine();
			}
			if (bot_token != "")
				Console.WriteLine("  Auth token found.");
			else
				Console.WriteLine("  Auth token missing!");

			discord = new DiscordClient(new DiscordConfiguration {
				Token = bot_token,
				TokenType = TokenType.Bot
			});
		}

		static PuckOptions ParseCommand(DiscordMessage message) {
			string command = message.Content;
			PuckOptions options = new PuckOptions() {
				owner = message.Author,
				isHelp = true,
				type = GroupType.Dungeon,
				mention = "",
				title = "",
				expiry = message.Timestamp,
				isConfig = false,
			};

			// Strip @mentions
			Regex regex_mention = new Regex(@"<@!\d+>");
			command = regex_mention.Replace(command, "").Trim();

			//if (command.StartsWith("-"))
			//	options.isHelp = true;
			//else
			//	options.isHelp = false;

			options.title = command;

			return options;
		}

		static string ToString(GroupDungeon group) {
			string output = "";
			string box_empty = "\u2610";
			string box_checked = "\u2611\uFE0E";

			output += (group.tank == 0) ? box_empty : box_checked;
			output += emoji_tank.ToString();

			output += " | ";
			output += (group.heal == 0) ? box_empty : box_checked;
			output += emoji_heal.ToString();

			for (int i=1; i<=3; i++) {
				output += " | ";
				output += (group.dps < i) ? box_empty : box_checked;
				output += emoji_dps.ToString();
			}

			return output;
		}
	}
}
