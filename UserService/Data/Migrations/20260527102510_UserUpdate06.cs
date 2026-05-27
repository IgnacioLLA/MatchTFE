using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserService.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserUpdate06 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserProfileUserId",
                table: "StudentSkill",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentSkill_UserProfileUserId",
                table: "StudentSkill",
                column: "UserProfileUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentSkill_UserProfile_UserProfileUserId",
                table: "StudentSkill",
                column: "UserProfileUserId",
                principalTable: "UserProfile",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentSkill_UserProfile_UserProfileUserId",
                table: "StudentSkill");

            migrationBuilder.DropIndex(
                name: "IX_StudentSkill_UserProfileUserId",
                table: "StudentSkill");

            migrationBuilder.DropColumn(
                name: "UserProfileUserId",
                table: "StudentSkill");
        }
    }
}
