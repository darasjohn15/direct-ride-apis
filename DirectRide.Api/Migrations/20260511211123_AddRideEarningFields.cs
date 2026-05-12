using DirectRide.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DirectRide.Api.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260511211123_AddRideEarningFields")]
    public partial class AddRideEarningFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "RideRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DriverEarningsAmount",
                table: "RideRequests",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0.00m);

            migrationBuilder.AddColumn<decimal>(
                name: "FareAmount",
                table: "RideRequests",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0.00m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "RideRequests");

            migrationBuilder.DropColumn(
                name: "DriverEarningsAmount",
                table: "RideRequests");

            migrationBuilder.DropColumn(
                name: "FareAmount",
                table: "RideRequests");
        }
    }
}
