using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserService.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserUpdate05 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentSkill_Tag_TagId",
                table: "StudentSkill");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentSkill_Tag_TagId",
                table: "StudentSkill",
                column: "TagId",
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

            migrationBuilder.AddForeignKey(
                name: "FK_StudentSkill_Tag_TagId",
                table: "StudentSkill",
                column: "TagId",
                principalTable: "Tag",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
