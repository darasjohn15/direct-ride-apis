using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DirectRide.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilePhotoKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfilePhotoKey",
                table: "Users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePhotoKey",
                table: "Users");
        }
    }
}
