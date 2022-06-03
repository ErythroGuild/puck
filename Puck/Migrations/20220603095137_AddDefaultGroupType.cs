using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Puck.Migrations
{
    public partial class AddDefaultGroupType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultGroupType",
                table: "GuildConfigs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultGroupType",
                table: "GuildConfigs");
        }
    }
}
