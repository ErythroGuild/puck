using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

using Microsoft.EntityFrameworkCore;

namespace Puck.Databases;

class GuildConfigDatabase : DbContext {
	public DbSet<GuildConfig> Configs { get; set; } = null!;
	
	private const string _pathConfig =
		@"Data Source=data/config.sqlite";

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
		optionsBuilder.UseSqlite(_pathConfig);
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		modelBuilder.Entity<GuildConfig>();

		modelBuilder.Entity<GuildRole>()
			.HasIndex(t => t.GuildId);
		modelBuilder.Entity<GroupType>()
			.HasIndex(t => t.GuildId);

		// Consider using a one-time script that compacts the
		// database by reassigning IDs to the appropriate rowid.
		// This could work better than the below.
		//modelBuilder.Entity<GuildRole>()
		//	.Property(t => t.Id)
		//	.UseAutoIncrement(false);
		//modelBuilder.Entity<GroupType>()
		//	.Property(t => t.Id)
		//	.UseAutoIncrement(false);
		//// SQLite usually uses _rowid_ instead of AUTOINCREMENT.
		//// (This only works with integer IDs though).
		//// Temporary workaround until the above is available:
		//modelBuilder.Entity<GuildRole>()
		//	.Property(t => t.Id)
		//	.HasConversion(v => v, v => v);
		//modelBuilder.Entity<GroupType>()
		//	.Property(t => t.Id)
		//	.HasConversion(v => v, v => v);
	}

	public GuildConfig? GetConfig(ulong guildId) {
		IEnumerable<GuildConfig> config_list =
			from _config in Configs
				.Include(c => c.AllowedGroupTypes)
				.Include(c => c.AllowedRoles)
			where _config.GuildId == guildId.ToString()
			select _config;
		return config_list.Any()
			? config_list.First()
			: null;
	}

	public static GuildConfig GetConfigOrDefault(DiscordGuild guild) {
		using GuildConfigDatabase database = new ();
		GuildConfig? config = database.GetConfig(guild.Id);
		if (config is null)
			config = DefaultConfig(guild);
		return config;
	}

	// This is a separate method because Entity Framework will
	// always call the mapped constructor before populating
	// members from the database.
	// (Even if the object already exists in the database.)
	public static GuildConfig DefaultConfig(DiscordGuild guild) {
		GuildConfig config = new (guild.Id.ToString(), guild.Name) {
			DefaultGroupType = "5-113",
			DefaultDurationMsec = GuildConfig._defaultDuration.TotalMilliseconds,
			AllowedRoles = new HashSet<GuildRole>(),
			AllowedGroupTypes = new HashSet<GroupType>(),
		};

		// (No allowed roles by default.)
		// Populate default allowed group types.
		IReadOnlyList<string> groupTypes =
			Commands.LFG.DefaultGroupTypes;
		foreach (string groupType in groupTypes) {
			config. AllowedGroupTypes.Add(
				new (config.GuildId, groupType)
			);
		}

		return config;
	}
}

[Table("GuildConfigs")]
class GuildConfig {
	[NotMapped]
	public static readonly TimeSpan _defaultDuration =
		TimeSpan.FromMinutes(5);

	[Key] public string GuildId { get; set; }

	[Required] public string GuildName           { get; set; }
	[Required] public string DefaultGroupType    { get; set; } = null!;
	[Required] public double DefaultDurationMsec { get; set; }

	[ForeignKey("GuildId")] public virtual ICollection<GuildRole> AllowedRoles      { get; set; } = null!;
	[ForeignKey("GuildId")] public virtual ICollection<GroupType> AllowedGroupTypes { get; set; } = null!;

	public GuildConfig(string guildId, string guildName) {
		GuildId = guildId;
		GuildName = guildName;
	}

	public TimeSpan DefaultDuration() =>
		TimeSpan.FromMilliseconds(DefaultDurationMsec);

	public IReadOnlyList<ulong> RoleList() {
		List<ulong> list = new ();
		foreach (GuildRole role in AllowedRoles)
			list.Add(ulong.Parse(role.RoleId));
		return list;
	}

	public IReadOnlyDictionary<ulong, string> RoleGroupTypes() {
		Dictionary<ulong, string> table = new ();
		foreach (GuildRole role in AllowedRoles)
			table.Add(ulong.Parse(role.RoleId), role.GroupTypeName);
		return table;
	}

	public IReadOnlyList<string> GroupTypeList() {
		List<string> list = new ();
		foreach (GroupType groupType in AllowedGroupTypes)
			list.Add(groupType.GroupTypeName);
		return list;
	}
}

[Table("GuildRoles")]
class GuildRole {
	[Key] public long Id { get; set; }

	[Required] public string GuildId { get; set; }
	[Required] public string RoleId  { get; set; }
	[Required] public string GroupTypeName { get; set; }

	public GuildRole(string guildId, string roleId, string groupTypeName) {
		GuildId = guildId;
		RoleId = roleId;
		GroupTypeName = groupTypeName;
	}
}

[Table("GroupTypes")]
class GroupType {
	[Key] public long Id { get; set; }

	[Required] public string GuildId       { get; set; }
	[Required] public string GroupTypeName { get; set; }

	public GroupType(string guildId, string groupTypeName) {
		GuildId = guildId;
		GroupTypeName = groupTypeName;
	}
}

