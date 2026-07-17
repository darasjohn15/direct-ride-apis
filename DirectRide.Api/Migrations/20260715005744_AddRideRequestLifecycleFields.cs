using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DirectRide.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRideRequestLifecycleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "RideRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "RideRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CancelledByUserId",
                table: "RideRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledAt",
                table: "RideRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "RideRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "RideRequests" AS r
                SET "ScheduledAt" = a."StartTime"
                FROM "AvailabilitySlots" AS a
                WHERE r."AvailabilitySlotId" = a."Id";
                """);

            migrationBuilder.Sql("""
                UPDATE "RideRequests"
                SET "ScheduledAt" = "CreatedAt"
                WHERE "ScheduledAt" IS NULL;
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ScheduledAt",
                table: "RideRequests",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "RideRequests");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "RideRequests");

            migrationBuilder.DropColumn(
                name: "CancelledByUserId",
                table: "RideRequests");

            migrationBuilder.DropColumn(
                name: "ScheduledAt",
                table: "RideRequests");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "RideRequests");
        }
    }
}
