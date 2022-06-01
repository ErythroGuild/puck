using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

using Spectre.Console;

namespace Puck;

class Program {
	// Debug flag.
	public static bool IsDebug {
		get {
			bool isDebug = false;
			CheckDebug(ref isDebug);
			return isDebug;
		}
	}
	// Discord client objects.
	public static DiscordClient Client { get; private set; }
	//// `=null!` late init -- this is supposed to be ugly
	//// only possible when guaranteed to terminate if cannot connect
	//static DiscordClient puck = null!;

	// Separate logger pipeline for D#+.
	private static Serilog.ILogger _loggerDsp;

	// Diagnostic timers.
	private static readonly Stopwatch
		_stopwatchConfig   = new (),
		_stopwatchConnect  = new (),
		_stopwatchDownload = new (),
		_stopwatchRegister = new ();

	// File paths for config files.
	private const string
		_pathToken = @"config/token.txt",
		_pathTokenDebug = @"config/token_debug.txt",
		_pathLogs = @"logs/";

	// Date / time format strings.
	private const string
		_formatLogs = @"yyyy-MM\/lo\g\s-MM-dd";

	// Serilog message templates.
	private const string
		_templateConsoleDebug   = @"[grey]{Timestamp:H:mm:ss} [{Level:w4}] {Message:lj}[/]{NewLine}{Exception}",
		_templateConsoleInfo    = @"[grey]{Timestamp:H:mm:ss}[/] [silver][{Level:w4}][/] {Message:lj}{NewLine}{Exception}",
		_templateConsoleWarning = @"[grey]{Timestamp:H:mm:ss}[/] [yellow][{Level:u4}][/] {Message:lj}{NewLine}{Exception}",
		_templateConsoleError   = @"[red]{Timestamp:H:mm:ss}[/] [invert red][{Level}][/] {Message:lj}{NewLine}{Exception}",
		_templateFile           = @"{Timestamp:yyyy-MM-dd HH:mm:ss.fff} > [{Level:u3}] {Message:j}{NewLine}{Exception}";

	static Program() {
		const string logo =
			"""
				  ______           _    
				  | ___ \         | |   
				  | |_/ /   _  ___| | __
				  |  __/ | | |/ __| |/ /
				  | |  | |_| | (__|   < 
				  \_|   \__,_|\___|_|\_\
				""";
		AnsiConsole.Markup(logo);
		AnsiConsole.WriteLine();

		InitSerilog();
		Log.Information("Logging initialized (Serilog).");

		// Parse authentication token from file.
		// Terminate if token is not found.
		string bot_token = "";
		string path_token = IsDebug
			? _pathTokenDebug
			: _pathToken;
		using (StreamReader token = File.OpenText(path_token)) {
			Log.Debug("  Token file opened.");
			bot_token = token.ReadLine() ?? "";
		}
		if (bot_token != "") {
			Log.Information("  Authentication token found.");
			int disp_size = 8;
			string token_disp =
				bot_token[..disp_size] +
				new string('*', bot_token.Length - 2*disp_size) +
				bot_token[^disp_size..];
			Log.Debug("    {DisplayToken}", token_disp);
			Log.Verbose("    {Token}", bot_token);
		} else {
			Log.Fatal("  No authentication token found.");
			Log.Debug("    Path: {TokenPath}", path_token);
			throw new FormatException($"Could not find auth token at {path_token}.");
		}

		// Initialize Discord client.
		Client = new DiscordClient(new DiscordConfiguration {
			Intents = DiscordIntents.AllUnprivileged,
			LoggerFactory = new LoggerFactory().AddSerilog(_loggerDsp),
			Token = bot_token,
			TokenType = TokenType.Bot
		});
		Log.Information("  Discord client configured.");
		Log.Debug("  Serilog attached to D#+.");
	}

	// A dummy function to force the static constructor to run.
	private static void InitStatic() { }
	// Set up and configure Serilog.
	[MemberNotNull(nameof(_loggerDsp))]
	private static void InitSerilog() {
		// General logs (all logs except D#+).
		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Debug()
			// Spectre.Console colorizes/formats any logs.
			.WriteTo.Map(
				e => e.Level,
				(level, writeTo) => writeTo.DelegatingTextSink(
					s => {
						s = s.EscapeMarkup()
							.Replace(@"[[/]]", @"[/]")
							.Replace(@"[[grey]]", @"[grey]")
							.Replace(@"[[silver]]", @"[silver]")
							.Replace(@"[[yellow]]", @"[yellow]")
							.Replace(@"[[red]]", @"[red]")
							.Replace(@"[[invert red]]", @"[invert red]");
						AnsiConsole.Markup(s);
					},
					outputTemplate: level switch {
						Serilog.Events.LogEventLevel.Debug or
						Serilog.Events.LogEventLevel.Verbose =>
							_templateConsoleDebug,
						Serilog.Events.LogEventLevel.Information =>
							_templateConsoleInfo,
						Serilog.Events.LogEventLevel.Warning =>
							_templateConsoleWarning,
						Serilog.Events.LogEventLevel.Error or
						Serilog.Events.LogEventLevel.Fatal =>
							_templateConsoleError,
						_ =>
							_templateConsoleInfo,
					}
				)
			)
			// New directories are created for every month of logs.
			.WriteTo.Map(
				e => DateTime.Now.ToString(_formatLogs),
				(prefix, writeTo) => writeTo.File(
					$"{_pathLogs}{prefix}.txt",
					outputTemplate: _templateFile,
					retainedFileTimeLimit: null
				)
			)
			.CreateLogger();

		// D#+ logs.
		_loggerDsp = new LoggerConfiguration()
			.MinimumLevel.Information()
			// New directories are created for every month of logs.
			.WriteTo.Map(
				e => {
					string prefix = DateTime.Now.ToString(_formatLogs);
					return prefix.Replace(@"logs-", @"logs-DSharpPlus-");
				},
				(prefix, writeTo) => writeTo.File(
					$"{_pathLogs}{prefix}.txt",
					outputTemplate: _templateFile,
					retainedFileTimeLimit: null
				)
			)
			.CreateLogger();
	}

	public static void Main() {
		// Initialize static members.
		InitStatic();

		// Run async entry point.
		MainAsync()
			.ConfigureAwait(false)
			.GetAwaiter()
			.GetResult();
	}



	/////////////////////////////////////// vvvvvvv

	static readonly Blocklist blocklist = new (path_blocklist);

	static Dictionary<ulong, Settings> settings  = new ();
	static Dictionary<ulong, Bulletin> bulletins = new ();

	const string path_settings  = @"config/settings.txt";
	const string path_blocklist = @"config/blocklist.txt";
	const ulong channel_debug_id = 489274692255875091;  // <Erythro> - #test

	public static Settings GetSettings(ulong guild_id) =>
		settings[guild_id];

	////////////////////////////////////// ^^^^^^




	static async Task MainAsync() {
		// Start configuration timer.
		_stopwatchConfig.Start();

		// Connected to discord servers (but not necessarily guilds yet!).
		Client.Ready += (client, e) => {
			_ = Task.Run(async () => {
				Log.Information("  Logged in to Discord servers.");
				_stopwatchConnect.LogMsecDebug("    Took {ConnectionTime} msec.");

				DiscordActivity status =
					new ("matchmaker", ActivityType.Playing);
				await client.UpdateStatusAsync(status);
				Log.Debug("  Custom status set.");
			});
			return Task.CompletedTask;
		};

		// Guild data has finished downloading.
		Client.GuildDownloadCompleted += (client, e) => {
			_ = Task.Run(async () => {
				// Stop download timer.
				Log.Information("  Downloaded guild data from Discord.");
				_stopwatchDownload.LogMsecDebug("    Took {DownloadTime} msec.");

				Emoji.Init(Client);
				Log.Debug("  Initialized emojis.");

				// Set up default config.
				settings = await Settings.Import(path_settings, Client);
				foreach (DiscordGuild guild in Client.Guilds.Values) {
					if (!settings.ContainsKey(guild.Id)) {
						DiscordChannel channel_default = Client.Guilds[guild.Id].GetDefaultChannel();
						Settings settings_default = new (channel_default);
						settings.Add(guild.Id, settings_default);
					}
				}
				await ExportSettings(Client);
				Log.Information("  Populated server-specific settings.");

				Client.GuildCreated += (client, e) => {
					_ = Task.Run(async () => {
						Log.Information("New guild added! - {GuildName}", e.Guild.Name);

						settings.TryAdd(e.Guild.Id, new Settings(null));
						await ExportSettings(Client);

						await e.Guild.Owner.SendMessageAsync(
							$""""
							Hello!
							I've just been added to your server, {e.Guild.Name.Bold()} :wave:
							""""
						);
						Log.Debug("  Sent config guide to server owner.");
					});
					return Task.CompletedTask;
				};

				Client.GuildDeleted += (client, e) => {
					_ = Task.Run(async () => {
						Log.Information("Removed from guild - {GuildName}", e.Guild.Name);
						settings.Remove(e.Guild.Id);
						await ExportSettings(Client, false);
						Log.Debug("  Removed guild settings.");
					});
					return Task.CompletedTask;
				};
			});
			return Task.CompletedTask;
		};




		//////////////////////////////////////////////// vvvvvvv


		puck.MessageCreated += async e => {
			if (e.Message.Author.IsCurrent) {
				return; // never respond to self
			}
			if (e.Message.Author.IsBot) {
				return; // never respond to other bots
			}

			bool is_mentioned = IsMessageMentioned(e.Message);
			if (is_mentioned) {
				_ = e.Message.Channel.TriggerTypingAsync();
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

				Bulletin bulletin = new Bulletin(message, data, e.Message.Id);
				if (blocklist.Contains(e.Message.Author.Id)) {
					bulletin.do_notify_owner = false;
				}
				bulletins.Add(message.Id, bulletin);
				bulletin.Delisted += (o, message_id) => {
					log.Info("Delisting group.", 0, message.Id);
					bulletins.Remove(message_id);
				};
			}
		};

		puck.MessageUpdated += async e => {
			Bulletin? bulletin = null;
			foreach (Bulletin bulletin_i in bulletins.Values) {
				if (e.Message.Id == bulletin_i.original_id) {
					bulletin = bulletin_i;
					break;
				}
			}

			if (bulletin != null) {
				log.Info("Existing message updated.", 0, e.Message.Id);
				bool is_mentioned = IsMessageMentioned(e.Message);

				if (!is_mentioned) {
					log.Info("New message no longer mentions this bot.", 0, e.Message.Id);
					log.Info("Delisting previously posted bulletin.", 0, e.Message.Id);
					bulletin.data.expiry = DateTimeOffset.Now;
					await bulletin.Update();
					return;
				}
				log.Info("Raw message:", 0, e.Message.Id);
				log.Info(e.Message.Content, 0, e.Message.Id);
				Group group_old = bulletin.data.group;

				BulletinData? data = await ParseMessage(e.Message);
				if (data == null) {
					log.Warning("No bulletin can be created.", 1, e.Message.Id);
					log.Info("Delisting previously posted bulletin.", 1, e.Message.Id);
					bulletin.data.expiry = DateTimeOffset.Now;
					await bulletin.Update();
					return;
				}

				log.Info("Updating bulletin...", 0, e.Message.Id);
				bulletin.data = data;
				if (data.group.type == group_old.type) {
					bulletin.data.group = group_old;
				}
				bulletins[bulletin.message.Id] = bulletin;
					await bulletin.message.ModifyAsync(bulletin.data.ToString());
				if (bulletin.data.group.type != group_old.type) {
					log.Info("Group type changed on update.", 1, e.Message.Id);
					log.Debug("Resetting reactions...", 1, e.Message.Id);
					await bulletin.message.DeleteAllReactionsAsync("Bulletin group type changed.");
					await CreateControls(bulletin.message, data.group.type);
				}
			}
		};

		puck.MessageDeleted += async e => {
			Bulletin? bulletin = null;
			foreach (Bulletin bulletin_i in bulletins.Values) {
				if (e.Message.Id == bulletin_i.original_id) {
					bulletin = bulletin_i;
					break;
				}
			}
			if (bulletin != null) {
				log.Info("Existing message deleted.", 0, e.Message.Id);
				log.Info("Deleting previously posted bulletin...", 1, e.Message.Id);
				bulletin.data.expiry = DateTimeOffset.Now;
				await bulletin.Update();
				await bulletin.message.DeleteAsync("Original post deleted.");
				log.Debug("Bulletin deleted.", 1, e.Message.Id);
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


		/////////////////////////////// ^^^^^^



		// Stop configuration timer.
		Log.Debug("  Configured Discord client.");
		_stopwatchConfig.LogMsecDebug("    Took {ConfigTime} msec.");

		// Start connection timer and connect.
		_stopwatchConnect.Start();
		_stopwatchDownload.Start();
		await Client.ConnectAsync();
		await Task.Delay(-1);
	}


	/////////////////////////////// vvvvvvv



	// Convenience wrapper for exporting current settings.
	// Directly exports from static member variables.
	static async Task ExportSettings(DiscordClient client, bool do_keep_cache = true) =>
		await Settings.Export(path_settings, client, settings, do_keep_cache);

	static bool IsMessageMentioned(DiscordMessage message) {
		foreach (DiscordUser mention in message.MentionedUsers) {
			if (mention.IsCurrent) {
				return true;
			}
		}
		foreach (DiscordRole role in message.MentionedRoles) {
			if (role.Name == Client.CurrentUser.Username) {
				Log.Debug("Role mention.");
				return true;
			}
		}
		return false;
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

		// Handle normal commands
		if (IsCommand(command_option)) {
			DiscordChannel? channel = await Util.GetPrivateChannel(message);
			if (channel == null) {
				log.Error("Cannot notify user:", 0, message.Id);
				log.Info("User: " + message.Author.Userstring(), 1, message.Id);
				return null;
			}
			await HandleCommand(command_option, message.Author, channel);
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

	static bool IsHelp(string command) {
		return command switch {
			"help"	=> true,
			"h"		=> true,
			"?"		=> true,
			_ => false,
		};
	}
	static bool IsConfig(string command) {
		return command switch {
			"config"		=> true,
			"configuration"	=> true,
			"conf"			=> true,
			"cfg"			=> true,
			_ => false,
		};
	}
	static bool IsCommand(string command) {
		return command switch {
			"mute"		=> true,
			"unmute"	=> true,
			_ => false,
		};
	}

	static async Task SendHelpText(DiscordChannel channel) {
		log.Info("Sending help text...", 1, channel.Id);
		string helptext =
			"(To show this help text, use the command `@Puck -help`.)\n" +
			"`@Puck lfm AD+5` creates a group with no extra options.\n" +
			"`@Puck !KSM JY+16 completion` pings the \"KSM\" role, if available.\n" +
			"`@Puck -raid M BoD mount run` formats the post as a raid group.\n" +
			"other group types include: `island`, `vision`, etc.\n" +
			"`@Puck -config <Erythro> channel lfg` sets the post channel.\n" +
			"`@Puck -config <Erythro> mention none` sets the default mention role.";
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
		DiscordGuild? guild_config = null;
		if (!message.Channel.IsPrivate) {
			log.Info("Detected server from message channel.", 1, message.Id);
			guild_config = message.Channel.Guild;
		} else if (command_guild != string.Empty) {
			foreach (DiscordGuild guild in puck.Guilds.Values) {
				if (guild.Name == command_guild) {
					log.Info("Found matching server.", 1, message.Id);
					guild_config = guild;
					break;
				}
			}
		} else {
			log.Warning("No server specified to config.", 1, message.Id);
			string text_help =
				":warning: " +
				"You'll need to specify which server to configure. e.g.:\n" +
				"@Puck -config <Erythro> channel general".Code();
			await puck.SendMessageAsync(channel, text_help);
			return;
		}
		if (guild_config == null) {
			log.Warning("No matching servers found.", 1, message.Id);
			string text_help =
				":warning: No servers found with the name " +
				command_guild.Bold();
			await puck.SendMessageAsync(channel, text_help);
			return;
		}
		log.Info("Server to configure: " + guild_config.Name, 1, message.Id);

		// Check for permission to configure server
		DiscordMember? member_author =
			message.Author.ToDiscordMember(guild_config);
		bool has_permission = false;
		if (member_author == null) {
			log.Warning("User is not a member of requested server.", 1, message.Id);
		} else {
			has_permission = Util.MemberHasPermissions(
				member_author,
				Permissions.ManageGuild
			);
		}
		if (!has_permission) {
			log.Info("User doesn't have permission to config server.", 1, message.Id);
			string text_permission =
				":warning: " +
				"You don't have permissions to manage that server.";
			await puck.SendMessageAsync(channel, text_permission);
			return;
		}

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
			settings[guild_config.Id].default_mention =
				MentionRole.FromName(command_data, guild_config);
			break;
		}

		await ExportSettings(puck);
		await puck.SendMessageAsync(channel, ":white_check_mark: Settings updated.");
	}

	static async Task HandleCommand(string command, DiscordUser user, DiscordChannel channel) {
		switch (command) {
		case "mute":
			string text_list_add =
				"Adding to blocklist: " + user.Userstring();
			log.Info(text_list_add, 0, user.Id);
			blocklist.Add(user.Id);
			blocklist.Export(path_blocklist);
			break;
		case "unmute":
			string text_list_rem =
				"Taking off blocklist: " + user.Userstring();
			log.Info(text_list_rem, 0, user.Id);
			blocklist.Remove(user.Id);
			blocklist.Export(path_blocklist);
			break;
		}
		log.Info("Notifying user...", 1, channel.Id);
		string text_update =
			":white_check_mark: Your preferences have been updated.\n" +
			"They will apply to all future groups you post.";
		await puck.SendMessageAsync(channel, text_update);
		log.Debug("User notified.", 1, channel.Id);
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
		Log.Information("Creating controls...");

		switch (type) {
		case Group.Type.Dungeon:
		case Group.Type.Raid:
		case Group.Type.Warfront:
		case Group.Type.Arenas:
		case Group.Type.RBG:
		case Group.Type.Battleground:
		case Group.Type.Vision:
		case Group.Type.Other:
			await message.CreateReactionAsync(Emoji.From(Emoji.Type.Tank));
			await message.CreateReactionAsync(Emoji.From(Emoji.Type.Heal));
			await message.CreateReactionAsync(Emoji.From(Emoji.Type.Dps));
			break;
		case Group.Type.Scenario:
		case Group.Type.Island:
			await message.CreateReactionAsync(Emoji.From(Emoji.Type.Dps));
			break;
		}
		await message.CreateReactionAsync(Emoji.From(Emoji.Type.Refresh));
		await message.CreateReactionAsync(Emoji.From(Emoji.Type.Delist));

		Log.Debug("Created controls.");
	}

	static async Task UpdateFromControls(MessageReactionAddEventArgs e) {
		ulong message_id = e.Message.Id;
		bool is_owner = (e.User == bulletins[message_id].data.owner);
		DiscordEmoji emoji = e.Emoji;
		Emoji.Type? emoji_type = Emoji.GetType(emoji);
		BulletinData data= bulletins[message_id].data;

		void UpdateData() {
			bulletins[message_id].data = data;
		}
		async void DeleteReaction() {
			await bulletins[message_id].message.
				DeleteReactionAsync(emoji, e.User);
			// TODO: add removal reason (for audit logs)
		}

		if (emoji_type == null) {
			Log.Information("  Reaction not recognized.");
			Log.Debug("    {EmojiString}", emoji);
			return;
		}

		// Global controls (refresh/delist)
		if (is_owner) {
			switch (emoji_type) {
			case Emoji.Type.Refresh:
				data.expiry += settings[e.Guild.Id].increment;
				DeleteReaction();
				break;
			case Emoji.Type.Delist:
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
				switch (emoji_type) {
				case Emoji.Type.Tank:
					++data.group.tank;
					data.group.tank %= 2;
					break;
				case Emoji.Type.Heal:
					++data.group.heal;
					data.group.heal %= 2;
					break;
				case Emoji.Type.Dps:
					++data.group.dps;
					data.group.dps %= 4;
					break;
				}
				DeleteReaction();
			} else {
				switch (emoji_type) {
				case Emoji.Type.Tank:
					if (data.group.tank < 1)
						++data.group.tank;
					break;
				case Emoji.Type.Heal:
					if (data.group.heal < 1)
						++data.group.heal;
					break;
				case Emoji.Type.Dps:
					if (data.group.dps < 3)
						++data.group.dps;
					break;
				}
			}
			UpdateData();
			break;
		case Group.Type.Raid:
		case Group.Type.Warfront:
		case Group.Type.RBG:
		case Group.Type.Battleground:
		case Group.Type.Other:
			switch (emoji_type) {
			case Emoji.Type.Tank:
				++data.group.tank;
				break;
			case Emoji.Type.Heal:
				++data.group.heal;
				break;
			case Emoji.Type.Dps:
				++data.group.dps;
				break;
			}
			UpdateData();
			break;
		case Group.Type.Arenas:
			if (is_owner) {
				switch (emoji_type) {
				case Emoji.Type.Tank:
					if (data.group.members() < 3)
						++data.group.tank;
					else
						data.group.tank = 0;
					break;
				case Emoji.Type.Heal:
					if (data.group.members() < 3)
						++data.group.heal;
					else
						data.group.heal = 0;
					break;
				case Emoji.Type.Dps:
					if (data.group.members() < 3)
						++data.group.dps;
					else
						data.group.dps = 0;
					break;
				}
				DeleteReaction();
			} else {
				switch (emoji_type) {
				case Emoji.Type.Tank:
					if (data.group.members() < 3)
						++data.group.tank;
					break;
				case Emoji.Type.Heal:
					if (data.group.members() < 3)
						++data.group.heal;
					break;
				case Emoji.Type.Dps:
					if (data.group.members() < 3)
						++data.group.dps;
					break;
				}
			}
			UpdateData();
			break;
		case Group.Type.Vision:
			if (is_owner) {
				switch (emoji_type) {
				case Emoji.Type.Tank:
					if (data.group.members() < 5)
						++data.group.tank;
					else
						data.group.tank = 0;
					break;
				case Emoji.Type.Heal:
					if (data.group.members() < 5)
						++data.group.heal;
					else
						data.group.heal = 0;
					break;
				case Emoji.Type.Dps:
					if (data.group.members() < 5)
						++data.group.dps;
					else
						data.group.dps = 0;
					break;
				}
				DeleteReaction();
			} else {
				switch (emoji_type) {
				case Emoji.Type.Tank:
					if (data.group.members() < 5)
						++data.group.tank;
					break;
				case Emoji.Type.Heal:
					if (data.group.members() < 5)
						++data.group.heal;
					break;
				case Emoji.Type.Dps:
					if (data.group.members() < 5)
						++data.group.dps;
					break;
				}
			}
			UpdateData();
			break;
		case Group.Type.Scenario:
		case Group.Type.Island:
			if (is_owner) {
				switch (emoji_type) {
				case Emoji.Type.Dps:
					++data.group.dps;
					data.group.dps %= 4;
					break;
				}
				DeleteReaction();
			} else {
				switch (emoji_type) {
				case Emoji.Type.Dps:
					if (data.group.dps < 3)
						++data.group.dps;
					break;
				}
			}
			UpdateData();
			break;
		}

		await bulletins[message_id].Update();
		if (!is_owner && bulletins[message_id].do_notify_owner) {
			Log.Debug("Notifying owner...", 1, message_id);
			DiscordChannel channel = await
				bulletins[message_id].
				data.owner.
				CreateDmChannelAsync();
			string text_name = e.User.ToDiscordMember(e.Guild)!.Nickname;
			if (text_name == "")
				text_name = e.User.Username;
			string notification =
				":information_source: " + text_name.Bold() +
				" signed up for your group: " +
				bulletins[message_id].data.title.Bold();
			await Client.SendMessageAsync(channel, notification);
		}
	}

	static async Task UpdateFromControls(MessageReactionRemoveEventArgs e) {
		ulong message_id = e.Message.Id;
		bool is_owner = (e.User == bulletins[message_id].data.owner);
		DiscordEmoji emoji = e.Emoji;
		Emoji.Type? emoji_type = Emoji.GetType(emoji);
		BulletinData data = bulletins[message_id].data;

		if (emoji_type == null) {
			log.Info("Reaction not recognized.", 1, message_id);
			log.Debug(emoji.ToString(), 2, message_id);
			return;
		}

		// Group.Type-specific controls
		switch (bulletins[message_id].data.group.type) {
		case Group.Type.Dungeon:
		case Group.Type.Arenas:
		case Group.Type.Vision:
			if (is_owner)
				break;
			switch (emoji_type) {
			case Emoji.Type.Tank:
				data.group.tank = Math.Max(--data.group.tank, 0);
				break;
			case Emoji.Type.Heal:
				data.group.heal = Math.Max(--data.group.heal, 0);
				break;
			case Emoji.Type.Dps:
				data.group.dps = Math.Max(--data.group.dps, 0);
				break;
			}
			bulletins[message_id].data = data;
			break;
		case Group.Type.Raid:
		case Group.Type.Warfront:
		case Group.Type.RBG:
		case Group.Type.Battleground:
		case Group.Type.Other:
			// Not `break`ing if the reaction was by the owner,
			// since owner reactions aren't auto-removed for these.
			switch (emoji_type) {
			case Emoji.Type.Tank:
				data.group.tank = Math.Max(--data.group.tank, 0);
				break;
			case Emoji.Type.Heal:
				data.group.heal = Math.Max(--data.group.heal, 0);
				break;
			case Emoji.Type.Dps:
				data.group.dps = Math.Max(--data.group.dps, 0);
				break;
			}
			bulletins[message_id].data = data;
			break;
		case Group.Type.Scenario:
		case Group.Type.Island:
			if (is_owner)
				break;
			switch (emoji_type) {
			case Emoji.Type.Dps:
				data.group.dps = Math.Max(--data.group.dps, 0);
				break;
			}
			bulletins[message_id].data = data;
			break;
		}

		await bulletins[message_id].Update();
		if (!is_owner && bulletins[message_id].do_notify_owner) {
			log.Debug("Notifying owner...", 1, message_id);
			DiscordChannel channel = await
				bulletins[message_id].
				data.owner.
				CreateDmChannelAsync();
			string text_name = e.User.ToDiscordMember(e.Guild)!.Nickname;
			if (text_name == "")
				text_name = e.User.Username;
			string notification =
				":information_source: " + text_name.Bold() +
				" removed themselves from your group " +
				bulletins[message_id].data.title.Bold();
			await puck.SendMessageAsync(channel, notification);
		}
	}

	// Private method used to define the public "IsDebug" property.
	[Conditional("DEBUG")]
	private static void CheckDebug(ref bool isDebug) { isDebug = true; }
}
