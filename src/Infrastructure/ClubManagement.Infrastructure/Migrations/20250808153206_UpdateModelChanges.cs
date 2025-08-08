using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FacilityBookings_Facilities_FacilityId",
                table: "FacilityBookings");

            migrationBuilder.DropForeignKey(
                name: "FK_FacilityBookings_Members_MemberId",
                table: "FacilityBookings");

            migrationBuilder.DropForeignKey(
                name: "FK_FacilityBookings_Users_BookedByUserId",
                table: "FacilityBookings");

            migrationBuilder.DropForeignKey(
                name: "FK_FacilityBookings_Users_CancelledByUserId",
                table: "FacilityBookings");

            migrationBuilder.DropForeignKey(
                name: "FK_FacilityBookings_Users_CheckedInByUserId",
                table: "FacilityBookings");

            migrationBuilder.DropForeignKey(
                name: "FK_FacilityBookings_Users_CheckedOutByUserId",
                table: "FacilityBookings");

            migrationBuilder.DropIndex(
                name: "IX_FacilityBookings_FacilityId",
                table: "FacilityBookings");

            migrationBuilder.DropIndex(
                name: "IX_FacilityBookings_MemberId",
                table: "FacilityBookings");

            migrationBuilder.DropIndex(
                name: "IX_Facilities_FacilityTypeId",
                table: "Facilities");

            migrationBuilder.DropIndex(
                name: "IX_Events_FacilityId",
                table: "Events");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "Members",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AllowedMembershipTiers",
                table: "FacilityTypes",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequiredCertifications",
                table: "FacilityTypes",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "RequiresSupervision",
                table: "FacilityTypes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AdditionalParticipants",
                table: "FacilityBookings",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "BookingSource",
                table: "FacilityBookings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsRecurring",
                table: "FacilityBookings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MemberNotes",
                table: "FacilityBookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RecurrenceGroupId",
                table: "FacilityBookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresStaffSupervision",
                table: "FacilityBookings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AllowedMembershipTiers",
                table: "Facilities",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "Facilities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Facilities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MemberConcurrentBookingLimit",
                table: "Facilities",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MemberHourlyRate",
                table: "Facilities",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NonMemberHourlyRate",
                table: "Facilities",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "RequiredCertifications",
                table: "Facilities",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "RequiresMemberSupervision",
                table: "Facilities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AllowedMembershipTiers",
                table: "Events",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MaximumAge",
                table: "Events",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinimumAge",
                table: "Events",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequiredCertifications",
                table: "Events",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "RequiresFacilityAccess",
                table: "Events",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "MemberBookingLimits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    FacilityId = table.Column<Guid>(type: "uuid", nullable: true),
                    FacilityTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApplicableTier = table.Column<int>(type: "integer", nullable: true),
                    MaxConcurrentBookings = table.Column<int>(type: "integer", nullable: false),
                    MaxBookingsPerDay = table.Column<int>(type: "integer", nullable: false),
                    MaxBookingsPerWeek = table.Column<int>(type: "integer", nullable: false),
                    MaxBookingsPerMonth = table.Column<int>(type: "integer", nullable: false),
                    MaxBookingDurationHours = table.Column<int>(type: "integer", nullable: false),
                    MaxAdvanceBookingDays = table.Column<int>(type: "integer", nullable: false),
                    MinAdvanceBookingHours = table.Column<int>(type: "integer", nullable: false),
                    EarliestBookingTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    LatestBookingTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    AllowedDays = table.Column<string>(type: "jsonb", nullable: true),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false),
                    AllowRecurringBookings = table.Column<bool>(type: "boolean", nullable: false),
                    CancellationPenaltyHours = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberBookingLimits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberBookingLimits_Facilities_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "Facilities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MemberBookingLimits_FacilityTypes_FacilityTypeId",
                        column: x => x.FacilityTypeId,
                        principalTable: "FacilityTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MemberBookingLimits_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MemberFacilityCertifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    CertificationType = table.Column<string>(type: "text", nullable: false),
                    CertifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CertifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberFacilityCertifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberFacilityCertifications_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MemberFacilityCertifications_Users_CertifiedByUserId",
                        column: x => x.CertifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Members_UserId1",
                table: "Members",
                column: "UserId1",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FacilityBookings_BookingSource",
                table: "FacilityBookings",
                column: "BookingSource");

            migrationBuilder.CreateIndex(
                name: "IX_FacilityBookings_FacilityId_StartDateTime_EndDateTime",
                table: "FacilityBookings",
                columns: new[] { "FacilityId", "StartDateTime", "EndDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_FacilityBookings_MemberId_Status",
                table: "FacilityBookings",
                columns: new[] { "MemberId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FacilityBookings_RecurrenceGroupId",
                table: "FacilityBookings",
                column: "RecurrenceGroupId",
                filter: "\"RecurrenceGroupId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FacilityBookings_Status_StartDateTime",
                table: "FacilityBookings",
                columns: new[] { "Status", "StartDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Facilities_FacilityTypeId_Status",
                table: "Facilities",
                columns: new[] { "FacilityTypeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Facilities_Location",
                table: "Facilities",
                column: "Location");

            migrationBuilder.CreateIndex(
                name: "IX_Facilities_Status_TenantId",
                table: "Facilities",
                columns: new[] { "Status", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_FacilityId_StartDateTime_EndDateTime",
                table: "Events",
                columns: new[] { "FacilityId", "StartDateTime", "EndDateTime" },
                filter: "\"FacilityId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Events_RequiresFacilityAccess",
                table: "Events",
                column: "RequiresFacilityAccess",
                filter: "\"RequiresFacilityAccess\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_MemberBookingLimits_ApplicableTier_IsActive",
                table: "MemberBookingLimits",
                columns: new[] { "ApplicableTier", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberBookingLimits_EffectiveFrom_EffectiveTo_IsActive",
                table: "MemberBookingLimits",
                columns: new[] { "EffectiveFrom", "EffectiveTo", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberBookingLimits_FacilityId_IsActive",
                table: "MemberBookingLimits",
                columns: new[] { "FacilityId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberBookingLimits_FacilityTypeId_IsActive",
                table: "MemberBookingLimits",
                columns: new[] { "FacilityTypeId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberBookingLimits_MemberId_IsActive",
                table: "MemberBookingLimits",
                columns: new[] { "MemberId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberFacilityCertifications_CertifiedByUserId",
                table: "MemberFacilityCertifications",
                column: "CertifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberFacilityCertifications_ExpiryDate_IsActive",
                table: "MemberFacilityCertifications",
                columns: new[] { "ExpiryDate", "IsActive" },
                filter: "\"ExpiryDate\" IS NOT NULL AND \"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_MemberFacilityCertifications_MemberId_CertificationType_IsA~",
                table: "MemberFacilityCertifications",
                columns: new[] { "MemberId", "CertificationType", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_FacilityBookings_Facilities_FacilityId",
                table: "FacilityBookings",
                column: "FacilityId",
                principalTable: "Facilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FacilityBookings_Members_MemberId",
                table: "FacilityBookings",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FacilityBookings_Users_BookedByUserId",
                table: "FacilityBookings",
                column: "BookedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FacilityBookings_Users_CancelledByUserId",
                table: "FacilityBookings",
                column: "CancelledByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FacilityBookings_Users_CheckedInByUserId",
                table: "FacilityBookings",
                column: "CheckedInByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FacilityBookings_Users_CheckedOutByUserId",
                table: "FacilityBookings",
                column: "CheckedOutByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Members_Users_UserId1",
                table: "Members",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FacilityBookings_Facilities_FacilityId",
                table: "FacilityBookings");

            migrationBuilder.DropForeignKey(
                name: "FK_FacilityBookings_Members_MemberId",
                table: "FacilityBookings");

            migrationBuilder.DropForeignKey(
                name: "FK_FacilityBookings_Users_BookedByUserId",
                table: "FacilityBookings");

            migrationBuilder.DropForeignKey(
                name: "FK_FacilityBookings_Users_CancelledByUserId",
                table: "FacilityBookings");

            migrationBuilder.DropForeignKey(
                name: "FK_FacilityBookings_Users_CheckedInByUserId",
                table: "FacilityBookings");

            migrationBuilder.DropForeignKey(
                name: "FK_FacilityBookings_Users_CheckedOutByUserId",
                table: "FacilityBookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Members_Users_UserId1",
                table: "Members");

            migrationBuilder.DropTable(
                name: "MemberBookingLimits");

            migrationBuilder.DropTable(
                name: "MemberFacilityCertifications");

            migrationBuilder.DropIndex(
                name: "IX_Members_UserId1",
                table: "Members");

            migrationBuilder.DropIndex(
                name: "IX_FacilityBookings_BookingSource",
                table: "FacilityBookings");

            migrationBuilder.DropIndex(
                name: "IX_FacilityBookings_FacilityId_StartDateTime_EndDateTime",
                table: "FacilityBookings");

            migrationBuilder.DropIndex(
                name: "IX_FacilityBookings_MemberId_Status",
                table: "FacilityBookings");

            migrationBuilder.DropIndex(
                name: "IX_FacilityBookings_RecurrenceGroupId",
                table: "FacilityBookings");

            migrationBuilder.DropIndex(
                name: "IX_FacilityBookings_Status_StartDateTime",
                table: "FacilityBookings");

            migrationBuilder.DropIndex(
                name: "IX_Facilities_FacilityTypeId_Status",
                table: "Facilities");

            migrationBuilder.DropIndex(
                name: "IX_Facilities_Location",
                table: "Facilities");

            migrationBuilder.DropIndex(
                name: "IX_Facilities_Status_TenantId",
                table: "Facilities");

            migrationBuilder.DropIndex(
                name: "IX_Events_FacilityId_StartDateTime_EndDateTime",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_RequiresFacilityAccess",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "AllowedMembershipTiers",
                table: "FacilityTypes");

            migrationBuilder.DropColumn(
                name: "RequiredCertifications",
                table: "FacilityTypes");

            migrationBuilder.DropColumn(
                name: "RequiresSupervision",
                table: "FacilityTypes");

            migrationBuilder.DropColumn(
                name: "AdditionalParticipants",
                table: "FacilityBookings");

            migrationBuilder.DropColumn(
                name: "BookingSource",
                table: "FacilityBookings");

            migrationBuilder.DropColumn(
                name: "IsRecurring",
                table: "FacilityBookings");

            migrationBuilder.DropColumn(
                name: "MemberNotes",
                table: "FacilityBookings");

            migrationBuilder.DropColumn(
                name: "RecurrenceGroupId",
                table: "FacilityBookings");

            migrationBuilder.DropColumn(
                name: "RequiresStaffSupervision",
                table: "FacilityBookings");

            migrationBuilder.DropColumn(
                name: "AllowedMembershipTiers",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "MemberConcurrentBookingLimit",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "MemberHourlyRate",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "NonMemberHourlyRate",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "RequiredCertifications",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "RequiresMemberSupervision",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "AllowedMembershipTiers",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "MaximumAge",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "MinimumAge",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RequiredCertifications",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RequiresFacilityAccess",
                table: "Events");

            migrationBuilder.CreateIndex(
                name: "IX_FacilityBookings_FacilityId",
                table: "FacilityBookings",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_FacilityBookings_MemberId",
                table: "FacilityBookings",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Facilities_FacilityTypeId",
                table: "Facilities",
                column: "FacilityTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_FacilityId",
                table: "Events",
                column: "FacilityId");

            migrationBuilder.AddForeignKey(
                name: "FK_FacilityBookings_Facilities_FacilityId",
                table: "FacilityBookings",
                column: "FacilityId",
                principalTable: "Facilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FacilityBookings_Members_MemberId",
                table: "FacilityBookings",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FacilityBookings_Users_BookedByUserId",
                table: "FacilityBookings",
                column: "BookedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FacilityBookings_Users_CancelledByUserId",
                table: "FacilityBookings",
                column: "CancelledByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FacilityBookings_Users_CheckedInByUserId",
                table: "FacilityBookings",
                column: "CheckedInByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FacilityBookings_Users_CheckedOutByUserId",
                table: "FacilityBookings",
                column: "CheckedOutByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
