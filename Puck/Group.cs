﻿using DbBulletin = Puck.Databases.Bulletin;

namespace Puck;

class Group {
	public const int MaxMembers = 40;

	public readonly bool AcceptAnyRole;
	public readonly bool HasMaxCount;
	public readonly DiscordUser Owner;
	public int Tank => _tankList.Count;
	public int Heal => _healList.Count;
	public int Dps  => _dpsList.Count;
	public int Members => Tank + Heal + Dps;
	public string Type { get {
		if (!HasMaxCount)
			return "raid group";

		int max = AcceptAnyRole
		? _tankMax
		: _tankMax + _healMax + _dpsMax;
		string composition = AcceptAnyRole
		? "any"
		: string.Join("-", _tankMax, _healMax, _dpsMax);

		return $"{max}-man group ({composition})";
	} }

	private readonly int _tankMax, _healMax, _dpsMax;
	private readonly List<DiscordUser>
		_tankList = new (),
		_healList = new (),
		_dpsList  = new ();

	private const string _separator = ",";

	// Public builders to define a valid Group.
	public static Group WithAnyRole(DiscordUser owner) =>
		new (owner, true, false, 0, 0, 0);
	public static Group WithAnyRole(DiscordUser owner, int max) =>
		new (owner, true, true, max, max, max);
	public static Group WithRoles(DiscordUser owner, int tank, int heal, int dps) =>
		new (owner, false, true, tank, heal, dps);
	// Hidden constructor.
	private Group(
		DiscordUser owner,
		bool acceptAnyRole,
		bool hasMaxCount,
		int tank, int heal, int dps
	) {
		Owner = owner;
		AcceptAnyRole = acceptAnyRole;
		HasMaxCount = hasMaxCount;
		_tankMax = tank;
		_healMax = heal;
		_dpsMax = dps;
	}

	// Cycle through valid role states (as the owner).
	public void CycleTank() { Cycle(_tankList, _tankMax); }
	public void CycleHeal() { Cycle(_healList, _healMax); }
	public void CycleDps()  { Cycle(_dpsList , _dpsMax ); }
	private void Cycle(List<DiscordUser> members, int max) {
		bool canAdd = AcceptAnyRole
			? Members < max
			: members.Count < max;
		if (!HasMaxCount && (Members < MaxMembers))
			canAdd = true;

		if (canAdd) {
			members.Add(Owner);
		} else {
			int removed = members.RemoveAll(
				(member) => member == Owner
			);
		}
	}

	// Add a member. Removes member from any other lists.
	// Fails silently if group was full.
	public void AddTank(DiscordUser member)
		{ Add(member, _tankList, _tankMax); }
	public void AddHeal(DiscordUser member)
		{ Add(member, _healList, _healMax); }
	public void AddDps(DiscordUser member)
		{ Add(member, _dpsList, _dpsMax); }
	private void Add(DiscordUser member, List<DiscordUser> members, int max) {
		bool canAdd = AcceptAnyRole
			? Members < max
			: members.Count < max;
		if (!HasMaxCount)
			canAdd = true;

		// Ensure member isn't in another group already.
		Remove(member);

		// Just re-adds the member if they were already in the list.
		if (canAdd)
			members.Add(member);
	}

	// Removes a member from all lists.
	public void Remove(DiscordUser member) {
		_tankList.RemoveAll((member_i) => member_i == member);
		_healList.RemoveAll((member_i) => member_i == member);
		_dpsList.RemoveAll((member_i) => member_i == member);
	}

	// Prints a formatted list of all members in the group, as well
	// as any open spots (depending on the type of group defined).
	public string PrintMemberList(Emojis e) {
		List<string> output = new ();

		void PrintList(DiscordEmoji emoji, List<DiscordUser> members, int max) {
			foreach (DiscordUser member in members) {
				string name = (member == Owner)
					? "*Pre-filled*"
					: member.Mention;
				output.Add($"{emoji} - {name}");
			}
			if (HasMaxCount && !AcceptAnyRole) {
				for (int i=members.Count; i<max; i++)
					output.Add($"{emoji} - **[ Open ]**");
			}
		}
		PrintList(e.Tank, _tankList, _tankMax);
		PrintList(e.Heal, _healList, _healMax);
		PrintList(e.Dps , _dpsList , _dpsMax );

		// Print available spots if there is a max count, and any
		// role is accepted.
		if (HasMaxCount && AcceptAnyRole) {
			for (int i=Members; i<_tankMax; i++)
				output.Add("**[ Open ]**");
		}

		return output.ToLines();
	}

	public void WriteToDatabaseEntry(ref DbBulletin entry) {
		entry.OwnerId = Owner.Id.ToString();

		List<string> tankIds = new ();
		foreach (DiscordUser user in _tankList)
			tankIds.Add(user.Id.ToString());
		List<string> healIds = new ();
		foreach (DiscordUser user in _healList)
			healIds.Add(user.Id.ToString());
		List<string> dpsIds = new ();
		foreach (DiscordUser user in _dpsList)
			dpsIds.Add(user.Id.ToString());

		entry.TankIds = string.Join(_separator, tankIds);
		entry.HealIds = string.Join(_separator, healIds);
		entry.DpsIds  = string.Join(_separator, dpsIds );

		entry.AcceptAnyRole = AcceptAnyRole;
		entry.HasMaxCount = HasMaxCount;
		entry.TankMax = _tankMax;
		entry.HealMax = _healMax;
		entry.DpsMax  = _dpsMax ;
	}
	public async Task PopulateFromDatabaseEntry(DbBulletin entry) {
		_tankList.AddRange(await
			UsersFromListAsync(entry.TankIds)
		);
		_healList.AddRange(await
			UsersFromListAsync(entry.HealIds)
		);
		_dpsList.AddRange(await
			UsersFromListAsync(entry.DpsIds)
		);
	}

	private static async Task<List<DiscordUser>> UsersFromListAsync(string list) {
		List<DiscordUser> users = new ();
		string[] ids = list.Split(
			_separator,
			StringSplitOptions.RemoveEmptyEntries
		);
		
		foreach (string id in ids) {
			DiscordUser user = await Program.Client
				.GetUserAsync(ulong.Parse(id));
			users.Add(user);
		}

		return users;
	}
}
