using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventHardwareIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventEquipmentRequirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    HardwareTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    SpecificHardwareId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    IsMandatory = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    AutoAssign = table.Column<bool>(type: "boolean", nullable: false),
                    MinimumCondition = table.Column<int>(type: "integer", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventEquipmentRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventEquipmentRequirements_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventEquipmentRequirements_HardwareTypes_HardwareTypeId",
                        column: x => x.HardwareTypeId,
                        principalTable: "HardwareTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EventEquipmentRequirements_Hardware_SpecificHardwareId",
                        column: x => x.SpecificHardwareId,
                        principalTable: "Hardware",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EventEquipmentAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequirementId = table.Column<Guid>(type: "uuid", nullable: false),
                    HardwareId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CheckedOutAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReturnedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventEquipmentAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventEquipmentAssignments_EventEquipmentRequirements_Requir~",
                        column: x => x.RequirementId,
                        principalTable: "EventEquipmentRequirements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventEquipmentAssignments_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventEquipmentAssignments_Hardware_HardwareId",
                        column: x => x.HardwareId,
                        principalTable: "Hardware",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventEquipmentAssignments_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventEquipmentAssignments_EventId",
                table: "EventEquipmentAssignments",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventEquipmentAssignments_HardwareId",
                table: "EventEquipmentAssignments",
                column: "HardwareId");

            migrationBuilder.CreateIndex(
                name: "IX_EventEquipmentAssignments_MemberId",
                table: "EventEquipmentAssignments",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_EventEquipmentAssignments_RequirementId",
                table: "EventEquipmentAssignments",
                column: "RequirementId");

            migrationBuilder.CreateIndex(
                name: "IX_EventEquipmentRequirements_EventId",
                table: "EventEquipmentRequirements",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventEquipmentRequirements_HardwareTypeId",
                table: "EventEquipmentRequirements",
                column: "HardwareTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EventEquipmentRequirements_SpecificHardwareId",
                table: "EventEquipmentRequirements",
                column: "SpecificHardwareId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventEquipmentAssignments");

            migrationBuilder.DropTable(
                name: "EventEquipmentRequirements");
        }
    }
}
