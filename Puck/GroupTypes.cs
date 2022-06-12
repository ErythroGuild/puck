namespace Puck;

static class GroupTypes {
	public enum Type {
		// Parties:
		Party2_Any,
		Party3_Any, Party3_012, Party3_111,
		Party4_Any, Party4_112,
		Party5_Any, Party5_113, Party5_122,
		Party6_Any, Party6_222,
		Party8_Any, Party8_224,

		// Raid groups:
		Raid10, Raid16,
		Raid20, Raid24, Raid25,
		Raid30,
		Raid40,
	}

	#region GroupType IDs (private const string)
	private const string
		// Parties:
		_id_party2_any = "2-any",
		_id_party3_any = "3-any",
		_id_party3_012 = "3-012",
		_id_party3_111 = "3-111",
		_id_party4_any = "4-any",
		_id_party4_112 = "4-112",
		_id_party5_any = "5-any",
		_id_party5_113 = "5-113",
		_id_party5_122 = "5-122",
		_id_party6_any = "6-any",
		_id_party6_222 = "6-222",
		_id_party8_any = "8-any",
		_id_party8_224 = "8-224",

		// Raid groups:
		_id_raid10 = "raid10",
		_id_raid16 = "raid16",
		_id_raid20 = "raid20",
		_id_raid24 = "raid24",
		_id_raid25 = "raid25",
		_id_raid30 = "raid30",
		// "raid" is used for compatibility with existing data.
		// (It used to be the only raid group type available.)
		_id_raid40 = "raid";
	#endregion

	private static readonly ConstBiMap<Type, string> _typeId_map =
		new (new Dictionary<Type, string> {
			// Parties:
			[Type.Party2_Any] = _id_party2_any,
			[Type.Party3_Any] = _id_party3_any,
			[Type.Party3_012] = _id_party3_012,
			[Type.Party3_111] = _id_party3_111,
			[Type.Party4_Any] = _id_party4_any,
			[Type.Party4_112] = _id_party4_112,
			[Type.Party5_Any] = _id_party5_any,
			[Type.Party5_113] = _id_party5_113,
			[Type.Party5_122] = _id_party5_122,
			[Type.Party6_Any] = _id_party6_any,
			[Type.Party6_222] = _id_party6_222,
			[Type.Party8_Any] = _id_party8_any,
			[Type.Party8_224] = _id_party8_224,

			// Raid groups:
			[Type.Raid10] = _id_raid10,
			[Type.Raid16] = _id_raid16,
			[Type.Raid20] = _id_raid20,
			[Type.Raid24] = _id_raid24,
			[Type.Raid25] = _id_raid25,
			[Type.Raid30] = _id_raid30,
			[Type.Raid40] = _id_raid40,
		});
	private static readonly ConstBiMap<Type, string> _typeName_map =
		new (new Dictionary<Type, string> {
			// Parties:
			[Type.Party2_Any] = "2-man (any)"  ,
			[Type.Party3_Any] = "3-man (any)"  ,
			[Type.Party3_012] = "3-man (0-1-2)",
			[Type.Party3_111] = "3-man (1-1-1)",
			[Type.Party4_Any] = "4-man (any)"  ,
			[Type.Party4_112] = "4-man (1-1-2)",
			[Type.Party5_Any] = "5-man (any)"  ,
			[Type.Party5_113] = "5-man (1-1-3)",
			[Type.Party5_122] = "5-man (1-2-2)",
			[Type.Party6_Any] = "6-man (any)"  ,
			[Type.Party6_222] = "6-man (2-2-2)",
			[Type.Party8_Any] = "8-man (any)"  ,
			[Type.Party8_224] = "8-man (2-2-4)",

			// Raid groups:
			[Type.Raid10] = "10-man raid",
			[Type.Raid16] = "16-man raid",
			[Type.Raid20] = "20-man raid",
			[Type.Raid24] = "24-man raid",
			[Type.Raid25] = "25-man raid",
			[Type.Raid30] = "30-man raid",
			[Type.Raid40] = "40-man raid",
		});

	public static Type TypeFromId(string id) => _typeId_map[id];
	public static Type TypeFromName(string name) => _typeName_map[name];
	public static string GetId(Type type) => _typeId_map[type];
	public static string GetName(Type type) => _typeName_map[type];

	public static Group CreateGroup(Type type, DiscordUser owner) =>
		type switch {
			// Parties:
			Type.Party2_Any => Group.WithAnyRole(owner, 2),
			Type.Party3_Any => Group.WithAnyRole(owner, 3),
			Type.Party3_012 => Group.WithRoles(owner, 0, 1, 2),
			Type.Party3_111 => Group.WithRoles(owner, 1, 1, 1),
			Type.Party4_Any => Group.WithAnyRole(owner, 4),
			Type.Party4_112 => Group.WithRoles(owner, 1, 1, 2),
			Type.Party5_Any => Group.WithAnyRole(owner, 5),
			Type.Party5_113 => Group.WithRoles(owner, 1, 1, 3),
			Type.Party5_122 => Group.WithRoles(owner, 1, 2, 2),
			Type.Party6_Any => Group.WithAnyRole(owner, 6),
			Type.Party6_222 => Group.WithRoles(owner, 2, 2, 2),
			Type.Party8_Any => Group.WithAnyRole(owner, 8),
			Type.Party8_224 => Group.WithRoles(owner, 2, 2, 4),

			// Raid groups:
			Type.Raid10 => Group.WithAnyRole(owner, 10),
			Type.Raid16 => Group.WithAnyRole(owner, 16),
			Type.Raid20 => Group.WithAnyRole(owner, 20),
			Type.Raid24 => Group.WithAnyRole(owner, 24),
			Type.Raid25 => Group.WithAnyRole(owner, 25),
			Type.Raid30 => Group.WithAnyRole(owner, 30),
			Type.Raid40 => Group.WithAnyRole(owner, 40),

			_ => throw new ArgumentException("Unknown group type.", nameof(type)),
		};

	public static IList<Type> GetAllTypes() =>
		Enum.GetValues<Type>();
	public static IList<Type> GetDefaultTypes() =>
		new List<Type> {
			Type.Party2_Any,
			Type.Party3_Any,
			Type.Party5_Any,
			Type.Party5_113,
			Type.Raid20,
			Type.Raid30,
		};

	public static IList<string> GetTypeIds(IReadOnlyList<Type> types) {
		List<string> ids = new ();
		foreach (Type type in types)
			ids.Add(GetId(type));
		return ids;
	}
	public static IList<string> GetTypeNames(IReadOnlyList<Type> types) {
		List<string> names = new ();
		foreach (Type type in types)
			names.Add(GetName(type));
		return names;
	}
}
