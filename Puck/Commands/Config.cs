using Puck.Components;

using static Puck.Commands.CommandHandler.CommandTree;

using Entry = Puck.Components.Selection.Option;

namespace Puck.Commands;

class Config : CommandHandler {
	public override CommandTree Tree { get; init; }

	// Selection menus, indexed by the ID of the member accessing them.
	private readonly ConcurrentDictionary<ulong, Selection> _selects = new ();

	private readonly Emojis _emojis;
	private const string
		_commandConfig           = "config",
		_commandHelp             = "help",
		_commandView             = "view",
		_commandDefaultGroupType = "default-group-type",
		_commandDefaultDuration  = "default-duration",
		_commandGroupTypes       = "group-types",
		_commandMention          = "mention",
		_commandList             = "list",
		_commandSet              = "set",
		_commandRemove           = "remove";
	private const string
		_optionGroupType = "group-type",
		_optionDuration  = "duration",
		_optionRole      = "role",
		_optionDefaultGroupType = "default-group-type";
	private const string
		_zwsp = "\u200B",
		_ensp = "\u2002",
		_emsp = "\u2003",
		_ellp = "\u22EF",
		_bbul = "\u2022",
		_wbul = "\u25E6",
		_arrr = "\u2192";

	public Config(Emojis emojis) {
		Tree = new (new (
			new GroupArgs(
				_commandConfig,
				"Configure Puck for your server.",
				Permissions.None
			),
			new () {
				new (new (
					_commandHelp,
					"View the setup guide.",
					ApplicationCommandOptionType.SubCommand
				), HelpAsync),
				new (new (
					_commandView,
					"View the current server settings.",
					ApplicationCommandOptionType.SubCommand
				), ViewAsync),
				new (new (
					_commandDefaultGroupType,
					"Set the default group type.",
					ApplicationCommandOptionType.SubCommand,
					options: new List<CommandOption> { new (
						_optionGroupType,
						"The group type to use.",
						ApplicationCommandOptionType.String,
						required: true,
						choices: LFG.GroupTypeChoices(LFG.AllGroupTypes)
					) }
				), DefaultGroupTypeAsync),
				new (new (
					_commandDefaultDuration,
					"Set the default duration.",
					ApplicationCommandOptionType.SubCommand,
					options: new List<CommandOption> { new (
						_optionDuration,
						"The duration to use.",
						ApplicationCommandOptionType.String,
						required: true,
						choices: new List<CommandChoice> {
							new ("2 minutes" , LFG.Choice2m ),
							new ("5 minutes" , LFG.Choice5m ),
							new ("15 minutes", LFG.Choice15m),
							new ("30 minutes", LFG.Choice30m),
							new ("1 hour"    , LFG.Choice1h ),
							new ("2 hours"   , LFG.Choice2h ),
							new ("6 hours"   , LFG.Choice6h ),
							new ("1 day"     , LFG.Choice1d ),
							new ("2 days"    , LFG.Choice2d ),
						}
					) }
				), DefaultDurationAsync),
				new (new (
					_commandGroupTypes,
					"Set the choosable group types.",
					ApplicationCommandOptionType.SubCommand
				), GroupTypesAsync),
			},
			new () { new (
				_commandMention,
				"Configure the choosable mentions.",
				new () {
					new ( new (
						_commandList,
						"List the current settings for mentions.",
						ApplicationCommandOptionType.SubCommand
					), MentionListAsync),
					new ( new (
						_commandSet,
						"Configure a mention to allow.",
						ApplicationCommandOptionType.SubCommand,
						options: new List<CommandOption> {
							new (
								_optionRole,
								"The role to configure.",
								ApplicationCommandOptionType.Role,
								required: true
							),
							new (
								_optionDefaultGroupType,
								"The default group type for the role.",
								ApplicationCommandOptionType.String,
								required: true,
								choices: LFG.GroupTypeChoices(LFG.AllGroupTypes)
							),
						}
					), MentionSetAsync),
					new ( new (
						_commandRemove,
						"Stop allowing a mention.",
						ApplicationCommandOptionType.SubCommand,
						options: new List<CommandOption> { new (
							_optionRole,
							"The role to stop allowing.",
							ApplicationCommandOptionType.Role,
							required: true
						) }
					), MentionRemoveAsync),
				}
			) }
		) );

		_emojis = emojis;
	}

	private async Task HelpAsync(DiscordInteraction interaction, Dictionary<string, object> _) {
		string helptext =
			$"""
			*Use Server Settings {_arrr} Integrations to set up allowed channels.*
			{_ensp}{_bbul}{_ensp}`/config help` displays this guide;
			{_ensp}{_bbul}{_ensp}`/config view` displays the current server settings;
			{_ensp}{_bbul}{_ensp}`/config mention list` displays all allowed mentions.

			*Configuring server settings requires the "Manage Server" permission.*

			The default settings are used in an **`/lfg`** command if those options aren't used.
			{_ensp}{_bbul}{_ensp}`/config default-group-type` sets the default group type to use.
			{_ensp}{_ensp}{_ensp}This __can__ be set to a (normally) disabled group type.
			{_ensp}{_bbul}{_ensp}`/config default-duration` sets the default duration to use.

			{_ensp}{_bbul}{_ensp}`/config group-types` sets the group types **`/lfg`** chooses from.
			{_ensp}{_ensp}{_ensp}The default type __can__ fill in a (normally) disabled group type.
			
			**`/lfg`** will __only__ ping roles that have been explicitly allowed.
			{_ensp}{_bbul}{_ensp}`/config mention list` displays all allowed mentions.
			{_ensp}{_bbul}{_ensp}`/config mention set` configures an allowed mention.
			{_ensp}{_bbul}{_ensp}`/config mention remove` removes an allowed mention.
			""";
		await interaction.CreateResponseAsync(
			InteractionResponseType.ChannelMessageWithSource,
			new DiscordInteractionResponseBuilder()
				.WithContent(helptext)
				.AsEphemeral(true)
		);
	}

	private async Task ViewAsync(DiscordInteraction interaction, Dictionary<string, object> _) {
		GuildConfig config =
			GuildConfigDatabase.GetConfigOrDefault(interaction.Guild);

		// Read in guild config.
		string group_type = LFG.GroupTypeName(config.DefaultGroupType)!;
		TimeSpan duration = config.DefaultDuration();
		string duration_string = duration switch {
			TimeSpan d when d < TimeSpan.FromHours(1) =>
				duration.ToString("m'm 'ss's'"),
			TimeSpan d when d < TimeSpan.FromDays(1) =>
				duration.ToString("h'h 'mm'm 'ss's'"),
			_ =>
				duration.ToString("d'd 'h'h'"),
		};
		List<string> group_types = new (config.GroupTypeList());

		// Format group type list.
		List<string> group_types_string = new ();
		if (group_types.Count == 0)
			group_types_string.Add($"*No enabled group types.*");
		else if (group_types.Count == 1)
			group_types_string.Add("*Enabled group type:*");
		else
			group_types_string.Add("*Enabled group types:*");
		foreach (string key in group_types) {
			group_types_string.Add(
				$"{_emsp}{_wbul}{_ensp}`{LFG.GroupTypeName(key)!}`"
			);
		}

		string response =
			$"""
			**LFG settings for this server:**
			*Default group type:* `{group_type}`
			*Default duration:* `{duration_string}`
			{group_types_string.ToLines()}
			see also: `/config mention list`
			""";
		await interaction.CreateResponseAsync(
			InteractionResponseType.ChannelMessageWithSource,
			new DiscordInteractionResponseBuilder()
				.WithContent(response)
				.AsEphemeral(true)
		);
	}

	private async Task DefaultGroupTypeAsync(DiscordInteraction interaction, Dictionary<string, object> args) {
		await interaction.DeferMessageAsync(true);

		// Check for permissions.
		if (!CanManageServer(interaction)) {
			await PermissionsError(interaction);
			return;
		}

		string response;
		DiscordGuild guild = interaction.Guild;
		string group_type = (string)args[_optionGroupType];

		// Initialize current settings from database.
		using GuildConfigDatabase database = new ();
		GuildConfig? config = database.GetConfig(guild.Id);
		if (config is null) {
			config = GuildConfigDatabase.DefaultConfig(guild);
			database.SaveChanges();
		}

		// Handle setting to the same value.
		string group_type_prev = config.DefaultGroupType;
		if (group_type == group_type_prev) {
			response =
				$"""
				Default group type already set to `{LFG.GroupTypeName(group_type)}`.
				{_emojis.Delist} No changes made.
				""";
			await interaction.UpdateMessageAsync(response);
			return;
		}

		config.DefaultGroupType = group_type;
		database.SaveChanges();

		response = $"{_emojis.Delist} Successfully set default group type to `{LFG.GroupTypeName(group_type)}`.";
		await interaction.UpdateMessageAsync(response);
	}

	private async Task DefaultDurationAsync(DiscordInteraction interaction, Dictionary<string, object> args) {
		await interaction.DeferMessageAsync(true);

		// Check for permissions.
		if (!CanManageServer(interaction)) {
			await PermissionsError(interaction);
			return;
		}
		
		string response;
		DiscordGuild guild = interaction.Guild;
		string duration_key = (string)args[_optionDuration];
		TimeSpan duration = LFG.GetDuration(duration_key);

		// Initialize current settings from database.
		using GuildConfigDatabase database = new ();
		GuildConfig? config = database.GetConfig(guild.Id);
		if (config is null) {
			config = GuildConfigDatabase.DefaultConfig(guild);
			database.SaveChanges();
		}

		// Handle setting to the same value.
		TimeSpan duration_prev = config.DefaultDuration();
		if (duration == duration_prev) {
			response =
				$"""
				Default duration already set to `{duration:d'd 'h'h 'mm'm 'ss's'}`.
				{_emojis.Delist} No changes made.
				""";
			await interaction.UpdateMessageAsync(response);
			return;
		}

		config.DefaultDurationMsec = duration.TotalMilliseconds;
		database.SaveChanges();

		response = $"{_emojis.Delist} Successfully set default duration to `{duration:d'd 'h'h 'mm'm 'ss's'}`.";
		await interaction.UpdateMessageAsync(response);
	}

	enum GroupTypes {
		Type2_Any,
		Type3_012, Type3_111, Type3_Any,
		Type4_112, Type4_Any,
		Type5_113, Type5_Any,
		Type6_Any,
		Type8_224,
		TypeRaid,
	};
	private async Task GroupTypesAsync(DiscordInteraction interaction, Dictionary<string, object> args) {
		await interaction.DeferMessageAsync(true);

		// Check for permissions.
		if (!CanManageServer(interaction)) {
			await PermissionsError(interaction);
			return;
		}

		DiscordGuild guild = interaction.Guild;

		// Initialize current settings from database.
		GuildConfig config =
			GuildConfigDatabase.GetConfigOrDefault(guild);

		// Fetch current enabled group types.
		List<GroupTypes> group_types = new ();
		foreach (string group_type in config.GroupTypeList()) {
			GroupTypes group_type_i = group_type switch {
				LFG.Choice2_any => GroupTypes.Type2_Any,
				LFG.Choice3_012 => GroupTypes.Type3_012,
				LFG.Choice3_111 => GroupTypes.Type3_111,
				LFG.Choice3_any => GroupTypes.Type3_Any,
				LFG.Choice4_112 => GroupTypes.Type4_112,
				LFG.Choice4_any => GroupTypes.Type4_Any,
				LFG.Choice5_113 => GroupTypes.Type5_113,
				LFG.Choice5_any => GroupTypes.Type5_Any,
				LFG.Choice6_any => GroupTypes.Type6_Any,
				LFG.Choice8_224 => GroupTypes.Type8_224,
				LFG.ChoiceRaid  => GroupTypes.TypeRaid ,
				_ => throw new ArgumentException("Unrecognized group type."),
			};
			group_types.Add(group_type_i);
		}
		group_types.Sort();

		// Create a registered Selection object.
		TaskCompletionSource<DiscordMessage> message_promise = new ();
		Selection select = Selection.Create(
			interaction,
			UpdateGroupTypesAsync,
			message_promise.Task,
			new List<KeyValuePair<GroupTypes, Entry>> {
				new (GroupTypes.Type2_Any, new Entry {
					Label = "2-man (any)",
					Id = "option_2_any",
				}),
				new (GroupTypes.Type3_012, new Entry {
					Label = "3-man (0-1-2)",
					Id = "option_3_012",
				}),
				new (GroupTypes.Type3_111, new Entry {
					Label = "3-man (1-1-1)",
					Id = "option_3_111",
				}),
				new (GroupTypes.Type3_Any, new Entry {
					Label = "3-man (any)",
					Id = "option_3_any",
				}),
				new (GroupTypes.Type4_112, new Entry {
					Label = "4-man (1-1-2)",
					Id = "option_4_112",
				}),
				new (GroupTypes.Type4_Any, new Entry {
					Label = "4-man (any)",
					Id = "option_4_any",
				}),
				new (GroupTypes.Type5_113, new Entry {
					Label = "5-man (1-1-3)",
					Id = "option_5_113",
				}),
				new (GroupTypes.Type5_Any, new Entry {
					Label = "5-man (any)",
					Id = "option_5_any",
				}),
				new (GroupTypes.Type6_Any, new Entry {
					Label = "6-man (any)",
					Id = "option_6_any",
				}),
				new (GroupTypes.Type8_224, new Entry {
					Label = "8-man (2-2-4)",
					Id = "option_8_224",
				}),
				new (GroupTypes.TypeRaid , new Entry {
					Label = "Raid group",
					Id = "option_raid",
				}),
			},
			new HashSet<GroupTypes>(group_types),
			"No allowed group types",
			isMultiple: true
		);

		// Disable any selections already in-flight.
		ulong user_id = interaction.User.Id;
		if (_selects.ContainsKey(user_id)) {
			await _selects[user_id].Discard();
			_selects.TryRemove(user_id, out _);
		}
		_selects.TryAdd(user_id, select);

		// Send response with selection menu.
		Dictionary<GroupTypes, Entry> _options = new () {
				[GroupTypes.Type2_Any] = new Entry {
					Label = "2-man (any)",
					Id = "option_2_any",
				},
				[GroupTypes.Type3_012] = new Entry {
					Label = "3-man (0-1-2)",
					Id = "option_3_012",
				},
				[GroupTypes.Type3_111] = new Entry {
					Label = "3-man (1-1-1)",
					Id = "option_3_111",
				},
				[GroupTypes.Type3_Any] = new Entry {
					Label = "3-man (any)",
					Id = "option_3_any",
				},
				[GroupTypes.Type4_112] = new Entry {
					Label = "4-man (1-1-2)",
					Id = "option_4_112",
				},
				[GroupTypes.Type4_Any] = new Entry {
					Label = "4-man (any)",
					Id = "option_4_any",
				},
				[GroupTypes.Type5_113] = new Entry {
					Label = "5-man (1-1-3)",
					Id = "option_5_113",
				},
				[GroupTypes.Type5_Any] = new Entry {
					Label = "5-man (any)",
					Id = "option_5_any",
				},
				[GroupTypes.Type6_Any] = new Entry {
					Label = "6-man (any)",
					Id = "option_6_any",
				},
				[GroupTypes.Type8_224] = new Entry {
					Label = "8-man (2-2-4)",
					Id = "option_8_224",
				},
				[GroupTypes.TypeRaid ] = new Entry {
					Label = "Raid group",
					Id = "option_raid",
				},
		};
		string roles_str =
			Selection.PrintSelected(group_types, _options, "role", "roles");
		DiscordWebhookBuilder response =
			new DiscordWebhookBuilder()
			.WithContent(roles_str)
			.AddComponents(select.Component);
		DiscordMessage message = await
			interaction.EditOriginalResponseAsync(response);
		message_promise.SetResult(message);

	}
	private async Task UpdateGroupTypesAsync(ComponentInteractionCreateEventArgs e) {
		await e.Interaction.AcknowledgeComponentAsync();

		List<string> selected = new (e.Values);
		List<string> group_types = new ();
		foreach (string id in selected) {
			group_types.Add(id switch {
				"option_2_any" => LFG.Choice2_any,
				"option_3_012" => LFG.Choice3_012,
				"option_3_111" => LFG.Choice3_111,
				"option_3_any" => LFG.Choice3_any,
				"option_4_112" => LFG.Choice4_112,
				"option_4_any" => LFG.Choice4_any,
				"option_5_113" => LFG.Choice5_113,
				"option_5_any" => LFG.Choice5_any,
				"option_6_any" => LFG.Choice6_any,
				"option_8_224" => LFG.Choice8_224,
				"option_raid"  => LFG.ChoiceRaid ,
				_ => throw new ArgumentException("Unknown group type ID."),
			});
		}

		DiscordGuild guild = e.Guild;

		// Initialize current settings from database.
		using GuildConfigDatabase database = new ();
		GuildConfig? config = database.GetConfig(guild.Id);
		if (config is null) {
			config = GuildConfigDatabase.DefaultConfig(guild);
			database.SaveChanges();
		}

		// Update database.
		List<string> group_types_prev = new (config.GroupTypeList());
		foreach (GroupType group_type in config.AllowedGroupTypes)
			config.AllowedGroupTypes.Remove(group_type);
		foreach (string group_type in LFG.AllGroupTypes) {
			if (group_types.Contains(group_type))
				config.AllowedGroupTypes.Add(new (guild.Id.ToString(), group_type));
		}
		database.SaveChanges();

		// Remember to update guild slash command.
		List<string> group_types_command = new ();
		foreach (string group_type in LFG.AllGroupTypes) {
			if (group_types.Contains(group_type))
				group_types_command.Add(group_type);
		}
		Command command_new = new LFG(group_types_command, _emojis).Command;
		List<CommandOption> options_new = new (command_new.Options);
		List<Command> commands_old = new (await guild.GetApplicationCommandsAsync());
		foreach (Command command_i in commands_old) {
			if (command_i.Name == command_new.Name)
				await guild.EditApplicationCommandAsync(command_i.Id, (c) => c.Options = options_new);
		}
		await e.Interaction.CreateFollowupMessageAsync(
			new DiscordFollowupMessageBuilder()
				.WithContent($"{_emojis.Delist} Update submitted!\nYou may need to click to a different server and back for changes to show up.")
				.AsEphemeral(true)
		);

		// Update select component.
		HashSet<Entry> options_updated = new ();
		List<GroupTypes> options_enum = new ();
		foreach (string group_type in config.GroupTypeList()) {
			GroupTypes group_type_i = group_type switch {
				LFG.Choice2_any => GroupTypes.Type2_Any,
				LFG.Choice3_012 => GroupTypes.Type3_012,
				LFG.Choice3_111 => GroupTypes.Type3_111,
				LFG.Choice3_any => GroupTypes.Type3_Any,
				LFG.Choice4_112 => GroupTypes.Type4_112,
				LFG.Choice4_any => GroupTypes.Type4_Any,
				LFG.Choice5_113 => GroupTypes.Type5_113,
				LFG.Choice5_any => GroupTypes.Type5_Any,
				LFG.Choice6_any => GroupTypes.Type6_Any,
				LFG.Choice8_224 => GroupTypes.Type8_224,
				LFG.ChoiceRaid  => GroupTypes.TypeRaid ,
				_ => throw new ArgumentException("Unrecognized group type."),
			};
			options_enum.Add(group_type_i);
		}
		options_enum.Sort();
		Dictionary<GroupTypes, Entry> _options = new () {
			[GroupTypes.Type2_Any] = new Entry {
				Label = "2-man (any)",
				Id = "option_2_any",
			},
			[GroupTypes.Type3_012] = new Entry {
				Label = "3-man (0-1-2)",
				Id = "option_3_012",
			},
			[GroupTypes.Type3_111] = new Entry {
				Label = "3-man (1-1-1)",
				Id = "option_3_111",
			},
			[GroupTypes.Type3_Any] = new Entry {
				Label = "3-man (any)",
				Id = "option_3_any",
			},
			[GroupTypes.Type4_112] = new Entry {
				Label = "4-man (1-1-2)",
				Id = "option_4_112",
			},
			[GroupTypes.Type4_Any] = new Entry {
				Label = "4-man (any)",
				Id = "option_4_any",
			},
			[GroupTypes.Type5_113] = new Entry {
				Label = "5-man (1-1-3)",
				Id = "option_5_113",
			},
			[GroupTypes.Type5_Any] = new Entry {
				Label = "5-man (any)",
				Id = "option_5_any",
			},
			[GroupTypes.Type6_Any] = new Entry {
				Label = "6-man (any)",
				Id = "option_6_any",
			},
			[GroupTypes.Type8_224] = new Entry {
				Label = "8-man (2-2-4)",
				Id = "option_8_224",
			},
			[GroupTypes.TypeRaid ] = new Entry {
				Label = "Raid group",
				Id = "option_raid",
			},
		};
		foreach (GroupTypes group_type in options_enum) {
			options_updated.Add(_options[group_type]);
		}
		await _selects[e.User.Id].Update(options_updated);
	}

	private async Task MentionListAsync(DiscordInteraction interaction, Dictionary<string, object> _) {
		await interaction.DeferMessageAsync(true);

		// Fetch current settings.
		DiscordGuild guild = interaction.Guild;
		GuildConfig config =
			GuildConfigDatabase.GetConfigOrDefault(guild);

		List<string> lines = new ()
			{ "**Allowed mentions:**" };

		// Collate all valid data.
		IReadOnlyDictionary<ulong, string> role_table =
			config.RoleGroupTypes();
		foreach (ulong role_id in role_table.Keys) {
			DiscordRole role = guild.GetRole(role_id);
			string? group_type =
				LFG.GroupTypeName(role_table[role_id]);

			if (group_type is null)
				throw new ArgumentException("Unknown group type.");

			lines.Add($"{_emsp}{_wbul}{_ensp}`{group_type,-13}` -  {role.Mention}");
		}

		// Special case for no results.
		if (lines.Count == 1) {
			lines[0] =
				$"""
				**No allowed mentions.**
				{_emojis.Refresh} You can use `/config mention set` to add some.
				""";
		}

		// Send response.
		TaskCompletionSource<DiscordMessage> message_promise = new ();
		DiscordWebhookBuilder response = Pages.Create(
			interaction,
			message_promise.Task,
			lines,
			pageSize: 12
		);
		response = response.AddMentions(Mentions.None);
		DiscordMessage message = await interaction
			.EditOriginalResponseAsync(response);
		message_promise.SetResult(message);
	}

	private async Task MentionSetAsync(DiscordInteraction interaction, Dictionary<string, object> args) {
		await interaction.DeferMessageAsync(true);

		// Check for permissions.
		if (!CanManageServer(interaction)) {
			await PermissionsError(interaction);
			return;
		}

		string response;
		DiscordGuild guild = interaction.Guild;
		DiscordRole role = interaction.GetTargetRole();
		string group_type = (string)args[_optionDefaultGroupType];

		// Initialize current settings from database.
		using GuildConfigDatabase database = new ();
		GuildConfig? config = database.GetConfig(guild.Id);
		if (config is null) {
			config = GuildConfigDatabase.DefaultConfig(guild);
			database.SaveChanges();
		}

		// Handle setting to the same value.
		IReadOnlyDictionary<ulong, string> roles_prev =
			config.RoleGroupTypes();
		if (roles_prev.ContainsKey(role.Id) && group_type == roles_prev[role.Id]) {
			response =
				$"""
				Role {role.Mention} already defaults to `{LFG.GroupTypeName(group_type)}`.
				{_emojis.Delist} No changes made.
				""";
			await interaction.EditOriginalResponseAsync(
				new DiscordWebhookBuilder()
					.WithContent(response)
					.AddMentions(Mentions.None)
			);
			return;
		}

		if (roles_prev.ContainsKey(role.Id)) {
			foreach (GuildRole role_i in config.AllowedRoles) {
				if (role_i.RoleId == role.Id.ToString()) {
					role_i.GroupTypeName = group_type;
					break;
				}
			}
		} else {
			config.AllowedRoles.Add(new (
				guild.Id.ToString(),
				role.Id.ToString(),
				group_type
			) );
		}
		database.SaveChanges();

		response = $"{_emojis.Delist} Successfully set {role.Mention} default group type to `{LFG.GroupTypeName(group_type)}`.";
		await interaction.UpdateMessageAsync(response);
	}

	private async Task MentionRemoveAsync(DiscordInteraction interaction, Dictionary<string, object> args) {
		await interaction.DeferMessageAsync(true);

		// Check for permissions.
		if (!CanManageServer(interaction)) {
			await PermissionsError(interaction);
			return;
		}

		string response;
		DiscordGuild guild = interaction.Guild;
		DiscordRole role = interaction.GetTargetRole();

		// Initialize current settings from database.
		using GuildConfigDatabase database = new ();
		GuildConfig? config = database.GetConfig(guild.Id);
		if (config is null) {
			config = GuildConfigDatabase.DefaultConfig(guild);
			database.SaveChanges();
		}

		// Handle role already not allowed.
		List<ulong> roles_prev = new (config.RoleList());
		if (!roles_prev.Contains(role.Id)) {
			response =
				$"""
				Role {role.Mention} does not currently ping.
				{_emojis.Delist} No changes made.
				""";
			await interaction.EditOriginalResponseAsync(
				new DiscordWebhookBuilder()
					.WithContent(response)
					.AddMentions(Mentions.None)
			);
			return;
		}

		foreach (GuildRole role_i in config.AllowedRoles) {
			if (role_i.RoleId == role.Id.ToString()) {
				config.AllowedRoles.Remove(role_i);
				break;
			}
		}
		database.SaveChanges();

		response = $"{_emojis.Delist} Successfully removed {role.Mention} from allowed pings.";
		await interaction.UpdateMessageAsync(response);
	}

	private static bool CanManageServer(DiscordInteraction interaction) {
		DiscordMember? caller = interaction.User as DiscordMember;
		return caller is not null &&
			caller.Permissions.HasPermission(Permissions.ManageGuild);
	}
	private static Task PermissionsError(DiscordInteraction interaction) =>
		interaction.UpdateMessageAsync($"You need \"Manage Server\" permissions to use this command.");
}
