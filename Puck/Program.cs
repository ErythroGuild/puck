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
		static readonly Logger log = new Logger();

		// `=null!` late init -- this is supposed to be ugly
		// only possible when guaranteed to terminate if cannot connect
		static DiscordClient puck = null!;
		static Dictionary<ulong, Settings> settings		= new Dictionary<ulong, Settings>();
		static Dictionary<ulong, Bulletin> bulletins	= new Dictionary<ulong, Bulletin>();

		const string path_token		= @"token.txt";
		const string path_settings	= @"settings.txt";
		const ulong channel_debug_id = 489274692255875091;  // <Erythro> - #test

		static DiscordEmoji?
			emoji_tank,
			emoji_heal,
			emoji_dps,
			emoji_refresh,
			emoji_delist;
		const string
			emoji_tank_str		= ":shield:",
			emoji_heal_str		= ":flag_ch:",
			emoji_dps_str		= ":archery:",
			emoji_refresh_str	= ":arrows_counterclockwise:",
			emoji_delist_str	= ":white_check_mark:";

		public static Dictionary<string, DiscordEmoji>? str_to_emoji;
		public static DiscordEmoji? GetEmoji(Group.Role role) {
			return role switch {
				Group.Role.Tank => emoji_tank,
				Group.Role.Heal => emoji_heal,
				Group.Role.Dps  => emoji_dps,
				_ => null,
			};
		}
		public static ref readonly Logger GetLogger() { return ref log; }
		public static Settings GetSettings(ulong guild_id) { return settings[guild_id]; }

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
			log.type_minimum = Logger.Type.Debug;
			MainAsync().ConfigureAwait(false).GetAwaiter().GetResult();
		}

		static async Task MainAsync() {
			log.Info("Initializing...");
			Connect();
			if (puck == null) {
				log.Error("Terminating program.");
				return;
			}

			puck.Ready += async e => {
				log.Info("Connected to discord.");
				log.Debug("Setting up emojis...", 1);

				// Initialize emojis.
				emoji_tank = DiscordEmoji.FromName(puck, emoji_tank_str);
				emoji_heal = DiscordEmoji.FromName(puck, emoji_heal_str);
				emoji_dps  = DiscordEmoji.FromName(puck, emoji_dps_str);
				emoji_refresh = DiscordEmoji.FromName(puck, emoji_refresh_str);
				emoji_delist  = DiscordEmoji.FromName(puck, emoji_delist_str);

				str_to_emoji = new Dictionary<string, DiscordEmoji> {
					{ emoji_tank_str,       emoji_tank },
					{ emoji_heal_str,       emoji_heal },
					{ emoji_dps_str,        emoji_dps },
					{ emoji_refresh_str,    emoji_refresh },
					{ emoji_delist_str,     emoji_delist },
				};

				// Set "custom status".
				log.Info("Setting bot custom status...", 1);
				DiscordActivity helptext =
					new DiscordActivity(@"#lfg for pings", ActivityType.Watching);
				await puck.UpdateStatusAsync(helptext);
				log.Info("Custom status set.", 1);
			};

			puck.GuildDownloadCompleted += async e => {
				log.Info("Connected to specific servers.");
				log.Debug("Populating server-specific settings...");

				// Set up default config
				settings = await Settings.Import(path_settings, puck);
				foreach (DiscordGuild guild in puck.Guilds.Values) {
					if (!settings.ContainsKey(guild.Id)) {
						DiscordChannel channel_default = puck.Guilds[guild.Id].GetDefaultChannel();
						Settings settings_default = new Settings(channel_default);
						settings.Add(guild.Id, settings_default);
					}
				}
				await ExportSettings(puck);

				puck.GuildCreated += async e_guild => {
					DiscordGuild guild = e_guild.Guild;
					log.Info("New server added! - " + guild.Name, 0, guild.Id);

					settings.TryAdd(guild.Id, new Settings(null));
					await ExportSettings(puck);

					log.Info("Sending config help to server owner...", 1, guild.Id);
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
					log.Debug("Config help sent.", 1, guild.Id);
				};

				puck.GuildDeleted += async e_guild => {
					DiscordGuild guild = e_guild.Guild;
					log.Info("Removed from server: " + guild.Name, 0, guild.Id);
					log.Info("Removing old settings...", 0, guild.Id);
					settings.Remove(guild.Id);
					await ExportSettings(puck, false);
				};
			};

			puck.MessageCreated += async e => {
				if (e.Message.Author.Username == puck.CurrentUser.Username) {
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
					if (role.Name == puck.CurrentUser.Username) {
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
						log.Warning("No bulletin created.", 1, e.Message.Id);
						return;
					}
					DiscordChannel? channel = settings[e.Guild.Id].bulletin;
#if DEBUG
					channel = await puck.GetChannelAsync(channel_debug_id);
#endif
					if (channel == null) {
						log.Error("Failed to find channel to post in.", 1, e.Message.Id);
						return;
					}

					log.Info("Posting bulletin...", 1, e.Message.Id);
					DiscordMessage message =
						await puck.SendMessageAsync(channel, data.ToString());
					await CreateControls(message, data.group.type);

					Bulletin bulletin = new Bulletin(message, data);
					bulletins.Add(message.Id, bulletin);
					bulletin.Delisted += (o, message_id) => {
						log.Info("Delisting group.", 0, message.Id);
						bulletins.Remove(message_id);
					};
				}
			};

			puck.MessageReactionAdded += async e => {
				if (bulletins.ContainsKey(e.Message.Id)) {
					// No need to respond to bot's own reactions
					if (e.User == puck.CurrentUser)
						return;

					string log_str =
						"button pressed: " + e.Emoji.GetDiscordName() +
						" (" + e.User.Userstring() + ")";
					log.Info(log_str, 1, e.Message.Id);

					await UpdateFromControls(e);
				}
			};
			puck.MessageReactionRemoved += async e => {
				if (bulletins.ContainsKey(e.Message.Id)) {
					// No need to respond to bot's own reactions
					if (e.User == puck.CurrentUser)
						return;

					string log_str =
						"button unpressed: " + e.Emoji.GetDiscordName() +
						" (" + e.User.Userstring() + ")";
					log.Info(log_str, 1, e.Message.Id);

					await UpdateFromControls(e);
				}
			};

			await puck.ConnectAsync();
			log.Info("Monitoring messages...");

			await Task.Delay(-1);
		}

		// Init discord client with token from text file.
		// This allows the token to be separated from source code.
		static void Connect() {
			log.Info("Reading authentication token...", 1);

			// Open text file.
			StreamReader file;
			try {
				file = File.OpenText(path_token);
			} catch (Exception) {
				log.Error("Could not open \"" + path_token + "\".", 1);
				log.Error("Cannot connect to Discord.", 1);
				return;
			}

			// Read text file.
			string token = file.ReadLine() ?? "";
			if (token != "") {
				log.Info("Authentication token found.", 1);
				int uncensor = 8;
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
			puck = new DiscordClient(new DiscordConfiguration {
				Token = token,
				TokenType = TokenType.Bot
			});
			log.Info("Connecting to discord...");
		}

		// Convenience wrapper for exporting current settings.
		// Directly exports from static member variables.
		static async Task ExportSettings(DiscordClient client, bool do_keep_cache = true) {
			await Settings.Export(path_settings, client, settings, do_keep_cache);
		}

		static async Task<BulletinData?> ParseMessage(DiscordMessage message) {
			// Strip @mentions
			string command = message.Content;
			command = Regex.Replace(command, @"<@[!&]?\d+>", "");
			command = command.Trim();
			log.Debug("Trimmed message:", 1, message.Id);
			log.Debug(command, 2, message.Id);

			// Separate command into component parts
			Regex regex = new Regex(@"^(?:-(\S+))?\s*(?:!(\S+))?\s*(?:(.+))?$");
			Match match = regex.Match(command);
			string command_option  = match.Groups[1].Value.ToLower();
			string command_mention = match.Groups[2].Value;
			string command_data    = match.Groups[3].Value;
			log.Debug("option:  " + command_option,  1, message.Id);
			log.Debug("mention: " + command_mention, 1, message.Id);
			log.Debug("data:    " + command_data,    1, message.Id);

			// Handle help command
			if (IsHelp(command_option)) {
				DiscordChannel? channel = await Util.GetPrivateChannel(message);
				if (channel == null) {
					log.Error("Cannot send help text to user:", 0, message.Id);
					log.Info("User: " + message.Author.Userstring(), 1, message.Id);
					return null;
				}
				await SendHelpText(channel);
				return null;
			}

			// Handle config commands
			if (IsConfig(command_option)) {
				await ParseConfig(command_data, message);
				return null;
			}

			// Create bulletin from message
			DiscordGuild? guild = message.Channel.Guild;
			if (guild == null) {
				log.Warning("Tried to post bulletin from non-server channel.", 0, message.Id);
				return null;
			}
			if (!Program.settings.ContainsKey(guild.Id)) {
				log.Warning("Tried to post bulletin to un-configured server.", 0, message.Id);
				return null;
			}
			Settings settings = Program.settings[guild.Id];
			return BulletinData.Parse(
				command_option,
				command_mention,
				command_data,
				message,
				settings
			);
		}

		static async Task SendHelpText(DiscordChannel channel) {
			log.Info("Sending help text...", 1, channel.Id);
			string helptext =
				"(To show this help text, use the command `@Puck -help`.)\n" +
				"`@Puck lfm AD+5` creates a group with no extra options.\n" +
				"`@Puck !KSM JY+16 completion` pings the \"KSM\" role, if available.\n" +
				"`@Puck -raid M BoD mount run` formats the post as a raid group.\n" +
				"other group types include: `island`, `vision`, etc.\n" +
				"`@Puck -config channel lfg` sets the post channel.\n" +
				"`@Puck -config mention none` sets the default mention role.";
			await puck.SendMessageAsync(channel, helptext);
			log.Debug("Help text sent.", 1, channel.Id);
		}

		static async Task ParseConfig(string command, DiscordMessage message) {
			command = command.Trim();
			log.Debug("Config command:", 1, message.Id);
			log.Debug(command, 2, message.Id);

			// Separate command into component parts
			Regex regex = new Regex(@"^(?:<(.+?)>)?\s*(?:(\S+))\s*(.+)?$");
			Match match = regex.Match(command);
			string command_guild  = match.Groups[1].Value;
			string command_action = match.Groups[2].Value.ToLower();
			string command_data   = match.Groups[3].Value;
			log.Debug("guild:  " + command_guild,  1, message.Id);
			log.Debug("action: " + command_action, 1, message.Id);
			log.Debug("data:   " + command_data,   1, message.Id);

			// Validate command
			switch (command_action) {
			case "view":
			case "channel":
			case "mention":
				break;
			default:
				log.Warning("Invalid command", 1, message.Id);
				return;
			}

			// Fetch DM channel early (for replying to errors)
			DiscordChannel? channel = await Util.GetPrivateChannel(message);
			if (channel == null) {
				log.Warning("Could not open a DM channel to user.", 0, message.Id);
				log.Debug("User: " + message.Author.Userstring(), 0, message.Id);
			}

			// Figure out which guild is being configured
			DiscordUser owner = message.Author;
			List<DiscordGuild> guilds_owned = GetOwnedGuilds(owner);
			DiscordGuild? guild_config = null;
			switch (guilds_owned.Count) {
			// only triggers default: if somehow List size is negative?
			default:
				log.Error("Server owners `List<>.Count` is negative.", 0, message.Id);
				log.Info("User: " + owner.Userstring(), 0, message.Id);
				return;
			case 0:
				log.Warning("Could not find any owned servers.", 1, message.Id);
				log.Debug("User: " + owner.Userstring(), 1, message.Id);
				return;
			case var _ when (guilds_owned.Count > 1):
				if (command_guild == string.Empty) {
					if (!message.Channel.IsPrivate) {
						log.Info("Detected server from message channel.", 1, message.Id);
						guild_config = message.Channel.Guild;
						break;
					}
					log.Warning("Owner of multiple guilds.", 1, message.Id);
					log.Info("User: " + owner.Userstring(), 1, message.Id);
					foreach (DiscordGuild guild in guilds_owned) {
						log.Debug(guild.Name, 2, message.Id);
					}
					string helptext =
						"You are the owner of multiple servers :confused:\n" +
						"You'll need to specify which server to configure, e.g.:\n" +
						("@Puck -config " + guilds_owned[0].Guildstring() +
						" channel {channel-name}").Code();
					await puck.SendMessageAsync(channel, helptext);
					return;
				}
				foreach (DiscordGuild guild in guilds_owned) {
					if (guild.Name == command_guild) {
						guild_config = guild;
						break;
					}
				}
				if (guild_config == null) {
					log.Warning("Could not find <" + command_guild + ">", 1, message.Id);
					log.Info("User: " + owner.Userstring(), 1, message.Id);
					foreach (DiscordGuild guild in guilds_owned) {
						log.Debug(guild.Name, 2, message.Id);
					}
					string helptext =
						"Could not find your specified server :confused:\n" +
						"You may not have permissions to that server.";
					await puck.SendMessageAsync(channel, helptext);
					return;
				}
				break;
			case 1:
				guild_config = guilds_owned[0];
				if (
					guild_config.Name != command_guild &&
					command_guild != string.Empty
				) {
					log.Warning("Specified server does not match owned server.", 1, message.Id);
					log.Info("Specified server: <" + command_guild +">", 2, message.Id);
					log.Info("Owned server:     " + guild_config.Guildstring(), 2, message.Id);
				}
				break;
			}
			log.Info("Server to configure: " + guild_config.Name, 1, message.Id);

			// Set configuration settings
			log.Info("Setting configuration...", 1, message.Id);
			if (!settings.ContainsKey(guild_config.Id)) {
				settings.Add(guild_config.Id, new Settings(null));
			}
			switch (command_action) {
			case "view":
				string settings_text =
					"Settings for " + guild_config.Name.Bold() + "\n" +
					"channel: " +
					(settings[guild_config.Id].bulletin?.Channelstring()
						?? "not set".Italics()) + "\n" +
					"mention: " +
					(settings[guild_config.Id].default_mention?.Rolestring()
						?? "not set".Italics() + " (no ping)");
				await puck.SendMessageAsync(channel, settings_text);
				return;
			case "channel":
				foreach (DiscordChannel channel_set in guild_config.Channels.Values) {
					if (channel_set.Name == command_data) {
						settings[guild_config.Id].bulletin = channel_set;
						break;
					}
				}
				break;
			case "mention":
				foreach (DiscordRole role in guild_config.Roles.Values) {
					if (role.Name == command_data) {
						settings[guild_config.Id].default_mention = role;
						break;
					}
				}
				if (command_data == "everyone") {
					settings[guild_config.Id].default_mention = guild_config.EveryoneRole;
				}
				if (command_data == "none") {
					settings[guild_config.Id].default_mention = null;
				}
				break;
			}

			await ExportSettings(puck);
			await puck.SendMessageAsync(channel, "Settings updated. :white_check_mark:");
		}

		static List<DiscordGuild> GetOwnedGuilds(DiscordUser owner) {
			List<DiscordGuild> guilds_owned = new List<DiscordGuild>();
			foreach (DiscordGuild guild in puck.Guilds.Values) {
				if (guild.Owner == owner) {
					guilds_owned.Add(guild);
				}
			}
			return guilds_owned;
		}

		static async Task CreateControls(DiscordMessage message, Group.Type type) {
			log.Info("Creating controls...", 1, message.Id);

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

			log.Info("Controls created.", 1, message.Id);
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

		static bool IsHelp(string command) {
			return command switch
			{
				"help"	=> true,
				"h"		=> true,
				"?"		=> true,
				_ => false,
			};
		}
		static bool IsConfig(string command) {
			return command switch
			{
				"config"		=> true,
				"configuration"	=> true,
				"conf"			=> true,
				"cfg"			=> true,
				_ => false,
			};
		}
	}
}
