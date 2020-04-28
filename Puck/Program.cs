using DSharpPlus;
using DSharpPlus.Entities;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Puck {
	class Program {
		private static DiscordClient discord;
		private static Dictionary<ulong, Settings> settings;
		private static Dictionary<ulong, GroupEntry> bulletins =
			new Dictionary<ulong, GroupEntry>();

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

		private static readonly double bulletin_refresh_interval =
			TimeSpan.FromSeconds(15).TotalMilliseconds;
		private const ulong channel_debug_id = 489274692255875091;	// <Erythro> - #test

		public static Settings GetSettings(ulong guild_id) { return settings[guild_id]; }

		public static DiscordEmoji getEmojiTank() { return emoji_tank; }
		public static DiscordEmoji getEmojiHeal() { return emoji_heal; }
		public static DiscordEmoji getEmojiDps()  { return emoji_dps;  }

		private struct GroupEntry {
			public DiscordMessage bulletin;
			public GroupOptions options;
			public GroupDungeon group;
			public Timer update;
		};

		private struct GroupOptions {
			public DiscordGuild guild;
			public DiscordUser owner;
			public bool isHelp;
			public GroupType type;
			public string mention;
			public string title;
			public DateTimeOffset expiry;
			public bool isConfig;
			public Settings config;
		};

		private struct GroupDungeon {
			public int tank, heal, dps;
		};

		public enum GroupType {
			Other = -1,
			Dungeon, Raid,
			Island, Warfront, Vision
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

					Console.WriteLine("Raw message:\n" + e.Message.Content + "\n");	// extra newline
					GroupOptions command = ParseCommand(e.Message);
					GroupDungeon group = new GroupDungeon() { tank = 0, heal = 0, dps = 0 };

					// Construct a bulletin post.
					string bulletin = ConstructBulletin(command, group);

					// Set up controls on the bulletin and save it to a table.
					DiscordGuild guild = e.Guild;
#if !DEBUG
					DiscordMessage message_sent =
						await discord.SendMessageAsync(settings[guild.Id].bulletin, bulletin);
#else
					DiscordChannel channel_debug =
						await discord.GetChannelAsync(channel_debug_id);
					DiscordMessage message_sent =
						await discord.SendMessageAsync(channel_debug, bulletin);
#endif
					await CreateControls(message_sent);
					ulong message_id = message_sent.Id;
					Timer timer = new Timer(bulletin_refresh_interval);
					timer.AutoReset = true;
					timer.Elapsed += (o, e) => { _ = UpdateBulletin(message_sent); };
					timer.Start();
					GroupEntry entry = new GroupEntry() {
						bulletin = message_sent,
						options = command,
						group = group,
						update = timer,
					};
					bulletins.Add(message_id, entry);
				}
			};

			discord.Ready += async e => {
				// Initialize emojis
				emoji_tank = DiscordEmoji.FromName(discord, emoji_tank_str);
				emoji_heal = DiscordEmoji.FromName(discord, emoji_heal_str);
				emoji_dps  = DiscordEmoji.FromName(discord, emoji_dps_str);
				emoji_refresh = DiscordEmoji.FromName(discord, emoji_refresh_str);
				emoji_delist  = DiscordEmoji.FromName(discord, emoji_delist_str);

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

			discord.MessageReactionAdded += async e => {
				ulong message_id = e.Message.Id;
				if (bulletins.ContainsKey(message_id)) {
					GroupEntry entry = bulletins[message_id];

					if (e.User == discord.CurrentUser)
						return;

					Console.Write("  button pressed: " + e.Emoji.GetDiscordName());
					Console.Write(" (" + e.User.Username + ")\n");
					if (e.User == entry.options.owner) {
						// modulo 1/1/3, but +1 because counting "0" as a state
						switch (e.Emoji.GetDiscordName()) {
						case emoji_tank_str:
							entry.group.tank = (entry.group.tank + 1) % 2;
							bulletins[message_id] = entry;
							// TODO: Add removal reason for audit logs
							await entry.bulletin.DeleteReactionAsync(emoji_tank, e.User);
							break;
						case emoji_heal_str:
							entry.group.heal = (entry.group.heal + 1) % 2;
							bulletins[message_id] = entry;
							// TODO: Add removal reason for audit logs
							await entry.bulletin.DeleteReactionAsync(emoji_heal, e.User);
							break;
						case emoji_dps_str:
							entry.group.dps = (entry.group.dps + 1) % 4;
							bulletins[message_id] = entry;
							// TODO: Add removal reason for audit logs
							await entry.bulletin.DeleteReactionAsync(emoji_dps, e.User);
							break;
						case emoji_refresh_str:
							ulong guild_id = entry.bulletin.Channel.GuildId;
							entry.options.expiry += settings[guild_id].increment;
							bulletins[message_id] = entry;
							// TODO: Add removal reason for audit logs
							await entry.bulletin.DeleteReactionAsync(emoji_refresh, e.User);
							break;
						case emoji_delist_str:
							entry.options.expiry = DateTimeOffset.Now;
							bulletins[message_id] = entry;
							// TODO: Add removal reason for audit logs
							await entry.bulletin.DeleteReactionAsync(emoji_delist, e.User);
							break;
						}
					} else {
						switch (e.Emoji.GetDiscordName()) {
						case emoji_tank_str:
							entry.group.tank = Math.Max(entry.group.tank + 1, 1);
							bulletins[message_id] = entry;
							break;
						case emoji_heal_str:
							entry.group.heal = Math.Max(entry.group.heal + 1, 1);
							bulletins[message_id] = entry;
							break;
						case emoji_dps_str:
							entry.group.dps  = Math.Max(entry.group.dps  + 1, 3);
							bulletins[message_id] = entry;
							break;
						}
					}

					await UpdateBulletin(entry.bulletin);
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

		static GroupOptions ParseCommand(DiscordMessage message) {
			string command = message.Content;
			Settings settings_message = settings[message.Channel.GuildId];
			GroupOptions options = new GroupOptions() {
				guild = message.Channel.Guild,
				owner = message.Author,
				isHelp = false,
				type = GroupType.Dungeon,
				mention = settings_message.default_mention?.Name ?? "",
				title = "",
				expiry = message.Timestamp + settings_message.duration,
				isConfig = false,
			};

			// Strip @mentions
			Regex regex_mention = new Regex(@"<@!\d+>");
			command = regex_mention.Replace(command, "").Trim();

			Regex regex_command = new Regex(@"(?:-(\S+)\s+)?(?:!(\S+)\s+)?(?:(.+))");
			Match match = regex_command.Match(command);
			string command_command = match.Groups[1].Value.ToLower();
			string command_mention = match.Groups[2].Value;
			string command_title   = match.Groups[3].Value;

			switch (command_command) {
			case "?":
			case "h":
			case "help":
				options.isHelp = true;
				break;
			case "c":
			case "config":
				options.isConfig = true;
				break;
			}

			if (command_mention != string.Empty) {
				options.mention = command_mention;
			}

			options.title = command_title;

			return options;
		}

		static string ToString(GroupDungeon group) {
			string output = "";
			string box_empty = "\u2610";
			string box_checked = "\u2611\uFE0E";
			string separator = "\u2002|\u2002";

			output += (group.tank == 0) ? box_empty : box_checked;
			output += emoji_tank.ToString();

			output += separator;
			output += (group.heal == 0) ? box_empty : box_checked;
			output += emoji_heal.ToString();

			for (int i=1; i<=3; i++) {
				output += separator;
				output += (group.dps < i) ? box_empty : box_checked;
				output += emoji_dps.ToString();
			}

			return output;
		}

		static async Task CreateControls(DiscordMessage message) {
			await message.CreateReactionAsync(emoji_tank);
			await message.CreateReactionAsync(emoji_heal);
			await message.CreateReactionAsync(emoji_dps);
			await message.CreateReactionAsync(emoji_refresh);
			await message.CreateReactionAsync(emoji_delist);
		}

		static string ConstructBulletin(GroupOptions command, GroupDungeon group) {
			TimeSpan interval = command.expiry - DateTimeOffset.Now;
			// TODO: better error checking
			if (interval >= TimeSpan.FromMinutes(60)) {
				command.expiry = DateTimeOffset.Now + TimeSpan.FromMinutes(60);
				interval = command.expiry - DateTimeOffset.Now;
			}

			string bulletin = "";
			if (interval > TimeSpan.Zero) {
				if (command.mention != "")
					foreach (DiscordRole role in command.guild.Roles.Values) {
						if (role.Name == command.mention) {
							bulletin += role.Mention + " ";
							break;
						}
					}
				bulletin += Format.Bold(command.title) + "\n";
			} else {
				bulletin += Format.Strikethrough(Format.Bold(command.title)) + "\n";
			}
			bulletin += "group lead: " + command.owner.Mention + "\n";
			bulletin += ToString(group) + "\n";
			string interval_str = interval.ToString(@"mm\:ss");
			string delist_str = "this group will be delisted in ~" + interval_str;
			if (interval < TimeSpan.Zero)
				delist_str = "this group has been delisted";
			bulletin += Format.Italicize(delist_str);
			return bulletin;
		}

		static async Task UpdateBulletin(DiscordMessage message) {
			string bulletin = ConstructBulletin(
				bulletins[message.Id].options,
				bulletins[message.Id].group
			);
			await message.ModifyAsync(bulletin);

			// TODO: add a grace period after delisting?
			// TODO: add a warning 1:30 before delisting?
			if (bulletins[message.Id].options.expiry < DateTimeOffset.Now) {
				DiscordMember owner =
					await bulletins[message.Id].options.guild.GetMemberAsync(bulletins[message.Id].options.owner.Id);
				_ = owner.SendMessageAsync("Your group **" + bulletins[message.Id].options.title + "** has been delisted. :white_check_mark:");

				bulletins.Remove(message.Id);
				Console.WriteLine("Delisting " + message.Id.ToString() + "\n");	// extra newline
			}
		}

		static void ExportSettings() {
			Settings.Export(path_settings, settings);
		}
	}
}
