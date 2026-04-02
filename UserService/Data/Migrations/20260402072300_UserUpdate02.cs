using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserService.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserUpdate02 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StudentSkill_TagId",
                table: "StudentSkill",
                column: "TagId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentSkill_Tag_TagId",
                table: "StudentSkill",
                column: "TagId",
                principalTable: "Tag",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserInterest_Tag_InterestsId",
                table: "UserInterest",
                column: "InterestsId",
                principalTable: "Tag",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentSkill_Tag_TagId",
                table: "StudentSkill");

            migrationBuilder.DropForeignKey(
                name: "FK_UserInterest_Tag_InterestsId",
                table: "UserInterest");

            migrationBuilder.DropIndex(
                name: "IX_StudentSkill_TagId",
                table: "StudentSkill");
        }
    }
}
