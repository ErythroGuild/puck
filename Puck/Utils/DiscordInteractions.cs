namespace Puck.Utils;

static partial class Util {
	// Convenience method for fetching the first resolved member of
	// the specified kind.
	// Useful for e.g. user/message context menu commands.
	public static DiscordAttachment GetTargetAttachment(this DiscordInteraction interaction) =>
		new List<DiscordAttachment>(interaction.Data.Resolved.Attachments.Values)[0];
	public static DiscordChannel GetTargetChannel(this DiscordInteraction interaction) =>
		new List<DiscordChannel>(interaction.Data.Resolved.Channels.Values)[0];
	public static DiscordRole GetTargetRole(this DiscordInteraction interaction) =>
		new List<DiscordRole>(interaction.Data.Resolved.Roles.Values)[0];
	public static DiscordMember GetTargetMember(this DiscordInteraction interaction) =>
		new List<DiscordMember>(interaction.Data.Resolved.Members.Values)[0];
	public static DiscordMessage GetTargetMessage(this DiscordInteraction interaction) =>
		new List<DiscordMessage>(interaction.Data.Resolved.Messages.Values)[0];

	// Convenience method for fetching command options.
	public static List<DiscordInteractionDataOption> GetArgs(this DiscordInteraction interaction) =>
		(interaction.Data.Options is not null)
			? new (interaction.Data.Options)
			: new ();
	public static List<DiscordInteractionDataOption> GetArgs(this DiscordInteractionDataOption option) =>
		(option.Options is not null)
			? new (option.Options)
			: new ();

	public static bool HasArg(Dictionary<string, object> args, string key) =>
		args.ContainsKey(key);
	public static T GetArg<T>(Dictionary<string, object> args, string key) =>
		(T)args[key];

	// Convenience methods for responding to interactions.
	public static Task DeferComponentAsync(this DiscordInteraction interaction) =>
		interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
	public static Task SubmitAutoCompleteAsync(this DiscordInteraction interaction, IReadOnlyList<string> choices) {
		// Convert list of strings to list of discord choices.
		List<DiscordAutoCompleteChoice> choices_discord = new ();
		foreach (string choice in choices)
			choices_discord.Add(new (choice, choice));

		return interaction.CreateResponseAsync(
			InteractionResponseType.AutoCompleteResult,
			new DiscordInteractionResponseBuilder()
				.AddAutoCompleteChoices(choices_discord)
		);
	}
}
