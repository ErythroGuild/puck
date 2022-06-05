using System.Timers;

namespace Puck;

class Bulletin {
	// indexed by (first post = the embed) message id
	private static readonly ConcurrentDictionary<ulong, Bulletin> _bulletins = new ();

	static Bulletin() {
		Program.Client.ComponentInteractionCreated += async (client, e) => {
			ulong id = e.Message.Id;
			if (!_bulletins.ContainsKey(id))
				return;

			e.Handled = true;
			Bulletin bulletin = _bulletins[id];
			// Any registered messages can be responded to.
			await e.Interaction.AcknowledgeComponentAsync();

			// Queue up action.
			bulletin._actions.Add(new Task(async () => {
				// Cancelling is the same for owner and not.
				if (e.Id == _idCancel)
					bulletin.Group.Remove(e.User);

				// Handle signups.
				if (e.User != bulletin.Owner) {
					switch (e.Id) {
					case _idTank:
						bulletin.Group.AddTank(e.User);
						break;
					case _idHeal:
						bulletin.Group.AddHeal(e.User);
						break;
					case _idDps:
						bulletin.Group.AddDps(e.User);
						break;
					}
				}

				// Handle configuration.
				if (e.User == bulletin.Owner) {
					switch (e.Id) {
					case _idTank:
						bulletin.Group.CycleTank();
						break;
					case _idHeal:
						bulletin.Group.CycleHeal();
						break;
					case _idDps:
						bulletin.Group.CycleDps();
						break;
					case _idRefresh:
						bulletin.IncrementTime();
						break;
					case _idDelist:
						await bulletin.Delist();
						break;
					}
				}

				await bulletin.Update();
			}));
		};
	}

	public readonly string Title;
	public readonly string? Description;
	public readonly DiscordRole? Mention;
	public readonly Group Group;
	public DiscordUser Owner => Group.Owner;
	public DateTimeOffset Expiry { get; private set; }

	private readonly Timer _timer;
	private DiscordThreadChannel? _thread = null;
	private DiscordMessage? _message = null;
	private readonly TaskQueue _actions = new ();
	private readonly Emojis _e;

	private readonly TimeSpan
		_durationAdd = TimeSpan.FromMinutes(5);
	private readonly DiscordColor
		_colorRed   = new (218,  67,  49),
		_colorLight = new (255, 206, 201),
		_colorDark  = new ( 62,   6,   0);
	private const string
		_idTank = "button_tank",
		_idHeal = "button_heal",
		_idDps  = "button_dps",
		_idCancel  = "button_cancel",
		_idRefresh = "button_refresh",
		_idDelist  = "button_delist";

	public Bulletin(
		string title,
		string? description,
		DiscordRole? mention,
		TimeSpan duration,
		Group group,
		Task<DiscordThreadChannel> thread,
		Emojis e
	) {
		Title = title;
		Description = description;
		Mention = mention;
		Group = group;
		Expiry = DateTimeOffset.Now + duration;
		_e = e;

		_timer = CreateTimer(duration, false);
		_timer.Elapsed += async (timer, e) =>
			await Delist();

		thread.ContinueWith(async (thread) => {
			_thread = thread.Result;
			_message = await
				_thread.SendMessageAsync(GetMessage(true));
			_bulletins.TryAdd(_message.Id, this);
			_timer.Start();
		});
	}

	public void IncrementTime() {
		_timer.Stop();
		Expiry += _durationAdd;
		double msec =
			(Expiry - DateTimeOffset.Now).TotalMilliseconds;
		_timer.Interval = msec;
		_timer.Start();
	}
	public async Task Delist() {
		_timer.Stop();

		if (_message is not null)
			await _message.ModifyAsync(GetMessage(false));

		if (_thread is not null)
			await _thread.ModifyAsync((t) => t.IsArchived = true);
		
		if (_message is not null)
			_bulletins.TryRemove(_message.Id, out _);
	}

	private async Task Update() {
		if (_message is not null) {
			if (_bulletins.ContainsKey(_message.Id))
				await _message.ModifyAsync(GetMessage(true));
		}
	}
	private DiscordMessageBuilder GetMessage(bool isEnabled) {
		// Select embed color.
		DiscordColor color = Group.HasMaxCount
			? _colorRed
			: _colorLight;

		// Format title.
		string title = isEnabled
			? Title
			: Title.Strikethrough();

		// Construct message body.
		string byline = $"*Listed by: {Owner.Mention}*";
		if (Mention is not null)
			byline += $" - **{Mention.Mention}**";
		string content = "";
		if (Description is not null)
			content += Description + "\n\n";
		content += Group.PrintMemberList(_e);
		if (isEnabled)
			content += $"\n\n*expiring ~{Expiry.Timestamp(TimestampStyle.Relative)}*";

		// Parse for potential thumbnail.
		string? thumbnail = GetThumbnailUrl();

		// Handle allowed mentions.
		List<IMention> mentions = isEnabled
			? new () { new UserMention(Owner) }
			: new (Mentions.None);
		if (isEnabled && Mention is not null)
			mentions.Add(new RoleMention(Mention));

		// Construct embed.
		DiscordEmbedBuilder embed =
			new DiscordEmbedBuilder()
			.WithColor(color)
			.WithTitle(title)
			.WithDescription(content);
		if (thumbnail is not null)
			embed = embed.WithThumbnail(thumbnail);

		// Construct message.
		DiscordMessageBuilder message =
			new DiscordMessageBuilder()
			.WithContent(byline) // workaround for disallowed embed pings
			.WithEmbed(embed)
			.WithAllowedMentions(mentions);
		if (isEnabled) {
			message = message
				.AddComponents(ButtonsSignup(_e))
				.AddComponents(ButtonsActions(_e));
		}

		return message;
	}

	private string? GetThumbnailUrl() {
		return null;
		//return "https://i.imgur.com/x6TwpSQ.jpeg";
	}

	private static DiscordComponent[] ButtonsSignup(Emojis e) =>
		new DiscordComponent[] {
			new DiscordButtonComponent(
				ButtonStyle.Secondary,
				_idCancel,
				label: "",
				disabled: false,
				emoji: new (e.Cancel)
			),
			new DiscordButtonComponent(
				ButtonStyle.Primary,
				_idTank,
				label: "",
				emoji: new (e.Tank)
			),
			new DiscordButtonComponent(
				ButtonStyle.Primary,
				_idHeal,
				label: "",
				emoji: new (e.Heal)
			),
			new DiscordButtonComponent(
				ButtonStyle.Primary,
				_idDps,
				label: "",
				emoji: new (e.Dps)
			),
		};
	private static DiscordComponent[] ButtonsActions(Emojis e) =>
		new DiscordComponent[] {
			new DiscordButtonComponent(
				ButtonStyle.Secondary,
				_idRefresh,
				label: "Add 5 minutes",
				emoji: new (e.Refresh)
			),
			new DiscordButtonComponent(
				ButtonStyle.Success,
				_idDelist,
				label: "Delist group",
				emoji: new (e.Delist)
			),
		};
}
