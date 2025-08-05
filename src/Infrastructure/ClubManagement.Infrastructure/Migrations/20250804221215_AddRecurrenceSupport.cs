using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurrenceSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRecurringMaster",
                table: "Events",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastGeneratedUntil",
                table: "Events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MasterEventId",
                table: "Events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OccurrenceNumber",
                table: "Events",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecurrenceStatus",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Events_IsRecurringMaster",
                table: "Events",
                column: "IsRecurringMaster");

            migrationBuilder.CreateIndex(
                name: "IX_Events_MasterEventId",
                table: "Events",
                column: "MasterEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_MasterEventId_StartDateTime",
                table: "Events",
                columns: new[] { "MasterEventId", "StartDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_RecurrenceStatus_LastGeneratedUntil",
                table: "Events",
                columns: new[] { "RecurrenceStatus", "LastGeneratedUntil" });

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Events_MasterEventId",
                table: "Events",
                column: "MasterEventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_Events_MasterEventId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_IsRecurringMaster",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_MasterEventId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_MasterEventId_StartDateTime",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_RecurrenceStatus_LastGeneratedUntil",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "IsRecurringMaster",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "LastGeneratedUntil",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "MasterEventId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "OccurrenceNumber",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RecurrenceStatus",
                table: "Events");
        }
    }
}
