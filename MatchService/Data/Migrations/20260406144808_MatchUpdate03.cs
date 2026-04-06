using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchService.Data.Migrations
{
    /// <inheritdoc />
    public partial class MatchUpdate03 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TfeRequiredSkill",
                columns: table => new
                {
                    TfeId = table.Column<int>(type: "integer", nullable: false),
                    TagId = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TfeRequiredSkill", x => new { x.TfeId, x.TagId });
                    table.ForeignKey(
                        name: "FK_TfeRequiredSkill_Tag_TagId",
                        column: x => x.TagId,
                        principalTable: "Tag",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TfeRequiredSkill_Tfe_TfeId",
                        column: x => x.TfeId,
                        principalTable: "Tfe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TfeRequiredSkill_TagId",
                table: "TfeRequiredSkill",
                column: "TagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TfeRequiredSkill");
        }
    }
}
