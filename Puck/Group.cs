namespace Puck;

class Group {
	public readonly DiscordMember Owner;
	public int Tank => _tankList.Count;
	public int Heal => _healList.Count;
	public int Dps  => _dpsList.Count;
	public int Members => Tank + Heal + Dps;

	private readonly bool _acceptAnyRole;
	private readonly bool _hasMaxCount;
	private readonly int _tankMax, _healMax, _dpsMax;
	private readonly List<DiscordMember>
		_tankList = new (),
		_healList = new (),
		_dpsList  = new ();

	// Public builders to define a valid Group.
	public static Group WithAnyRole(DiscordMember owner) =>
		new (owner, true, false, 0, 0, 0);
	public static Group WithAnyRole(DiscordMember owner, int max) =>
		new (owner, true, true, max, max, max);
	public static Group WithRoles(DiscordMember owner, int tank, int heal, int dps) =>
		new (owner, false, true, tank, heal, dps);
	// Hidden constructor.
	private Group(
		DiscordMember owner,
		bool acceptAnyRole,
		bool hasMaxCount,
		int tank, int heal, int dps
	) {
		Owner = owner;
		_acceptAnyRole = acceptAnyRole;
		_hasMaxCount = hasMaxCount;
		_tankMax = tank;
		_healMax = heal;
		_dpsMax = dps;
	}

	// Cycle through valid role states (as the owner).
	public void CycleTank() { Cycle(_tankList, _tankMax); }
	public void CycleHeal() { Cycle(_healList, _healMax); }
	public void CycleDps()  { Cycle(_dpsList , _dpsMax ); }
	private void Cycle(List<DiscordMember> members, int max) {
		bool canAdd = _acceptAnyRole
			? Members < max
			: members.Count < max;
		if (!_hasMaxCount)
			canAdd = true;

		if (canAdd) {
			members.Add(Owner);
		} else {
			int removed = members.RemoveAll(
				(member) => member == Owner
			);
			if (removed > 0)
				members.Add(Owner);
		}
	}

	// Add a member. Removes member from any other lists.
	// Fails silently if group was full.
	public void AddTank(DiscordMember member)
		{ Add(member, _tankList, _tankMax); }
	public void AddHeal(DiscordMember member)
		{ Add(member, _healList, _healMax); }
	public void AddDps(DiscordMember member)
		{ Add(member, _dpsList, _dpsMax); }
	private void Add(DiscordMember member, List<DiscordMember> members, int max) {
		bool canAdd = _acceptAnyRole
			? Members < max
			: members.Count < max;
		if (!_hasMaxCount)
			canAdd = true;

		// Ensure member isn't in another group already.
		Remove(member);

		// Just re-adds the member if they were already in the list.
		if (canAdd)
			members.Add(member);
	}

	// Removes a member from all lists.
	public void Remove(DiscordMember member) {
		_tankList.RemoveAll((member_i) => member_i == member);
		_healList.RemoveAll((member_i) => member_i == member);
		_dpsList.RemoveAll((member_i) => member_i == member);
	}

	// Prints the group type, unformatted (a plain string).
	public string PrintGroupType() {
		if (!_hasMaxCount)
			return "raid group";

		int max = _acceptAnyRole
			? _tankMax
			: _tankMax + _healMax + _dpsMax;
		string composition = _acceptAnyRole
			? "any"
			: string.Join("-", _tankMax, _healMax, _dpsMax);

		return $"{max}-man group ({composition})";
	}
	// Prints a formatted list of all members in the group, as well
	// as any open spots (depending on the type of group defined).
	public string PrintMemberList(Emojis e) {
		List<string> output = new ();

		void PrintList(DiscordEmoji emoji, List<DiscordMember> members, int max) {
			foreach (DiscordMember member in members) {
				string name = (member == Owner)
					? "*Pre-filled*"
					: member.Mention;
				output.Add($"{emoji} - {name}");
			}
			if (_hasMaxCount && !_acceptAnyRole) {
				for (int i=members.Count; i<max; i++)
					output.Add($"{emoji} - **[ Open ]**");
			}
		}
		PrintList(e.Tank, _tankList, _tankMax);
		PrintList(e.Heal, _healList, _healMax);
		PrintList(e.Dps , _dpsList , _dpsMax );

		// Print available spots if there is a max count, and any
		// role is accepted.
		if (_hasMaxCount && _acceptAnyRole) {
			for (int i=Members; i<_tankMax; i++)
				output.Add("**[ Open ]**");
		}

		return output.ToLines();
	}
}
