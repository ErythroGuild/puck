namespace Puck;

class Initializer {
	// Public fields + properties.
	public Task CompletionTask => _completion.Task;
	public bool IsComplete => CompletionTask.IsCompleted;

	private readonly TaskCompletionSource _completion = new ();
	private readonly List<Task> _predicates = new ();

	public Initializer(ICollection<Task> predicates) {
		_predicates = new (predicates);

		// Start waiting for predicates.
		// We can return immediately.
		_ = WaitForPredicates();
	}

	public void ContinueWith(Task task) {
		CompletionTask.ContinueWith(async (t) => { await task; });
	}

	private async Task WaitForPredicates() {
		await Task.WhenAll(_predicates.ToArray());
		_completion.SetResult();
	}
}
