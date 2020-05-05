using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Puck {
	class Program {
		private const ulong channel_debug_id = 489274692255875091;  // <Erythro> - #test

		private static Logger log = new Logger();
		public static ref Logger GetLogger() { return ref log; }

		private static DiscordClient discord;
		private static Dictionary<ulong, Settings> settings;
		private static Dictionary<ulong, Bulletin> bulletins =
			new Dictionary<ulong, Bulletin>();
		private static Dictionary<ulong, DiscordMember> owners =
			new Dictionary<ulong, DiscordMember>();
		public static Settings GetSettings(ulong guild_id) { return settings[guild_id]; }

		private const string path_token = @"token.txt";
		private const string path_settings = @"settings.txt";

		private static DiscordEmoji?
			emoji_tank,
			emoji_heal,
			emoji_dps,
			emoji_refresh,
			emoji_delist;
		private const string
			emoji_tank_str		= ":shield:",
			emoji_heal_str		= ":flag_ch:",
			emoji_dps_str		= ":archery:",
			emoji_refresh_str	= ":arrows_counterclockwise:",
			emoji_delist_str	= ":white_check_mark:";

		public static Dictionary<string, DiscordEmoji> str_to_emoji;
		public static DiscordEmoji? GetEmoji(Group.Role role) {
			return role switch {
				Group.Role.Tank => emoji_tank,
				Group.Role.Heal => emoji_heal,
				Group.Role.Dps  => emoji_dps,
				_ => null,
			};
		}

		public static DiscordMember? GetDiscordMember(DiscordUser user, DiscordGuild guild) {
			foreach (DiscordMember member in guild.Members.Values) {
				if (member.Id == user.Id)
					return member;
			}
			return null;
		}

		static bool IsRequestHelp(string command) {
			return command switch
			{
				"help" => true,
				"h" => true,
				"?" => true,
				_ => false,
			};
		}
		static bool IsRequestConfig(string command) {
			return command switch
			{
				"config" => true,
				_ => false,
			};
		}

		static void Main() {
			// not using Logger for title for ease of formatting
			const string title_ascii =
				@"  ______           _    " + "\n" +
				@"  | ___ \         | |   " + "\n" +
				@"  | |_/ /   _  ___| | __" + "\n" +
				@"  |  __/ | | |/ __| |/ /" + "\n" +
				@"  | |  | |_| | (__|   < " + "\n" +
				@"  \_|   \__,_|\___|_|\_\" + "\n";
			Console.WriteLine(title_ascii);
			log.show_timestamp = true;
			MainAsync().ConfigureAwait(false).GetAwaiter().GetResult();
		}

		static async Task MainAsync() {
			log.Info("Initializing...");
			Connect();

			discord.Ready += async e => {
				log.Info("Connected to discord.");
				log.Info("Setting up emojis...", 1);

				// Initialize emojis.
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
				log.Info("Emojis set.", 1);

				// Set "custom status".
				log.Info("Setting bot custom status...", 1);
				DiscordActivity helptext =
					new DiscordActivity(@"#lfg for pings", ActivityType.Watching);
				await discord.UpdateStatusAsync(helptext);
				log.Info("Custom status set.", 1);
			};

			discord.GuildDownloadCompleted += async e => {
				// Set up default config
				settings = await Settings.Import(path_settings, discord);
				foreach (ulong guild_id in discord.Guilds.Keys) {
					DiscordGuild guild = await discord.GetGuildAsync(guild_id);
					owners.Add(guild_id, guild.Owner);

					if (!settings.ContainsKey(guild_id)) {
						DiscordChannel channel_default = discord.Guilds[guild_id].GetDefaultChannel();
						Settings settings_default = new Settings(channel_default);
						settings.Add(guild_id, settings_default);
					}
				}
				await ExportSettings();

				discord.GuildCreated += async f => {
					DiscordGuild guild = f.Guild;
					if (owners.ContainsKey(guild.Id))
						owners[guild.Id] = guild.Owner;
					else
						owners.Add(guild.Id, guild.Owner);

					settings.TryAdd(guild.Id, new Settings(null));
					await ExportSettings();

					await guild.Owner.SendMessageAsync(
						"Hello! I've just been added to your server, " +
						guild.Name.Bold() +
						" :wave:\n" +
						"Reply to this message to configure me.\n" +
						("You can also update this later by typing " +
						"anywhere in the server.").Italics() + "\n" +
						"@Puck -config channel {channel-name}".Code() + "\n" +
						"@Puck -config mention {role-name}".Code() + "\n" +
						"The above commands will set my response channel " +
						"and default mention.\n" +
						"You can skip the second command, or set it to " +
						"none".Code() + "/" + "everyone".Code() + "."
					);
				};
			};

			log.Info("Monitoring messages...");
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
				foreach (DiscordRole role in e.Message.MentionedRoles) {
					if (role.Name == discord.CurrentUser.Username) {
						isMentioned = true;
						log.Debug("Role mention.", 0, e.Message.Id);
						break;
					}
				}

				if (isMentioned) {
					_ = e.Message.Channel.TriggerTypingAsync(); // don't need to await
					log.Info("Raw message:", 0, e.Message.Id);
					log.Info(e.Message.Content, 0, e.Message.Id);

					BulletinData? data = await ParseMessage(e.Message);
					if (data == null) {
						log.Error("Failed to parse message.", 1, e.Message.Id);
						return;
					}
					DiscordChannel? channel = settings[e.Guild.Id].bulletin;
#if DEBUG
					channel = await discord.GetChannelAsync(channel_debug_id);
#endif
					if (channel == null) {
						log.Error("Failed to find channel to post in.", 1, e.Message.Id);
						return;
					}

					log.Info("Posting bulletin...", 1, e.Message.Id);
					DiscordMessage message =
						await discord.SendMessageAsync(channel, data.ToString());
					log.Info("Creating controls...", 1, e.Message.Id);
					await CreateControls(message, data.group.type);

					Bulletin bulletin = new Bulletin(message, data);
					bulletins.Add(message.Id, bulletin);
					bulletin.Delisted += (o, message_id) => {
						log.Info("Delisting group.", 0, message.Id);
						bulletins.Remove(message_id);
					};
				}
			};

			discord.MessageReactionAdded += async e => {
				if (bulletins.ContainsKey(e.Message.Id)) {
					// No need to respond to bot's own reactions
					if (e.User == discord.CurrentUser)
						return;

					string log_str =
						"button pressed: " + e.Emoji.GetDiscordName() +
						" (" + e.User.Username +
						"#" + e.User.Discriminator + ")";
					log.Info(log_str, 1, e.Message.Id);

					await UpdateFromControls(e);
				}
			};
			discord.MessageReactionRemoved += async e => {
				if (bulletins.ContainsKey(e.Message.Id)) {
					// No need to respond to bot's own reactions
					if (e.User == discord.CurrentUser)
						return;

					string log_str =
						"button unpressed: " + e.Emoji.GetDiscordName() +
						" (" + e.User.Username +
						"#" + e.User.Discriminator + ")";
					log.Info(log_str, 1, e.Message.Id);

					await UpdateFromControls(e);
				}
			};

			await discord.ConnectAsync();
			await Task.Delay(-1);
		}

		// Init discord client with token from text file.
		// This allows the token to be separated from source code.
		static void Connect() {
			log.Info("Reading authentication token...", 1);

			// Open text file.
			StreamReader? file = null;
			try {
				file = File.OpenText(path_token);
			} catch (Exception) {
				log.Error("Could not open \"token.txt\".", 1);
			}

			// Read text file.
			string token = file?.ReadLine() ?? "";
			if (token != "") {
				log.Info("Authentication token found.", 1);
				int uncensor = 4;
				string token_censored =
					token.Substring(0, uncensor) +
					new string('*', token.Length - 2 * uncensor) +
					token.Substring(token.Length - uncensor);
				log.Debug("token: " + token_censored, 1);
			} else {
				log.Error("Authentication token missing!", 1);
				log.Error("Cannot connect to Discord.", 1);
				return;
			}

			// Instantiate discord client.
			discord = new DiscordClient(new DiscordConfiguration {
				Token = token,
				TokenType = TokenType.Bot
			});
			log.Info("Connecting to discord servers...");
		}

		static async Task ExportSettings() {
			await Settings.Export(path_settings, discord, settings);
		}

		static async Task<BulletinData?> ParseMessage(DiscordMessage message) {
			// Strip @mentions
			string command = message.Content;
			command = Regex.Replace(command, @"<@[!&]?\d+>", "");
			command = command.Trim();

			// Separate command into component parts
			Regex regex_command = new Regex(@"^(?:-(\S+))?\s*(?:!(\S+))?\s*(?:(.+))?$");
			Match match = regex_command.Match(command);
			string command_option	= match.Groups[1].Value.ToLower();
			string command_mention	= match.Groups[2].Value;
			string command_data		= match.Groups[3].Value;

			// Decide what to do with message
			bool is_guild = (message.Channel.Type == ChannelType.Text);
			DiscordGuild? guild = message.Channel.Guild;
			Settings? settings = null;
			if (guild != null)
				settings = Program.settings[guild.Id];

			// Handle help command
			if (IsRequestHelp(command_option)) {
				DiscordChannel channel = message.Channel;
				if (!channel.IsPrivate) {
					// TODO: only way this can fail is if message needing help
					// is sent from non-guild, non-private channel (e.g. group DM)
					DiscordMember member =
						GetDiscordMember(message.Author, guild!)!;
					channel = await member.CreateDmChannelAsync();
				}
				await SendHelpText(channel);
				return null;
			}

			// Handle config command
			if (IsRequestConfig(command_option)) {
				Regex regex_config = new Regex(@"(?:<(.+?)>\s+)?(?:channel\s+(\S+)|mention\s+(.+))");
				Match match_config = regex_config.Match(command_data);
				// TODO: check for failed matching
				string command_guild = match_config.Groups[1].Value;
				string command_config =
					match_config.Groups[2].Value +
					match_config.Groups[3].Value;
				// TODO: warn on specified guild not matching owned guild
				// (possible overspecification or insufficient permissions)

				DiscordUser owner = message.Author;
				List<DiscordGuild> guilds_owned = new List<DiscordGuild>();
				foreach (KeyValuePair<ulong, DiscordMember> pair in owners) {
					if (pair.Value == owner)
						guilds_owned.Add(await discord.GetGuildAsync(pair.Key));
				}
				DiscordChannel channel = message.Channel;
				if (!channel.IsPrivate) {
					command_guild = channel.Guild.Name;
					// TODO: only time this is wrong is if message needing help
					// is sent from non-guild, non-private channel (e.g. group DM)
				}
				// TODO: log error if not owner of any guilds
				if (
					guilds_owned.Count > 1 &&
					command_guild != string.Empty
				) {
					List<DiscordGuild> guild_specified = new List<DiscordGuild>();
					foreach (DiscordGuild guild_choose in guilds_owned) {
						if (guild_choose.Name == command_guild) {
							guild_specified.Add(guild_choose);
						}
					}
					guilds_owned = guild_specified;
				}
				if (guilds_owned.Count > 1) {
					DiscordGuild guild_example = guilds_owned[0];
					string helptext =
						"You are the owner of multiple guilds :confused:\n" +
						"You'll need to specify which guild to configure, e.g.:\n" +
						("@Puck -config <" + guild_example.Name + ">" +
						" channel {channel-name}").Code() ;
					await discord.SendMessageAsync(channel, helptext);
				} else {
					await SetConfig(guilds_owned[0], command_data);
					await discord.SendMessageAsync(channel, "Settings updated. :white_check_mark:");
				}
				return null;
			}

			// Non-help/config sent to non-guild channel: Fail silently.
			// TODO: warn user? + log
			if (settings == null)
				return null;	// fail if settings haven't been found
			return BulletinData.Parse(
				command_option,
				command_mention,
				command_data,
				message,
				settings!	// just ensured non-null with earlier check
			);
		}

		static async Task SendHelpText(DiscordChannel channel) {
			string helptext =
				"(To show this help text, use the command `@Puck -help`.)\n" +
				"`@Puck lfm AD+5` creates a group with no extra options.\n" +
				"`@Puck !KSM JY+16 completion` pings the \"KSM\" role, if available.\n" +
				"`@Puck -raid M BoD mount run` formats the post as a raid group.\n" +
				"other group types include: `island`, `vision`, etc.\n" +
				"`@Puck -config channel lfg` sets the post channel.\n" +
				"`@Puck -config mention none` sets the default mention role.";
			await discord.SendMessageAsync(channel, helptext);
		}

		static async Task SetConfig(DiscordGuild guild, string command) {
			if (!settings.ContainsKey(guild.Id)) {
				settings.Add(guild.Id, new Settings(null));
			}
			if (command.StartsWith("channel")) {
				string channel_str = command.Replace("channel", "").Trim();
				foreach (DiscordChannel channel in guild.Channels.Values) {
					if (channel.Name == channel_str) {
						settings[guild.Id].bulletin = channel;
						break;
					}
				}
			}
			if (command.StartsWith("mention")) {
				string mention_str = command.Replace("mention", "").Trim();
				foreach (DiscordRole role in guild.Roles.Values) {
					if (role.Name == mention_str) {
						settings[guild.Id].default_mention = role;
						break;
					}
				}
				if (mention_str == "everyone") {
					settings[guild.Id].default_mention = guild.EveryoneRole;
				}
				if (mention_str == "none") {
					settings[guild.Id].default_mention = null;
				}
			}
			await ExportSettings();
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

			void UpdateData() {
				bulletins[message_id].data = data;
			}
			async void DeleteReaction() {
				await bulletins[message_id].message.
					DeleteReactionAsync(str_to_emoji[emoji_str], e.User);
				// TODO: add removal reason (for audit logs)
			}

			// Global controls (refresh/delist)
			if (is_owner) {
				switch (emoji_str) {
				case emoji_refresh_str:
					data.expiry += settings[e.Guild.Id].increment;
					DeleteReaction();
					break;
				case emoji_delist_str:
					data.expiry = DateTimeOffset.Now;
					DeleteReaction();
					break;
				}
				// Cannot put `DeleteReaction()` here: logic bug.
				// The captured `emoji_str` is different.
				UpdateData();
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
					DeleteReaction();
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
				}
				UpdateData();
				break;
			case Group.Type.Raid:
			case Group.Type.Warfront:
			case Group.Type.Other:
				switch (emoji_str) {
				case emoji_tank_str:
					++data.group.tank;
					break;
				case emoji_heal_str:
					++data.group.heal;
					break;
				case emoji_dps_str:
					++data.group.dps;
					break;
				}
				UpdateData();
				break;
			case Group.Type.Scenario:
			case Group.Type.Island:
				if (is_owner) {
					switch (emoji_str) {
					case emoji_dps_str:
						++data.group.dps;
						data.group.dps %= 4;
						break;
					}
					DeleteReaction();
				} else {
					switch (emoji_str) {
					case emoji_dps_str:
						data.group.dps = Math.Max(++data.group.dps, 3);
						break;
					}
				}
				UpdateData();
				break;
			case Group.Type.Vision:
				switch (emoji_str) {
				case emoji_tank_str:
					if (data.group.members() < 5)
						++data.group.tank;
					break;
				case emoji_heal_str:
					if (data.group.members() < 5)
						++data.group.heal;
					break;
				case emoji_dps_str:
					if (data.group.members() < 5)
						++data.group.dps;
					break;
				}
				UpdateData();
				break;
			}

			await bulletins[message_id].Update();
		}

		static async Task UpdateFromControls(MessageReactionRemoveEventArgs e) {
			ulong message_id = e.Message.Id;
			string emoji_str = e.Emoji.GetDiscordName();
			BulletinData data = bulletins[message_id].data;

			// Group.Type-specific controls
			switch (bulletins[message_id].data.group.type) {
			case Group.Type.Dungeon:
				if (e.User == data.owner)
					break;
				switch (emoji_str) {
				case emoji_tank_str:
					data.group.tank = Math.Min(--data.group.tank, 0);
					break;
				case emoji_heal_str:
					data.group.heal = Math.Min(--data.group.heal, 0);
					break;
				case emoji_dps_str:
					data.group.dps = Math.Min(--data.group.dps, 0);
					break;
				}
				bulletins[message_id].data = data;
				break;
			case Group.Type.Raid:
			case Group.Type.Warfront:
			case Group.Type.Vision:
			case Group.Type.Other:
				// Not `break`ing if the reaction was by the owner,
				// since owner reactions aren't auto-removed for these.
				switch (emoji_str) {
				case emoji_tank_str:
					data.group.tank = Math.Min(--data.group.tank, 0);
					break;
				case emoji_heal_str:
					data.group.heal = Math.Min(--data.group.heal, 0);
					break;
				case emoji_dps_str:
					data.group.dps = Math.Min(--data.group.dps, 0);
					break;
				}
				bulletins[message_id].data = data;
				break;
			case Group.Type.Scenario:
			case Group.Type.Island:
				if (e.User == data.owner)
					break;
				switch (emoji_str) {
				case emoji_dps_str:
					data.group.dps = Math.Min(--data.group.dps, 0);
					break;
				}
				bulletins[message_id].data = data;
				break;
			}

			await bulletins[message_id].Update();
		}
	}
}
