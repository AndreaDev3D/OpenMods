using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenMods.Server.Migrations
{
    /// <inheritdoc />
    public partial class FixNullTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Mods\" SET \"Tags\" = ARRAY[]::text[] WHERE \"Tags\" IS NULL;");

            migrationBuilder.AlterColumn<System.Collections.Generic.List<string>>(
                name: "Tags",
                table: "Mods",
                type: "text[]",
                nullable: false,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
