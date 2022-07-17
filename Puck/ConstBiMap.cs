namespace Puck;

class ConstBiMap<T, U>
	where T: notnull
	where U: notnull
{
	private readonly ReadOnlyDictionary<T, U> _tableForward;
	private readonly ReadOnlyDictionary<U, T> _tableReverse;

	public ConstBiMap(IReadOnlyDictionary<T, U> table) {
		ConcurrentDictionary<T, U> tableForward = new (table);
		ConcurrentDictionary<U, T> tableReverse = new ();

		foreach (T key in tableForward.Keys) {
			U value = tableForward[key];
			if (tableReverse.ContainsKey(value))
				throw new ArgumentException("Input map is not one-to-one.", nameof(table));
			else
				tableReverse.TryAdd(value, key);
		}

		_tableForward = new (tableForward);
		_tableReverse = new (tableReverse);
	}

	public U this[T key] => _tableForward[key];
	public T this[U key] => _tableReverse[key];

	// Explicit accessors are provided in case the indexer overloads
	// are ambiguous (T and U are the same type).
	public U GetForward(T key) => _tableForward[key];
	public T GetReverse(U key) => _tableReverse[key];
}
