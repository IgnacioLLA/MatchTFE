using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchService.Data.Migrations
{
    /// <inheritdoc />
    public partial class MatchUpdate10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TfeRequiredSkill_Tag_TagId",
                table: "TfeRequiredSkill");

            migrationBuilder.AddForeignKey(
                name: "FK_TfeRequiredSkill_Tag_TagId",
                table: "TfeRequiredSkill",
                column: "TagId",
                principalTable: "Tag",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TfeRequiredSkill_Tag_TagId",
                table: "TfeRequiredSkill");

            migrationBuilder.AddForeignKey(
                name: "FK_TfeRequiredSkill_Tag_TagId",
                table: "TfeRequiredSkill",
                column: "TagId",
                principalTable: "Tag",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
