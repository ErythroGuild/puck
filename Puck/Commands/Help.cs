using static Puck.Commands.CommandHandler.CommandTree;

namespace Puck.Commands;

class Help : CommandHandler {
	public override CommandTree Tree { get; init; }

	private readonly Emojis _emojis;
	private const string
		_bbul = "\u2022",
		_wbul = "\u25E6",
		_ensp = "\u2002",
		_emsp = "\u2003",
		_zwsp = "\u200B";
	private const string
		_commandHelp = "help";

	public Help(Emojis emojis) {
		Tree =  new (
			new (new LeafArgs(
				_commandHelp,
				"Show how to use Puck's commands.",
				new List<CommandOption>(),
				Permissions.None
			), HelpAsync)
		);
		_emojis = emojis;
	}

	private Task HelpAsync(DiscordInteraction interaction, Dictionary<string, object> _) {
		string helptext =
			$"""
			**Group Signup**

			**List Group**
			**`/lfg`** creates a new thread for your group.
			`title` is the only mandatory field (all others can be omitted).
			`mention` only works with these roles:
			{_emsp}{_wbul}{_ensp}@role
			`group-type` is "5-man (1-1-3)" by default. "Raid group" has a cap of 40 members.
			`duration` is 5 minutes by default. The thread will be archived after this time.

			**Group Setup**

			**Configure**
			**`/config`** customizes the bot for your server.
			*Note: use Server Settings -> Integrations to set up allowed channels.*
			`/config help` displays a guide with further details.
			""";
		return interaction.CreateResponseAsync(
			InteractionResponseType.ChannelMessageWithSource,
			new DiscordInteractionResponseBuilder()
				.WithContent(helptext)
				.AddMentions(Mentions.None)
				.AsEphemeral(true)
		);
	}
}
