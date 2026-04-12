using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchService.Data.Migrations
{
    /// <inheritdoc />
    public partial class MatchUpdate06 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TfeRequiredSkill_Tfe_TFEId",
                table: "TfeRequiredSkill");

            migrationBuilder.DropForeignKey(
                name: "FK_TfeRequiredSkill_Tfe_TfeId",
                table: "TfeRequiredSkill");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TfeRequiredSkill",
                table: "TfeRequiredSkill");

            migrationBuilder.DropIndex(
                name: "IX_TfeRequiredSkill_TFEId",
                table: "TfeRequiredSkill");

            migrationBuilder.DropColumn(
                name: "TfeId",
                table: "TfeRequiredSkill");

            migrationBuilder.RenameColumn(
                name: "TFEId",
                table: "TfeRequiredSkill",
                newName: "TfeId");

            migrationBuilder.AlterColumn<int>(
                name: "TfeId",
                table: "TfeRequiredSkill",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TfeRequiredSkill",
                table: "TfeRequiredSkill",
                columns: new[] { "TfeId", "TagId" });

            migrationBuilder.AddForeignKey(
                name: "FK_TfeRequiredSkill_Tfe_TfeId",
                table: "TfeRequiredSkill",
                column: "TfeId",
                principalTable: "Tfe",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TfeRequiredSkill_Tfe_TfeId",
                table: "TfeRequiredSkill");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TfeRequiredSkill",
                table: "TfeRequiredSkill");

            migrationBuilder.RenameColumn(
                name: "TfeId",
                table: "TfeRequiredSkill",
                newName: "TFEId");

            migrationBuilder.AlterColumn<int>(
                name: "TFEId",
                table: "TfeRequiredSkill",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "TfeId",
                table: "TfeRequiredSkill",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TfeRequiredSkill",
                table: "TfeRequiredSkill",
                columns: new[] { "TfeId", "TagId" });

            migrationBuilder.CreateIndex(
                name: "IX_TfeRequiredSkill_TFEId",
                table: "TfeRequiredSkill",
                column: "TFEId");

            migrationBuilder.AddForeignKey(
                name: "FK_TfeRequiredSkill_Tfe_TFEId",
                table: "TfeRequiredSkill",
                column: "TFEId",
                principalTable: "Tfe",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TfeRequiredSkill_Tfe_TfeId",
                table: "TfeRequiredSkill",
                column: "TfeId",
                principalTable: "Tfe",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
