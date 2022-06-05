using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

using Microsoft.EntityFrameworkCore;

namespace Puck.Databases;

class BulletinDatabase : DbContext {
	public DbSet<Bulletin> Bulletins { get; set; } = null!;

	private const string _pathBulletins =
		@"Data Source=data/bulletins.sqlite";

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
		optionsBuilder.UseSqlite(_pathBulletins);
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		modelBuilder.Entity<Bulletin>();
	}

	public Bulletin? GetBulletinFromMessage(ulong messageId) {
		IEnumerable<Bulletin> bulletin_list =
			from _bulletin in Bulletins
			where _bulletin.MessageId == messageId.ToString()
			select _bulletin;
		return bulletin_list.Any()
			? bulletin_list.First()
			: null;
	}

	public List<Bulletin> GetBulletins() {
		List<Bulletin> bulletins = new (
			from _bulletin in Bulletins
			select _bulletin
		);
		return bulletins;
	}
}

[Table("Bulletins")]
class Bulletin {
	[Key] public string MessageId { get; set; }

	// Bulletin data.
	[Required] public string GuildId     { get; set; } = null!;
	[Required] public string ThreadId    { get; set; } = null!;
	[Required] public string Title       { get; set; } = null!;
	[Required] public string Description { get; set; } = null!;
	[Required] public string MentionId   { get; set; } = null!;
	[Required] public string Expiry      { get; set; } = null!;

	// Group data.
	[Required] public string OwnerId { get; set; } = null!;
	[Required] public string TankIds { get; set; } = null!;
	[Required] public string HealIds { get; set; } = null!;
	[Required] public string DpsIds  { get; set; } = null!;
	[Required] public bool AcceptAnyRole { get; set; }
	[Required] public bool HasMaxCount   { get; set; }
	[Required] public int TankMax { get; set; }
	[Required] public int HealMax { get; set; }
	[Required] public int DpsMax  { get; set; }

	public Bulletin(string messageId) {
		MessageId = messageId;
	}
}
