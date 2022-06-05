using System.Timers;

using ComponentRow = DSharpPlus.Entities.DiscordActionRowComponent;
using Component = DSharpPlus.Entities.DiscordComponent;

namespace Puck.Components;

class Pages {
	public const int DefaultPageSize = 8;
	public static TimeSpan DefaultTimeout => TimeSpan.FromMinutes(10);

	// Table of all Pages to handle, indexed by the message ID of the
	// owning message.
	// This also serves as a way to "hold" fired timers, preventing them
	// from going out of scope and being destroyed.
	private static readonly ConcurrentDictionary<ulong, Pages> _pages = new ();
	private const string
		_idButtonPrev = "list_prev",
		_idButtonNext = "list_next",
		_idButtonPage = "list_page";
	private static readonly string[] _ids = new string[]
		{ _idButtonPrev, _idButtonNext, _idButtonPage };
	private const string
		_labelPrev = "\u25B2",
		_labelNext = "\u25BC";

	public static void Init() { }
	// All events are handled by a single delegate, registered on init.
	// This means there doesn't need to be a large amount of delegates
	// that each event has to filter through until it hits the correct
	// handler.
	static Pages() {
		Program.Client.ComponentInteractionCreated += (client, e) => {
			_ = Task.Run(async () => {
				ulong id = e.Message.Id;

				// Consume all interactions originating from a registered
				// message, and created by the corresponding component.
				if (_pages.ContainsKey(id)) {
					await e.Interaction.AcknowledgeComponentAsync();
					e.Handled = true;

					Pages pages = _pages[id];

					// Can only update if message was already created.
					if (pages._message is null)
						return;

					// Only respond to interactions created by the "owner"
					// of the component.
					if (e.User != pages._interaction.User)
						return;

					// Handle buttons.
					switch (e.Id) {
					case _idButtonPrev:
						pages._page--;
						break;
					case _idButtonNext:
						pages._page++;
						break;
					}
					pages._page = Math.Max(pages._page, 0);
					pages._page = Math.Min(pages._page, pages._pageCount);

					// Edit original message.
					// This must be done through the original interaction, as
					// responses to interactions don't actually "exist" as real
					// messages.
					await pages._interaction
						.EditOriginalResponseAsync(pages.AsWebhookBuilder);
				}
			});
			return Task.CompletedTask;
		};
		Log.Debug("  Created handler for component: Pages");
	}

	// Instance (configuration) properties.
	private readonly List<string> _data;
	private int _page;
	private readonly int _pageCount;
	private readonly int _pageSize;
	private DiscordMessage? _message;
	private readonly DiscordInteraction _interaction;
	private readonly Timer _timer;

	// Generated instance properties.
	private DiscordWebhookBuilder AsWebhookBuilder { get {
		DiscordWebhookBuilder webhook =
			new DiscordWebhookBuilder()
			.WithContent(CurrentContent);
		if (_pageCount > 1)
			webhook = webhook.AddComponents(CurrentButtons);
		return webhook;
	} }
	private string CurrentContent { get {
		int i_start = _page * _pageSize;
		int i_end = Math.Min(i_start + _pageSize, _data.Count);
		int i_range = i_end - i_start;

		return _data.GetRange(i_start, i_range).ToLines();
	} }
	private Component[] CurrentButtons =>
		GetButtons(_page, _pageCount);

	// Private constructor.
	// Use Pages.Create() to create a new instance.
	private Pages(
		List<string> data,
		int pageSize,
		DiscordInteraction interaction,
		Timer timer
	) {
		// Calculate page count.
		// It's convenient to save this result.
		double pageCount = data.Count / (double)pageSize;
		pageCount = Math.Round(Math.Ceiling(pageCount));

		_data = data;
		_page = 0;
		_pageCount = (int)pageCount;
		_pageSize = pageSize;
		_interaction = interaction;
		_timer = timer;
	}

	// Manually time-out the timer (and fire the elapsed handler).
	public async Task Discard() {
		const double delay = 0.1;
		_timer.Stop();
		_timer.Interval = delay; // arbitrarily small interval, must be >0
		_timer.Start();
		await Task.Delay(TimeSpan.FromMilliseconds(delay));
	}

	// Cleanup task to dispose of all resources.
	// Does not check for _message being completed yet.
	private async Task Cleanup() {
		if (_message is null)
			return;

		// Remove held references.
		_pages.TryRemove(_message.Id, out _);

		// Update message to disable component.
		// Interaction responses behave as webhooks and need to be
		// constructed as such.
		_message = await _interaction.GetOriginalResponseAsync();
		DiscordWebhookBuilder message_new =
			new DiscordWebhookBuilder()
			.WithContent(_message.Content);
		if (_pageCount > 1) {
			List<ComponentRow> rows = ComponentsButtonsDisabled(
				new List<ComponentRow>(_message.Components)
			);
			if (rows.Count > 0)
				message_new.AddComponents(rows);
		}

		// Edit original message.
		// This must be done through the original interaction, as
		// responses to interactions don't actually "exist" as real
		// messages.
		await _interaction.EditOriginalResponseAsync(message_new);
	}

	public static DiscordWebhookBuilder Create(
		DiscordInteraction interaction,
		Task<DiscordMessage> messageTask,
		IReadOnlyList<string> data,
		int? pageSize=null,
		TimeSpan? timeout=null
	) {
		pageSize ??= DefaultPageSize;
		timeout ??= DefaultTimeout;
		Timer timer = Util.CreateTimer(timeout.Value, false);

		// Construct partial Pages object.
		Pages pages = new (
			new List<string>(data),
			pageSize.Value,
			interaction,
			timer
		);
		messageTask.ContinueWith((messageTask) => {
			DiscordMessage message = messageTask.Result;
			pages._message = message;
			_pages.TryAdd(message.Id, pages);
			pages._timer.Start();
		});
		timer.Elapsed += async (obj, e) => {
			// Run (or schedule to run) cleanup task.
			if (!messageTask.IsCompleted)
				await messageTask.ContinueWith((e) => pages.Cleanup());
			else
				await pages.Cleanup();
		};

		return pages.AsWebhookBuilder;
	}

	// Returns a row of buttons for paginating the data.
	private static Component[] GetButtons(int page, int total) =>
		new Component[] {
			new DiscordButtonComponent(
				ButtonStyle.Secondary,
				_idButtonPrev,
				_labelPrev,
				disabled: (page + 1 == 1)
			),
			new DiscordButtonComponent(
				ButtonStyle.Secondary,
				_idButtonPage,
				$"{page + 1} / {total}"
			),
			new DiscordButtonComponent(
				ButtonStyle.Secondary,
				_idButtonNext,
				_labelNext,
				disabled: (page + 1 == total)
			),
		};
	
	// Return a new list of components, with any DiscordButtonComponents
	// (with a matching ID) disabled.
	private static List<ComponentRow> ComponentsButtonsDisabled(
		List<ComponentRow> rows
	) {
		List<ComponentRow> rows_new = new ();

		foreach (ComponentRow row in rows) {
			List<Component> components_new = new ();

			foreach (Component component in row.Components) {
				if (component is
					DiscordButtonComponent button &&
					Array.IndexOf(_ids, component.CustomId) != -1
				) {
					button.Disable();
					components_new.Add(button);
				} else {
					components_new.Add(component);
				}
			}

			rows_new.Add(new ComponentRow(components_new));
		}

		return rows_new;
	}
}
