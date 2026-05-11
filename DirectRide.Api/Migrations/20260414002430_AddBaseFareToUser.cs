using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DirectRide.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBaseFareToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BaseFare",
                table: "Users",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseFare",
                table: "Users");
        }
    }
}
