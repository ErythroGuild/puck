using static Puck.Commands.CommandHandler.CommandTree;

namespace Puck.Commands;

class LFG : CommandHandler {
	public static IList<string> DefaultGroupTypes =>
		new List<string> {
			Choice2_any,
			Choice3_any,
			Choice5_113,
			Choice5_any,
			ChoiceRaid ,
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
	private const string
		_choice2m  = "2min" ,
		_choice5m  = "5min" ,
		_choice15m = "15min",
		_choice30m = "30min",
		_choice1h  = "1hrs" ,
		_choice2h  = "2hrs" ,
		_choice6h  = "6hrs" ,
		_choice1d  = "1day" ,
		_choice2d  = "2day" ;
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

	public LFG(IList<string> groupTypes, Emojis emojis) {
		List<CommandChoice> groupChoices = new ();
		foreach (string key in groupTypes)
			groupChoices.Add(new (_groupNames[key], key));

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
							new ("2 minutes" , _choice2m ),
							new ("5 minutes" , _choice5m ),
							new ("15 minutes", _choice15m),
							new ("30 minutes", _choice30m),
							new ("1 hour"    , _choice1h ),
							new ("2 hours"   , _choice2h ),
							new ("6 hours"   , _choice6h ),
							new ("1 day"     , _choice1d ),
							new ("2 days"    , _choice2d ),
						}
					),
					new (
						_optionGroupType,
						"The type of group to list for.",
						ApplicationCommandOptionType.String,
						required: false,
						choices: groupChoices
					),
				},
				Permissions.UseApplicationCommands
			), LfgAsync )
		);

		_emojis = emojis;
	}

	private async Task LfgAsync(DiscordInteraction interaction, Dictionary<string, object> args) {
		await interaction.CreateResponseAsync(
			InteractionResponseType.DeferredChannelMessageWithSource,
			new DiscordInteractionResponseBuilder()
				.AsEphemeral(true)
		);

		// Parse arguments.
		string title = GetArg<string>(args, _optionTitle);
		string? description = HasArg(args, _optionDescription)
			? GetArg<string>(args, _optionDescription)
			: null;
		DiscordRole? mention = HasArg(args, _optionMention)
			? interaction.GetTargetRole()
			: null;
		TimeSpan duration = HasArg(args, _optionDuration)
			? GetDuration(GetArg<string>(args, _optionDuration))
			: TimeSpan.FromMinutes(5);
		DiscordUser owner = interaction.User;
		Group group = HasArg(args, _optionGroupType)
			? GetGroup(GetArg<string>(args, _optionGroupType), owner)
			: Group.WithRoles(owner, 1, 1, 3);

		TaskCompletionSource<DiscordThreadChannel> thread_promise = new ();
		_ = new Bulletin(
			title,
			description,
			mention,
			duration,
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

		string response =
			$"{_emojis.Delist} Created LFG listing:";
		response += $" {thread.Mention}";
		await interaction.EditOriginalResponseAsync(
			new DiscordWebhookBuilder().WithContent(response)
		);
	}

	private static TimeSpan GetDuration(string option) =>
		option switch {
			_choice2m  => TimeSpan.FromMinutes(2),
			_choice5m  => TimeSpan.FromMinutes(5),
			_choice15m => TimeSpan.FromMinutes(15),
			_choice30m => TimeSpan.FromMinutes(30),
			_choice1h  => TimeSpan.FromHours(1),
			_choice2h  => TimeSpan.FromHours(2),
			_choice6h  => TimeSpan.FromHours(6),
			_choice1d  => TimeSpan.FromDays(1),
			_choice2d  => TimeSpan.FromDays(2),
			_ => throw new ArgumentException("Unrecognized duration option."),
		};
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
