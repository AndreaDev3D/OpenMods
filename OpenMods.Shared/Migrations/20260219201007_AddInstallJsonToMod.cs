using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenMods.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddInstallJsonToMod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InstallJson",
                table: "Mods",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InstallJson",
                table: "Mods");
        }
    }
}
