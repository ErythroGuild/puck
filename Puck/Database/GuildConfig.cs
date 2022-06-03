using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

using Microsoft.EntityFrameworkCore;

namespace Puck.Database;

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
	}

	public GuildConfig? GetConfig(ulong guildId) {
		IEnumerable<GuildConfig> config_list =
			from _config in Configs
			where _config.GuildId == guildId.ToString()
			select _config;
		return config_list.Any()
			? config_list.First()
			: null;
	}
}

[Table("GuildConfigs")]
class GuildConfig {
	[NotMapped]
	private static readonly TimeSpan _defaultDuration =
		TimeSpan.FromMinutes(5);

	[Key] public string GuildId { get; set; }

	[Required] public string GuildName           { get; set; }
	[Required] public double DefaultDurationMsec { get; set; }

	[ForeignKey("GuildId")] public virtual ICollection<GuildRole> AllowedRoles      { get; set; }
	[ForeignKey("GuildId")] public virtual ICollection<GroupType> AllowedGroupTypes { get; set; }

	public GuildConfig(ulong guildId, string guildName)
		: this (guildId.ToString(), guildName) { }
	public GuildConfig(string guildId, string guildName) {
		GuildId = guildId;
		GuildName = guildName;

		DefaultDurationMsec = _defaultDuration.TotalMilliseconds;

		AllowedRoles = new HashSet<GuildRole>();
		AllowedGroupTypes = new HashSet<GroupType>();
	}

	public TimeSpan DefaultDuration() =>
		TimeSpan.FromMilliseconds(DefaultDurationMsec);

	public IList<ulong> RoleList() {
		List<ulong> list = new ();
		foreach (GuildRole role in AllowedRoles)
			list.Add(ulong.Parse(role.RoleId));
		return list;
	}

	public IList<string> GroupTypeList() {
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

	public GuildRole(ulong guildId, ulong roleId)
		: this (guildId.ToString(), roleId.ToString()) { }
	public GuildRole(string guildId, string roleId) {
		GuildId = guildId;
		RoleId = roleId;
	}
}

[Table("GroupTypes")]
class GroupType {
	[Key] public long Id { get; set; }

	[Required] public string GuildId       { get; set; }
	[Required] public string GroupTypeName { get; set; }

	public GroupType(ulong guildId, string groupTypeName)
		: this (guildId.ToString(), groupTypeName) { }
	public GroupType(string guildId, string groupTypeName) {
		GuildId = guildId;
		GroupTypeName = groupTypeName;
	}
}

