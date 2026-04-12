using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchService.Data.Migrations
{
    /// <inheritdoc />
    public partial class MatchUpdate05 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TfeProposal_Tfe_TFEId",
                table: "TfeProposal");

            migrationBuilder.DropForeignKey(
                name: "FK_TfeTopic_Tfe_TFEId",
                table: "TfeTopic");

            migrationBuilder.RenameColumn(
                name: "TFEId",
                table: "TfeTopic",
                newName: "TfeId");

            migrationBuilder.RenameColumn(
                name: "TFEId",
                table: "TfeProposal",
                newName: "TfeId");

            migrationBuilder.RenameIndex(
                name: "IX_TfeProposal_TFEId",
                table: "TfeProposal",
                newName: "IX_TfeProposal_TfeId");

            migrationBuilder.AddForeignKey(
                name: "FK_TfeProposal_Tfe_TfeId",
                table: "TfeProposal",
                column: "TfeId",
                principalTable: "Tfe",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TfeTopic_Tfe_TfeId",
                table: "TfeTopic",
                column: "TfeId",
                principalTable: "Tfe",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TfeProposal_Tfe_TfeId",
                table: "TfeProposal");

            migrationBuilder.DropForeignKey(
                name: "FK_TfeTopic_Tfe_TfeId",
                table: "TfeTopic");

            migrationBuilder.RenameColumn(
                name: "TfeId",
                table: "TfeTopic",
                newName: "TFEId");

            migrationBuilder.RenameColumn(
                name: "TfeId",
                table: "TfeProposal",
                newName: "TFEId");

            migrationBuilder.RenameIndex(
                name: "IX_TfeProposal_TfeId",
                table: "TfeProposal",
                newName: "IX_TfeProposal_TFEId");

            migrationBuilder.AddForeignKey(
                name: "FK_TfeProposal_Tfe_TFEId",
                table: "TfeProposal",
                column: "TFEId",
                principalTable: "Tfe",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TfeTopic_Tfe_TFEId",
                table: "TfeTopic",
                column: "TFEId",
                principalTable: "Tfe",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
