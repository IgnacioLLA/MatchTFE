using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchService.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixTfeProposalFaltaFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TfeProposal_UserProfile_UserProfileUserId",
                table: "TfeProposal");

            migrationBuilder.DropIndex(
                name: "IX_TfeProposal_UserProfileUserId",
                table: "TfeProposal");

            migrationBuilder.DropColumn(
                name: "UserProfileUserId",
                table: "TfeProposal");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserProfileUserId",
                table: "TfeProposal",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TfeProposal_UserProfileUserId",
                table: "TfeProposal",
                column: "UserProfileUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TfeProposal_UserProfile_UserProfileUserId",
                table: "TfeProposal",
                column: "UserProfileUserId",
                principalTable: "UserProfile",
                principalColumn: "UserId");
        }
    }
}
