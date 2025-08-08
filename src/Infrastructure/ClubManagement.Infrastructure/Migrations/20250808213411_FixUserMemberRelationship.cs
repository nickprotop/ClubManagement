using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixUserMemberRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Members_Users_UserId1",
                table: "Members");

            migrationBuilder.DropIndex(
                name: "IX_Members_UserId",
                table: "Members");

            migrationBuilder.DropIndex(
                name: "IX_Members_UserId1",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "Members");

            migrationBuilder.CreateIndex(
                name: "IX_Members_UserId",
                table: "Members",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Members_UserId",
                table: "Members");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "Members",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Members_UserId",
                table: "Members",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Members_UserId1",
                table: "Members",
                column: "UserId1",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Members_Users_UserId1",
                table: "Members",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
