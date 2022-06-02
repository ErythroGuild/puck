namespace Puck;

class TaskQueue {
	public bool IsRunning => !_task.IsCompleted;

	private readonly ConcurrentQueue<Task> _queue = new ();
	private Task _task = Task.CompletedTask;

	public void Add(Task action) {
		_queue.Enqueue(action);

		if (!IsRunning) {
			_task = Task.Run(async () => {
				while (!_queue.IsEmpty) {
					_queue.TryDequeue(out Task? task);

					if (task is null)
						continue;

					if (task.Status == TaskStatus.Created)
						task.Start();

					await task;
				}
			});
		}
	}
}
