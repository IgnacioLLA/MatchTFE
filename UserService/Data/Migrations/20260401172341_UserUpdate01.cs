using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserService.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserUpdate01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudentSkill",
                columns: table => new
                {
                    StudentProfileId = table.Column<string>(type: "text", nullable: false),
                    TagId = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentSkill", x => new { x.StudentProfileId, x.TagId });
                    table.ForeignKey(
                        name: "FK_StudentSkill_UserProfile_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "UserProfile",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserInterest",
                columns: table => new
                {
                    InterestsId = table.Column<int>(type: "integer", nullable: false),
                    UserProfileUserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserInterest", x => new { x.InterestsId, x.UserProfileUserId });
                    table.ForeignKey(
                        name: "FK_UserInterest_UserProfile_UserProfileUserId",
                        column: x => x.UserProfileUserId,
                        principalTable: "UserProfile",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserInterest_UserProfileUserId",
                table: "UserInterest",
                column: "UserProfileUserId");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentSkill");

            migrationBuilder.DropTable(
                name: "UserInterest");
        }
    }
}
