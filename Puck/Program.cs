using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

using Spectre.Console;

using Puck.Commands;

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
	public static Emojis? Emojis = null;
	public static readonly ConcurrentDictionary<string, CommandHandler> Commands = new ();

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
				    [#86CFDA on black]______           _    [/]
				    [#86CFDA on black]| ___ \         | |   [/]
				    [#81B4FE on black]| |_/ /   _  ___| | __[/]
				    [#81B4FE on black]|  __/ | | |/ __| |/ /[/]
				    [#81B4FE on black]| |  | |_| | (__|   < [/]
				    [#AF83F8 on black]\_|   \__,_|\___|_|\_\[/]
				""";
		AnsiConsole.MarkupLine(logo);
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

				// Ensure all guilds at least have default database
				// entries.
				foreach (DiscordGuild guild in Client.Guilds.Values) {
					using GuildConfigDatabase database = new ();
					GuildConfig? config =
						database.GetConfig(guild.Id);
					if (config is null) {
						config = GuildConfigDatabase.DefaultConfig(guild);
						database.Configs.Add(config);
						database.SaveChanges();
					}
				}

				// Initialize emojis.
				Emojis = new Emojis(Client);
				Log.Debug("  Initialized custom emojis.");

				// Initialize custom components.
				Components.Pages.Init();
				Components.Selection.Init();

				// Attach command handler before registering commands.
				Client.InteractionCreated += (client, e) => {
					_ = Task.Run(async () => {
						string name = e.Interaction.Data.Name;
						if (Commands.ContainsKey(name)) {
							e.Handled = true;
							await Commands[name].HandleAsync(e.Interaction);
						}
					});
					return Task.CompletedTask;
				};

				// Initialize and collate all commands.
				// LFG is custom for each guild, so we need to
				// create a generic handler separately.
				List<CommandHandler> handlers = new () {
					new Help(Emojis),
					new Config(Emojis),
				};
				List<Command> commands = new ();
				foreach (CommandHandler handler in handlers) {
					Commands.TryAdd(handler.Command.Name, handler);
					commands.Add(handler.Command);
				}
				LFG lfg_generic = new (new List<string>(), Emojis);
				Commands.TryAdd(lfg_generic.Command.Name, lfg_generic);

				// Register commands in each connected guild.
				// A customized version of the LFG command is used.
				List<Task> tasks = new ();
				_stopwatchRegister.Start();
				foreach (DiscordGuild guild in Client.Guilds.Values) {
					using GuildConfigDatabase database = new ();
					GuildConfig? config = database.GetConfig(guild.Id);
					IReadOnlyList<string> keys = config is null
						? LFG.DefaultGroupTypes
						: config.GroupTypeList();
					List<Command> commands_guild = new (commands) {
						new LFG(keys, Emojis).Command
					};
					tasks.Add(guild.BulkOverwriteApplicationCommandsAsync(commands_guild));
				}
				await Task.WhenAll(tasks);
				Log.Information("  Registered commands in {Count} guild(s).", tasks.Count);
				_stopwatchRegister.LogMsecDebug("    Took {RegisterTime} msec.");

				Client.GuildCreated += (client, e) => {
					_ = Task.Run(() => {
						Log.Information("Added to guild: {GuildName}", e.Guild.Name);

						using GuildConfigDatabase database = new ();
						GuildConfig config =
							GuildConfigDatabase.DefaultConfig(e.Guild);
						database.Add(config);
						database.SaveChanges();
					});
					return Task.CompletedTask;
				};

				Client.GuildDeleted += (client, e) => {
					_ = Task.Run(() => {
						Log.Information("Removed from guild: {GuildName}", e.Guild.Name);

						using GuildConfigDatabase database = new ();
						GuildConfig? config =
							database.GetConfig(e.Guild.Id);
						if (config is not null)
							database.Remove(config);
						database.SaveChanges();
					});
					return Task.CompletedTask;
				};
			});
			return Task.CompletedTask;
		};

		Client.GuildUpdated += (client, e) => {
			_ = Task.Run(() => {
				if (e.GuildBefore.Name  == e.GuildAfter.Name)
					return;

				Log.Debug(
					"Guild name updated: {NameBefore} -> {NameAfter}",
					e.GuildBefore.Name,
					e.GuildAfter.Name
				);

				using GuildConfigDatabase database = new ();
				GuildConfig? config = database.GetConfig(e.GuildBefore.Id);
				if (config is null) {
					config = GuildConfigDatabase.DefaultConfig(e.GuildAfter);
					database.SaveChanges();
				}
				config.GuildName = e.GuildAfter.Name;
				database.SaveChanges();
			});
			return Task.CompletedTask;
		};

		// Stop configuration timer.
		Log.Debug("  Configured Discord client.");
		_stopwatchConfig.LogMsecDebug("    Took {ConfigTime} msec.");

		// Start connection timer and connect.
		_stopwatchConnect.Start();
		_stopwatchDownload.Start();
		await Client.ConnectAsync();
		await Task.Delay(-1);
	}

	// Private method used to define the public "IsDebug" property.
	[Conditional("DEBUG")]
	private static void CheckDebug(ref bool isDebug) { isDebug = true; }
}
