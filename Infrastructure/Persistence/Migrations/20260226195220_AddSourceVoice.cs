using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExulofraApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceVoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceVoice",
                table: "Translations",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceVoice",
                table: "Translations");
        }
    }
}
