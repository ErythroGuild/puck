using System.Timers;

using SelectionCallback = System.Func<DSharpPlus.EventArgs.ComponentInteractionCreateEventArgs, System.Threading.Tasks.Task>;
using ComponentRow = DSharpPlus.Entities.DiscordActionRowComponent;
using Component = DSharpPlus.Entities.DiscordComponent;
using SelectOption = DSharpPlus.Entities.DiscordSelectComponentOption;

namespace Puck.Components;

class Selection {
	public readonly record struct Option (
		string Label,
		string Id,
		DiscordComponentEmoji? Emoji,
		string? Description
	);

	public static TimeSpan DefaultTimeout => TimeSpan.FromMinutes(10);

	// Table of all Selections to handle, indexed by the message ID of
	// the owning message.
	// This also serves as a way to "hold" fired timers, preventing them
	// from going out of scope and being destroyed.
	private static readonly ConcurrentDictionary<ulong, Selection> _selections = new ();
	private const string _id = "selection";
	
	public static void Init() { }
	// All events are handled by a single delegate, registered on init.
	// This means there doesn't need to be a large amount of delegates
	// that each event has to filter through until it hits the correct
	// handler.
	static Selection() {
		Program.Client.ComponentInteractionCreated += (client, e) => {
			_ = Task.Run(async () => {
				ulong id = e.Message.Id;

				// Consume all interactions originating from a registered
				// message, and created by the corresponding component.
				if (_selections.ContainsKey(id) && e.Id == _id) {
					e.Handled = true;
					Selection selection = _selections[id];

					// Only respond to interactions created by the "owner"
					// of the component.
					if (e.User != selection._interaction.User) {
						await e.Interaction.AcknowledgeComponentAsync();
						return;
					}

					await selection._callback(e);
				}
			});
			return Task.CompletedTask;
		};
		Log.Debug("  Created handler for component: Selection");
	}

	public DiscordSelectComponent Component { get; private set; }

	// Instanced (configuration) properties.
	private DiscordMessage? _message;
	private readonly DiscordInteraction _interaction;
	private readonly Timer _timer;
	private readonly SelectionCallback _callback;

	// Private constructor.
	// Use Selection.Create() to create a new instance.
	private Selection(
		DiscordSelectComponent component,
		DiscordInteraction interaction,
		Timer timer,
		SelectionCallback callback
	) {
		Component = component;
		_interaction = interaction;
		_timer = timer;
		_callback = callback;
	}

	// Manually time-out the timer (and fire the elapsed handler).
	public async Task Discard() {
		const double delay = 0.1;
		_timer.Stop();
		_timer.Interval = delay; // arbitrarily small interval, must be >0
		_timer.Start();
		await Task.Delay(TimeSpan.FromMilliseconds(delay));
	}

	// Update the selected entries of the select component.
	public async Task Update(IReadOnlySet<Option> selected) {
		// Can only update if message was already created.
		if (_message is null)
			return;

		// Update component by constructing a new DiscordMessage
		// from the data of the old one.
		// Interaction responses behave as webhooks and need to be
		// constructed as such.
		_message = await _interaction.GetOriginalResponseAsync();
		DiscordWebhookBuilder message =
			new DiscordWebhookBuilder()
			.WithContent(_message.Content);
		List<ComponentRow> rows = ComponentsSelectUpdated(
			new List<ComponentRow>(_message.Components),
			selected
		);
		if (rows.Count > 0)
			message.AddComponents(rows);

		// Edit original message.
		// This must be done through the original interaction, as
		// responses to interactions don't actually "exist" as real
		// messages.
		await _interaction
			.EditOriginalResponseAsync(message);
	}

	// Cleanup task to dispose of all resources.
	// Does not check for _message being completed yet.
	private async Task Cleanup() {
		if (_message is null)
			return;

		// Remove held references.
		_selections.TryRemove(_message.Id, out _);

		// Update message to disable component, constructing a new
		// DiscordMessage from the data of the old one.
		// Interaction responses behave as webhooks and need to be
		// constructed as such.
		_message = await _interaction.GetOriginalResponseAsync();
		DiscordWebhookBuilder message_new =
			new DiscordWebhookBuilder()
			.WithContent(_message.Content);
		List<ComponentRow> rows = ComponentsSelectDisabled(
			new List<ComponentRow>(_message.Components)
		);
		if (rows.Count > 0)
			message_new.AddComponents(rows);

		// Edit original message.
		// This must be done through the original interaction, as
		// responses to interactions don't actually "exist" as real
		// messages.
		await _interaction.EditOriginalResponseAsync(message_new);
	}

	public static Selection Create<T>(
		DiscordInteraction interaction,
		SelectionCallback callback,
		Task<DiscordMessage> messageTask,
		IReadOnlyList<KeyValuePair<T, Option>> options,
		IReadOnlySet<T> selected,
		string placeholder,
		bool isMultiple,
		TimeSpan? timeout=null
	) where T : Enum {
		timeout ??= DefaultTimeout;
		Timer timer = Util.CreateTimer(timeout.Value, false);

		// Construct select component options.
		List<SelectOption> options_obj = new ();
		foreach (KeyValuePair<T, Option> option in options) {
			Option option_obj = option.Value;
			SelectOption option_discord = new (
				option_obj.Label,
				option_obj.Id,
				option_obj.Description,
				selected.Contains(option.Key),
				option_obj.Emoji
			);
			options_obj.Add(option_discord);
		}

		// Construct select component.
		DiscordSelectComponent component = new (
			_id,
			placeholder,
			options_obj,
			disabled: false,
			minOptions: isMultiple ? 0 : 1,
			maxOptions: isMultiple ? options.Count : 1
		);

		// Construct partial Selection object.
		Selection selection =
			new (component, interaction, timer, callback);
		messageTask.ContinueWith((messageTask) => {
			DiscordMessage message = messageTask.Result;
			selection._message = message;
			_selections.TryAdd(message.Id, selection);
			selection._timer.Start();
		});
		timer.Elapsed += async (obj, e) => {
			// Run (or schedule to run) cleanup task.
			if (!messageTask.IsCompleted)
				await messageTask.ContinueWith((e) => selection.Cleanup());
			else
				await selection.Cleanup();
		};

		return selection;
	}

	// Formats the selected roles (from a given list) into a string.
	// The name to print should be lower case (this is not checked).
	// Casing will be converted to upper-case if needed, but not the
	// other way around.
	public static string PrintSelected<T>(
		IReadOnlyList<T> selected,
		IReadOnlyDictionary<T, Option> options,
		string name_singular,
		string name_plural
	) {
		// Casing conversions for names.
		string name_singular_upper =
			char.ToUpper(name_singular[0]) + name_singular[1..];
		string name_plural_upper =
			char.ToUpper(name_plural[0]) + name_plural[1..];

		// Special cases for none/singular.
		if (selected.Count == 0)
			return $"No {name_plural} previously set.";
		if (selected.Count == 1)
			return $"{name_singular_upper} previously set:\n**{options[selected[0]].Label}**";

		// Construct list of role names.
		StringWriter text = new ();
		text.WriteLine($"{name_plural_upper} previously set:");
		foreach (T option in selected)
			text.Write($"**{options[option].Label}**  ");
		return text.ToString()[..^2];
	}

	// Return a new list of components, with any DiscordSelectComponents
	// (with a matching ID) updated as selected.
	// IList (instead of read-only) allows members to be updated.
	private static List<ComponentRow> ComponentsSelectUpdated(
		List<ComponentRow> rows,
		IReadOnlySet<Option> selected
	) {
		List<ComponentRow> rows_new = new ();

		foreach (ComponentRow row in rows) {
			List<Component> components_new = new ();

			foreach (Component component in row.Components) {
				if (component is
					DiscordSelectComponent select &&
					component.CustomId == _id
				) {
					components_new.Add(UpdateSelect(select, selected));
				} else {
					components_new.Add(component);
				}
			}

			rows_new.Add(new ComponentRow(components_new));
		}

		return rows_new;
	}

	// Return a new list of components, with any DiscordSelectComponents
	// (with a matching ID) disabled.
	// IList (instead of read-only) allows members to be disabled.
	private static List<ComponentRow> ComponentsSelectDisabled(
		List<ComponentRow> rows
	) {
		List<ComponentRow> rows_new = new ();

		foreach (ComponentRow row in rows) {
			List<Component> components_new = new ();

			foreach (Component component in row.Components) {
				if (component is
					DiscordSelectComponent select &&
					component.CustomId == _id
				) {
					select.Disable();
					components_new.Add(select);
				} else {
					components_new.Add(component);
				}
			}

			rows_new.Add(new ComponentRow(components_new));
		}

		return rows_new;
	}

	// Convenience function for updating a DiscordSelectComponent with
	// a new set of options selected.
	// No checks are made.
	private static DiscordSelectComponent UpdateSelect(
		DiscordSelectComponent select,
		IReadOnlySet<Option> selected
	) {
		List<Option> selected_list = new (selected);

		// Create a list of options with updated "selected" state.
		List<SelectOption> options = new ();
		foreach (SelectOption option in select.Options) {
			// Check that the option is selected.
			bool isSelected = selected_list.Exists(
				(option_i) => option_i.Id == option.Value
			);
			// Construct a new option, copied from the original, but
			// with the appropriate "selected" state.
			SelectOption option_new = new (
				option.Label,
				option.Value,
				option.Description,
				isSelected,
				option.Emoji
			);
			options.Add(option_new);
		}

		// Construct new select component with the updated options.
		return new DiscordSelectComponent(
			select.CustomId,
			select.Placeholder,
			options,
			select.Disabled,
			select.MinimumSelectedValues ?? 1,
			select.MaximumSelectedValues ?? 1
		);
	}
}
