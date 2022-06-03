using static Puck.Commands.CommandHandler.CommandTree;

namespace Puck.Commands;

class Help : CommandHandler {
	public override CommandTree Tree { get; init; }

	private const int _capRoles = 8;

	private readonly Emojis _emojis;
	private const string
		_commandHelp = "help";
	private const string
		_zwsp = "\u200B",
		_ensp = "\u2002",
		_emsp = "\u2003",
		_ellp = "\u22EF",
		_bbul = "\u2022",
		_wbul = "\u25E6",
		_arrr = "\u2192";

	public Help(Emojis emojis) {
		Tree =  new (
			new (new LeafArgs(
				_commandHelp,
				"Explain how to use Puck's commands.",
				new List<CommandOption>(),
				Permissions.None
			), HelpAsync)
		);

		_emojis = emojis;
	}

	private async Task HelpAsync(DiscordInteraction interaction, Dictionary<string, object> _) {
		await interaction.CreateResponseAsync(
			InteractionResponseType.DeferredChannelMessageWithSource,
			new DiscordInteractionResponseBuilder()
				.AsEphemeral(true)
		);

		DiscordGuild guild = interaction.Guild;

		string helptext =
			$"""
			**Sign Up**
			Press one of the signup buttons ({_emojis.Tank}/{_emojis.Heal}/{_emojis.Dps}) to sign up for that role.
			If you're already signed up for a different role, you'll be switched to the new role.
			Press the {_emojis.Cancel} button to cancel any spots you've signed up for.

			**List Group**
			**`/lfg`** creates a new thread for your group.
			`title` is the only mandatory field (all others can be omitted).
			{GetMentionHelp(guild)}
			`group-type` depends on `mention` by default. ("Raid group" caps at {Group.MaxMembers} members.)
			{GetGroupTypeHelp(guild)}
			`duration` is `{GetDefaultDuration(guild)}` by default. The thread will be archived after this time.

			**Set Up Group**
			Use the signup buttons ({_emojis.Tank}/{_emojis.Heal}/{_emojis.Dps}) to pre-fill spots in your group.
			Press the {_emojis.Cancel} button to clear *all* spots you've pre-filled.

			**Configure**
			**`/config`** customizes the bot for your server.
			*Note: use Server Settings {_arrr} Integrations to set up allowed channels.*
			`/config help` displays a guide with further details.
			""";
		await interaction.EditOriginalResponseAsync(
			new DiscordWebhookBuilder()
				.WithContent(helptext)
				.AddMentions(Mentions.None)
		);
	}

	private static string GetMentionHelp(DiscordGuild guild) {
		IReadOnlyDictionary<ulong, DiscordRole> roleTable =
			guild.Roles;

		GuildConfig config =
			GuildConfigDatabase.GetConfigOrDefault(guild);

		List<string> roles = new ()
			{"`mention` only works with these roles:"};
		foreach (ulong roleId in config.RoleList()) {
			if (roleTable.ContainsKey(roleId))
				roles.Add($"{_emsp}{_wbul}{_ensp}{roleTable[roleId].Mention}");
		}

		// Special case if no roles are enabled.
		if (roles.Count == 0) {
			roles = new () {
				"`mention` is *not enabled* for any roles.",
				$"{_emsp}{_ensp}To change this setting for this server, use `/config mention`.",
				$"{_emsp}{_ensp}(You will need \"Manage Server\" permissions.)",
			};
		}

		// Cap list to a reasonable number (+1 for the first line).
		if (roles.Count > 1 + _capRoles) {
			// Subtracting an additional two lines means when a list
			// hits the cap, 2 extra lines will be elided, ensuring
			// a minimum number of elided lines.
			// (This prevents "orphaning" a single line.)
			roles = roles.GetRange(0, 1 + _capRoles - 2);
			roles.Add($"{_emsp}{_ensp}{_ensp}{_ellp}{_emsp}*(see `/config mention list` for full list)*");
		}

		return roles.ToLines();
	}

	private static string GetGroupTypeHelp(DiscordGuild guild) {
		IReadOnlyDictionary<ulong, DiscordRole> roleTable =
			guild.Roles;

		GuildConfig config =
			GuildConfigDatabase.GetConfigOrDefault(guild);

		// Show default group type first.
		List<string> roles = new ();
		string? type_default = LFG.GroupTypeName(config.DefaultGroupType);
		if (type_default is not null)
			roles.Add($"{_emsp}{_wbul}{_ensp}`{type_default,-13}` if no `mention` specified");

		// Compile all mentions.
		IReadOnlyDictionary<ulong, string> typeTable =
			config.RoleGroupTypes();
		foreach (ulong roleId in typeTable.Keys) {
			if (roleTable.ContainsKey(roleId)) {
				string? type = LFG.GroupTypeName(typeTable[roleId]);
				DiscordRole role = roleTable[roleId];
				if (type is not null)
					roles.Add($"{_emsp}{_wbul}{_ensp}`{type,-13}` for {role.Mention}");
			}
		}

		// Cap list to a reasonable number (+1 for the first line).
		if (roles.Count > 1 + _capRoles) {
			// Subtracting an additional two lines means when a list
			// hits the cap, 2 extra lines will be elided, ensuring
			// a minimum number of elided lines.
			// (This prevents "orphaning" a single line.)
			roles = roles.GetRange(0, 1 + _capRoles - 2);
			roles.Add($"{_emsp}{_ensp}{_ensp}{_ellp}{_emsp}*(see `/config mention list` for full list)*");
		}

		return roles.ToLines();
	}

	private static string GetDefaultDuration(DiscordGuild guild) {
		GuildConfig config =
			GuildConfigDatabase.GetConfigOrDefault(guild);

		TimeSpan duration = config.DefaultDuration();

		return duration switch {
			TimeSpan t when t < TimeSpan.FromHours(1) =>
				duration.ToString("m 'min'"),
			TimeSpan t when t < TimeSpan.FromHours(4) =>
				duration.ToString("h 'hrs' m 'min'"),
			TimeSpan t when t < TimeSpan.FromDays(1) =>
				duration.ToString("h 'hrs'"),
			TimeSpan t when t < TimeSpan.FromDays(2) =>
				duration.ToString("d 'day' h 'hrs'"),
			TimeSpan t when t < TimeSpan.FromDays(4) =>
				duration.ToString("d 'days' h 'hrs'"),
			_ =>
				duration.ToString("d 'days'"),
		};
	}
}
