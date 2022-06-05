namespace Puck;

static class GroupInstances {
	public enum WowDungeon {
		// Battle for Azeroth
		FH, SotS, WM, TD, SoB,
		AD, ToS , UR, ML, KR ,
		OpM, OpM_JY, OpM_WS,

		// Shadowlands
		NW , PF , MoTS, HoA,
		SoA, ToP, DOS , SD ,
		TV, TV_SoW, TV_SG,
	};

	public enum WowRaid {
		// Battle for Azeroth
		Uldir, BoD, CoS, TEP, NWC,

		// Shadowlands
		CN, SoD, SFO,
	};

	private static readonly ReadOnlyDictionary<WowDungeon, string> _wowDungeonUrls =
		new (new ConcurrentDictionary<WowDungeon, string> {
			[WowDungeon.NW  ] = @"",
			[WowDungeon.PF  ] = @"",
			[WowDungeon.MoTS] = @"",
			[WowDungeon.HoA ] = @"",
			[WowDungeon.SoA ] = @"",
			[WowDungeon.ToP ] = @"",
			[WowDungeon.DOS ] = @"",
			[WowDungeon.SD  ] = @"",
			[WowDungeon.TV  ] = @"",
			[WowDungeon.TV_SoW] = @"",
			[WowDungeon.TV_SG ] = @"",
		});
	private static readonly ReadOnlyDictionary<WowRaid, string> _wowRaidUrls =
		new (new ConcurrentDictionary<WowRaid, string> {
			[WowRaid.CN ] = @"",
			[WowRaid.SoD] = @"",
			[WowRaid.SFO] = @"",
		});

	public static string ThumbnailUrl(WowDungeon dungeon) =>
		_wowDungeonUrls[dungeon];
	public static string ThumbnailUrl(WowRaid raid) =>
		_wowRaidUrls[raid];

	public static WowDungeon? ParseWowDungeon(string input) {
		return null;
	}
	public static WowRaid? ParseWowRaid(string input) {
		return null;
	}
}
