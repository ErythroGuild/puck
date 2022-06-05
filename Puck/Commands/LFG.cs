using static Puck.Commands.CommandHandler.CommandTree;

namespace Puck.Commands;

class LFG : CommandHandler {
	public static string? GroupTypeName(string choice) =>
		_groupNames.ContainsKey(choice)
			? _groupNames[choice]
			: null;
	public static IReadOnlyList<string> DefaultGroupTypes =>
		new List<string> {
			Choice2_any,
			Choice3_any,
			Choice5_113,
			Choice5_any,
			ChoiceRaid ,
		};
	public static IReadOnlyList<string> AllGroupTypes =>
		new List<string> {
			Choice2_any,
			Choice3_012,
			Choice3_111,
			Choice3_any,
			Choice4_112,
			Choice4_any,
			Choice5_113,
			Choice5_any,
			Choice6_any,
			Choice8_224,
			ChoiceRaid ,
		};
	public static List<CommandChoice> GroupTypeChoices(IReadOnlyList<string> keys) {
		List<CommandChoice> choices = new ();
		foreach (string key in keys) {
			if (_groupNames.ContainsKey(key))
				choices.Add(new (GroupTypeName(key), key));
		}
		return choices;
	}
	
	public static TimeSpan GetDuration(string option) =>
		option switch {
			Choice2m  => TimeSpan.FromMinutes(2),
			Choice5m  => TimeSpan.FromMinutes(5),
			Choice15m => TimeSpan.FromMinutes(15),
			Choice30m => TimeSpan.FromMinutes(30),
			Choice1h  => TimeSpan.FromHours(1),
			Choice2h  => TimeSpan.FromHours(2),
			Choice6h  => TimeSpan.FromHours(6),
			Choice1d  => TimeSpan.FromDays(1),
			Choice2d  => TimeSpan.FromDays(2),
			_ => throw new ArgumentException("Unrecognized duration option."),
		};

	public override CommandTree Tree { get; init; }
	
	private readonly Emojis _emojis;
	private readonly static ReadOnlyDictionary<string, string> _groupNames =
		new (new ConcurrentDictionary<string, string> {
			[Choice2_any] = "2-man (any)"  ,
			[Choice3_012] = "3-man (0-1-2)",
			[Choice3_111] = "3-man (1-1-1)",
			[Choice3_any] = "3-man (any)"  ,
			[Choice4_112] = "4-man (1-1-2)",
			[Choice4_any] = "4-man (any)"  ,
			[Choice5_113] = "5-man (1-1-3)",
			[Choice5_any] = "5-man (any)"  ,
			[Choice6_any] = "6-man (any)"  ,
			[Choice8_224] = "8-man (2-2-4)",
			[ChoiceRaid ] = "raid group"   ,
		});
	private const string
		_commandLfg = "lfg";
	private const string
		_optionTitle = "title",
		_optionDescription = "description",
		_optionMention = "mention",
		_optionDuration = "duration",
		_optionGroupType = "group-type";
	public const string
		Choice2m  = "2min" ,
		Choice5m  = "5min" ,
		Choice15m = "15min",
		Choice30m = "30min",
		Choice1h  = "1hrs" ,
		Choice2h  = "2hrs" ,
		Choice6h  = "6hrs" ,
		Choice1d  = "1day" ,
		Choice2d  = "2day" ;
	public const string
		Choice2_any = "2-any",
		Choice3_012 = "3-012",
		Choice3_111 = "3-111",
		Choice3_any = "3-any",
		Choice4_112 = "4-112",
		Choice4_any = "4-any",
		Choice5_113 = "5-113",
		Choice5_any = "5-any",
		Choice6_any = "6-any",
		Choice8_224 = "8-224",
		ChoiceRaid  = "raid" ;

	public LFG(IReadOnlyList<string> groupTypes, Emojis emojis) {
		Tree = new (
			new (new LeafArgs(
				_commandLfg,
				"Create a thread to advertise a group.",
				new List<CommandOption> {
					new (
						_optionTitle,
						"The title of the group.",
						ApplicationCommandOptionType.String,
						required: true
					),
					new (
						_optionDescription,
						"A short description of the group.",
						ApplicationCommandOptionType.String,
						required: false
					),
					new (
						_optionMention,
						"The role to ping.",
						ApplicationCommandOptionType.Role,
						required: false
					),
					new (
						_optionDuration,
						"The group will delist after this interval.",
						ApplicationCommandOptionType.String,
						required: false,
						choices: new List<CommandChoice> {
							new ("2 minutes" , Choice2m ),
							new ("5 minutes" , Choice5m ),
							new ("15 minutes", Choice15m),
							new ("30 minutes", Choice30m),
							new ("1 hour"    , Choice1h ),
							new ("2 hours"   , Choice2h ),
							new ("6 hours"   , Choice6h ),
							new ("1 day"     , Choice1d ),
							new ("2 days"    , Choice2d ),
						}
					),
					new (
						_optionGroupType,
						"The type of group to list for.",
						ApplicationCommandOptionType.String,
						required: false,
						choices: GroupTypeChoices(groupTypes)
					),
				},
				Permissions.UseApplicationCommands
			), LfgAsync )
		);

		_emojis = emojis;
	}

	private async Task LfgAsync(DiscordInteraction interaction, Dictionary<string, object> args) {
		await interaction.DeferMessageAsync(true);

		// Parse arguments.
		string title = GetArg<string>(args, _optionTitle);
		string? description = HasArg(args, _optionDescription)
			? GetArg<string>(args, _optionDescription)
			: null;
		DiscordRole? mention = HasArg(args, _optionMention)
			? interaction.GetTargetRole()
			: null;
		TimeSpan? duration = HasArg(args, _optionDuration)
			? GetDuration(GetArg<string>(args, _optionDuration))
			: null;
		DiscordUser owner = interaction.User;
		Group? group = HasArg(args, _optionGroupType)
			? GetGroup(GetArg<string>(args, _optionGroupType), owner)
			: null;

		DiscordGuild guild = interaction.Guild;

		// Set default duration if not specified.
		if (duration is null) {
			GuildConfig config =
				GuildConfigDatabase.GetConfigOrDefault(guild);
			duration = config.DefaultDuration();
		}

		// Set default group type if not specified.
		if (group is null) {
			GuildConfig config =
				GuildConfigDatabase.GetConfigOrDefault(guild);

			string? group_type = null;
			if (mention is not null) {
				IReadOnlyDictionary<ulong, string> group_types =
					config.RoleGroupTypes();
				if (group_types.ContainsKey(mention.Id))
					group_type = group_types[mention.Id];
			}
			group_type ??= config.DefaultGroupType;
			group = GetGroup(group_type, owner);
		}

		// Only set the mention if it's explicitly allowed.
		if (mention is not null) {
			GuildConfig config =
				GuildConfigDatabase.GetConfigOrDefault(guild);
			List<ulong> roles_allowed = new (config.RoleList());
			if (!roles_allowed.Contains(mention.Id))
				mention = null;
		}

		// Create and initialize bulletin.
		TaskCompletionSource<DiscordThreadChannel> thread_promise = new ();
		_ = new Bulletin(
			title,
			description,
			mention,
			duration.Value,
			group,
			thread_promise.Task,
			_emojis
		);
		DiscordThreadChannel thread = await
			interaction.Channel.CreateThreadAsync(
				title,
				AutoArchiveDuration.Day,
				ChannelType.PublicThread,
				"New LFG listing."
			);
		thread_promise.SetResult(thread);

		// Update original command response.
		string response =
			$"{_emojis.Delist} Created LFG listing:";
		response += $" {thread.Mention}";
		await interaction.UpdateMessageAsync(response);
	}

	private static Group GetGroup(string option, DiscordUser owner) =>
		option switch {
			Choice3_012 => Group.WithRoles(owner, 0, 1, 2),
			Choice3_111 => Group.WithRoles(owner, 1, 1, 1),
			Choice4_112 => Group.WithRoles(owner, 1, 1, 2),
			Choice5_113 => Group.WithRoles(owner, 1, 1, 3),
			Choice8_224 => Group.WithRoles(owner, 2, 2, 4),
			Choice2_any => Group.WithAnyRole(owner, 2),
			Choice3_any => Group.WithAnyRole(owner, 3),
			Choice4_any => Group.WithAnyRole(owner, 4),
			Choice5_any => Group.WithAnyRole(owner, 5),
			Choice6_any => Group.WithAnyRole(owner, 6),
			ChoiceRaid  => Group.WithAnyRole(owner),
			_ => throw new ArgumentException("Unrecognized group type option."),
		};
}
