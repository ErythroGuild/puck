using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace Puck.Database;

class GuildConfigDatabase : DbContext {
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

	public GuildConfig(string guildId, string guildName) {
		GuildId = guildId;
		GuildName = guildName;

		DefaultDurationMsec = _defaultDuration.TotalMilliseconds;

		AllowedRoles = new HashSet<GuildRole>();
		AllowedGroupTypes = new HashSet<GroupType>();
	}
}

[Table("GuildRoles")]
class GuildRole {
	[Key] public long Id { get; set; }

	[Required] public string GuildId { get; set; }
	[Required] public string RoleId  { get; set; }

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

	public GroupType(string guildId, string groupTypeName) {
		GuildId = guildId;
		GroupTypeName = groupTypeName;
	}
}

