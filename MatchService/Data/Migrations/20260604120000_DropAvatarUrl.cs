using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchService.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropAvatarUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "UserProfile");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "UserProfile",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");
        }
    }
}
