using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Puck {
	class Program {
		private const ulong channel_debug_id = 489274692255875091;  // <Erythro> - #test

		private static DiscordClient discord;
		private static Dictionary<ulong, Settings> settings;
		private static Dictionary<ulong, Bulletin> bulletins =
			new Dictionary<ulong, Bulletin>();
		public static Settings GetSettings(ulong guild_id) { return settings[guild_id]; }

		private const string path_token = @"token.txt";
		private const string path_settings = @"settings.txt";

		private static DiscordEmoji
			emoji_tank,
			emoji_heal,
			emoji_dps,
			emoji_refresh,
			emoji_delist;
		private const string
			emoji_tank_str		= ":shield:",
			emoji_heal_str		= ":candle:",
			emoji_dps_str		= ":archery:",
			emoji_refresh_str	= ":arrows_counterclockwise:",
			emoji_delist_str	= ":white_check_mark:";

		public static Dictionary<string, DiscordEmoji> str_to_emoji;
		public static DiscordEmoji getEmojiTank() { return emoji_tank; }
		public static DiscordEmoji getEmojiHeal() { return emoji_heal; }
		public static DiscordEmoji getEmojiDps()  { return emoji_dps;  }

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

			discord.Ready += async e => {
				// Initialize emojis
				emoji_tank = DiscordEmoji.FromName(discord, emoji_tank_str);
				emoji_heal = DiscordEmoji.FromName(discord, emoji_heal_str);
				emoji_dps  = DiscordEmoji.FromName(discord, emoji_dps_str);
				emoji_refresh = DiscordEmoji.FromName(discord, emoji_refresh_str);
				emoji_delist  = DiscordEmoji.FromName(discord, emoji_delist_str);

				str_to_emoji = new Dictionary<string, DiscordEmoji> {
					{ emoji_tank_str,       emoji_tank },
					{ emoji_heal_str,       emoji_heal },
					{ emoji_dps_str,        emoji_dps },
					{ emoji_refresh_str,    emoji_refresh },
					{ emoji_delist_str,     emoji_delist },
			};

				// Set "custom status" (TODO: still waiting for an actual API)
				DiscordActivity helptext =
					new DiscordActivity(@"#lfg for pings", ActivityType.Watching);
				await discord.UpdateStatusAsync(helptext);

				Console.WriteLine("Startup complete.\n");	// extra newline
				Console.WriteLine("Monitoring messages...\n");
			};

			discord.GuildDownloadCompleted += async e => {
				// Set up default config
				settings = await Settings.Import(path_settings, discord);
				foreach (ulong guild_id in discord.Guilds.Keys) {
					if (!settings.ContainsKey(guild_id)) {
						DiscordChannel channel_default = discord.Guilds[guild_id].GetDefaultChannel();
						Settings settings_default = new Settings(channel_default);
						settings.Add(guild_id, settings_default);
					}
				}
				ExportSettings();
			};

			discord.MessageCreated += async e => {
				if (e.Message.Author.Username == discord.CurrentUser.Username) {
					return; // never respond to self
				}

				bool isMentioned = false;
				foreach (DiscordUser mention in e.Message.MentionedUsers) {
					if (mention.IsCurrent) {
						isMentioned = true;
						break;
					}
				}

				if (isMentioned) {
					_ = e.Message.Channel.TriggerTypingAsync(); // don't need to await
					Console.WriteLine("Raw message:\n" + e.Message.Content + "\n");

					BulletinData data = BulletinData.Parse(e.Message);
					DiscordChannel channel = settings[e.Guild.Id].bulletin;
#if DEBUG
					channel = await discord.GetChannelAsync(channel_debug_id);
#endif
					DiscordMessage message =
						await discord.SendMessageAsync(channel, data.ToString());
					await CreateControls(message, data.group.type);

					Bulletin bulletin = new Bulletin(message, data);
					bulletins.Add(message.Id, bulletin);
					bulletin.Delisted += (o, message_id) => {
						bulletins.Remove(message_id);
					};
				}
			};

			discord.MessageReactionAdded += async e => {
				if (bulletins.ContainsKey(e.Message.Id)) {
					// No need to respond to bot's own reactions
					if (e.User == discord.CurrentUser)
						return;

					Console.Write("  button pressed: " + e.Emoji.GetDiscordName());
					Console.Write(" (" + e.User.Username);
					Console.Write("#" + e.User.Discriminator + ")\n");

					await UpdateFromControls(e);
				}
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

		static void ExportSettings() {
			Settings.Export(path_settings, settings);
		}

		static async Task CreateControls(DiscordMessage message, Group.Type type) {
			switch (type) {
			case Group.Type.Dungeon:
			case Group.Type.Raid:
			case Group.Type.Warfront:
			case Group.Type.Vision:
			case Group.Type.Other:
				await message.CreateReactionAsync(emoji_tank);
				await message.CreateReactionAsync(emoji_heal);
				await message.CreateReactionAsync(emoji_dps);
				break;
			case Group.Type.Scenario:
			case Group.Type.Island:
				await message.CreateReactionAsync(emoji_dps);
				break;
			}
			await message.CreateReactionAsync(emoji_refresh);
			await message.CreateReactionAsync(emoji_delist);
		}

		static async Task UpdateFromControls(MessageReactionAddEventArgs e) {
			ulong message_id = e.Message.Id;
			bool is_owner = (e.User == bulletins[message_id].data.owner);
			string emoji_str = e.Emoji.GetDiscordName();
			BulletinData data= bulletins[message_id].data;

			// Global controls (refresh/delist)
			if (is_owner) {
				switch (emoji_str) {
				case emoji_refresh_str:
					data.expiry += settings[e.Guild.Id].increment;
					await bulletins[message_id].message.
						DeleteReactionAsync(str_to_emoji[emoji_str], e.User);
					break;
				case emoji_delist_str:
					data.expiry = DateTimeOffset.Now;
					await bulletins[message_id].message.
						DeleteReactionAsync(str_to_emoji[emoji_str], e.User);
					break;
				}
				bulletins[message_id].data = data;
				// TODO: add removal reason (for audit logs)
			}

			// Group.Type-specific controls
			switch (bulletins[message_id].data.group.type) {
			case Group.Type.Dungeon:
				if (is_owner) {
					// modulo 1/1/3, but +1 because counting "0" as a state
					switch (emoji_str) {
					case emoji_tank_str:
						++data.group.tank;
						data.group.tank %= 2;
						break;
					case emoji_heal_str:
						++data.group.heal;
						data.group.heal %= 2;
						break;
					case emoji_dps_str:
						++data.group.dps;
						data.group.dps %= 4;
						break;
					}
					bulletins[message_id].data = data;
					DiscordEmoji em = str_to_emoji[emoji_str];
					await bulletins[message_id].message.
						DeleteReactionAsync(str_to_emoji[emoji_str], e.User);
					// TODO: add removal reason (for audit logs)
				} else {
					switch (emoji_str) {
					case emoji_tank_str:
						data.group.tank = Math.Max(++data.group.tank, 1);
						break;
					case emoji_heal_str:
						data.group.heal = Math.Max(++data.group.heal, 1);
						break;
					case emoji_dps_str:
						data.group.dps = Math.Max(++data.group.dps, 3);
						break;
					}
					bulletins[message_id].data = data;
				}
				break;
			}

			await bulletins[message_id].Update();
		}
	}
}
