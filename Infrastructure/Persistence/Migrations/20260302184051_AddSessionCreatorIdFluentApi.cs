using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExulofraApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionCreatorIdFluentApi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_Users_CreatorId",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_CreatorId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "Sessions");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_CreatorUserId",
                table: "Sessions",
                column: "CreatorUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_Users_CreatorUserId",
                table: "Sessions",
                column: "CreatorUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_Users_CreatorUserId",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_CreatorUserId",
                table: "Sessions");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatorId",
                table: "Sessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_CreatorId",
                table: "Sessions",
                column: "CreatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_Users_CreatorId",
                table: "Sessions",
                column: "CreatorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
