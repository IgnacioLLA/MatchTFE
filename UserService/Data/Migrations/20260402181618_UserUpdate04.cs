using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserService.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserUpdate04 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserInterest_Tag_InterestsId",
                table: "UserInterest");

            migrationBuilder.DropForeignKey(
                name: "FK_UserInterest_UserProfile_UserProfileUserId",
                table: "UserInterest");

            migrationBuilder.RenameColumn(
                name: "UserProfileUserId",
                table: "UserInterest",
                newName: "UserProfileId");

            migrationBuilder.RenameColumn(
                name: "InterestsId",
                table: "UserInterest",
                newName: "TagId");

            migrationBuilder.RenameIndex(
                name: "IX_UserInterest_UserProfileUserId",
                table: "UserInterest",
                newName: "IX_UserInterest_UserProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserInterest_Tag_TagId",
                table: "UserInterest",
                column: "TagId",
                principalTable: "Tag",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserInterest_UserProfile_UserProfileId",
                table: "UserInterest",
                column: "UserProfileId",
                principalTable: "UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserInterest_Tag_TagId",
                table: "UserInterest");

            migrationBuilder.DropForeignKey(
                name: "FK_UserInterest_UserProfile_UserProfileId",
                table: "UserInterest");

            migrationBuilder.RenameColumn(
                name: "UserProfileId",
                table: "UserInterest",
                newName: "UserProfileUserId");

            migrationBuilder.RenameColumn(
                name: "TagId",
                table: "UserInterest",
                newName: "InterestsId");

            migrationBuilder.RenameIndex(
                name: "IX_UserInterest_UserProfileId",
                table: "UserInterest",
                newName: "IX_UserInterest_UserProfileUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserInterest_Tag_InterestsId",
                table: "UserInterest",
                column: "InterestsId",
                principalTable: "Tag",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserInterest_UserProfile_UserProfileUserId",
                table: "UserInterest",
                column: "UserProfileUserId",
                principalTable: "UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
