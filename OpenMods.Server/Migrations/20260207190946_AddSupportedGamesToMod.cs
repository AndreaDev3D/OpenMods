using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenMods.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportedGamesToMod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameMod",
                columns: table => new
                {
                    ModsId = table.Column<int>(type: "integer", nullable: false),
                    SupportedGamesId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameMod", x => new { x.ModsId, x.SupportedGamesId });
                    table.ForeignKey(
                        name: "FK_GameMod_Games_SupportedGamesId",
                        column: x => x.SupportedGamesId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameMod_Mods_ModsId",
                        column: x => x.ModsId,
                        principalTable: "Mods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameMod_SupportedGamesId",
                table: "GameMod",
                column: "SupportedGamesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameMod");
        }
    }
}
