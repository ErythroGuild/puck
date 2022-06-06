using static Puck.Commands.CommandHandler.CommandTree;

namespace Puck.Commands;

class About : CommandHandler {
	public override CommandTree Tree { get; init; }

	private readonly Emojis _emojis;
	private readonly DiscordColor
		_colorRed   = new (218,  67,  49);

	private static readonly object _lock = new ();
	private const string
		_pathBuild = @"config/commit.txt",
		_pathVersion = @"config/tag.txt";
	private const string
		_commandAbout = "about";
	private const string
		_urlRepo    = @"https://github.com/ErythroGuild/puck",
		_urlPrivacy = @"https://github.com/ErythroGuild/puck/blob/master/Privacy.md",
		_urlCredits = @"https://github.com/ErythroGuild/puck/blob/master/Acknowledgements.md",
		_urlLicense = @"https://github.com/ErythroGuild/puck/blob/master/License.txt";


	public About(Emojis emojis) {
		Tree = new (
			new (new LeafArgs(
				_commandAbout,
				"Display build, stats, and metadata.",
				new List<CommandOption>(),
				Permissions.None
			), AboutAsync)
		);

		_emojis = emojis;
	}

	private async Task AboutAsync(DiscordInteraction interaction, Dictionary<string, object> _) {
		await interaction.DeferMessageAsync(true);

		// Read in data.
		StreamReader file;
		string version = "", build = "";
		lock (_lock) {
			file = File.OpenText(_pathVersion);
			version = file.ReadLine() ?? "";
			file.Close();
		}
		lock (_lock) {
			file = File.OpenText(_pathBuild);
			build = file.ReadLine() ?? "";
			if (build.Length > 7)
				build = build[..7];
			file.Close();
		}

		// Print status data.
		int bulletins = Bulletin.Count;
		int servers = Program.Client.Guilds.Count;
		string word_listing = (bulletins == 1)
			? "listing"
			: "listings";
		string word_server = (servers == 1)
			? "server"
			: "servers";
		string line_data =
			$"Tracking: **{bulletins}** {word_listing} + **{servers}** {word_server}";
		string line_urls =
			$"[Source]({_urlRepo}) {_emojis.Tank} [Privacy]({_urlPrivacy}) {_emojis.Heal} [Credits]({_urlCredits}) {_emojis.Dps} [License]({_urlLicense})";
		string description = line_data + "\n" + line_urls;

		DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
			.WithColor(_colorRed)
			.WithTitle($"**Puck {version}** - build `{build}`")
			.WithDescription(description);
		await interaction.EditOriginalResponseAsync(
			new DiscordWebhookBuilder().AddEmbed(embed)
		);
	}
}
