using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Puck.Migrations.BulletinDatabaseMigrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bulletins",
                columns: table => new
                {
                    MessageId = table.Column<string>(type: "TEXT", nullable: false),
                    GuildId = table.Column<string>(type: "TEXT", nullable: false),
                    ThreadId = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    MentionId = table.Column<string>(type: "TEXT", nullable: false),
                    Expiry = table.Column<string>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<string>(type: "TEXT", nullable: false),
                    TankIds = table.Column<string>(type: "TEXT", nullable: false),
                    HealIds = table.Column<string>(type: "TEXT", nullable: false),
                    DpsIds = table.Column<string>(type: "TEXT", nullable: false),
                    AcceptAnyRole = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasMaxCount = table.Column<bool>(type: "INTEGER", nullable: false),
                    TankMax = table.Column<int>(type: "INTEGER", nullable: false),
                    HealMax = table.Column<int>(type: "INTEGER", nullable: false),
                    DpsMax = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bulletins", x => x.MessageId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bulletins");
        }
    }
}
