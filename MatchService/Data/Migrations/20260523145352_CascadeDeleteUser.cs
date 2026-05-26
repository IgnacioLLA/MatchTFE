using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchService.Data.Migrations
{
    /// <inheritdoc />
    public partial class CascadeDeleteUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InterestProposal_UserProfile_DestinationUserId",
                table: "InterestProposal");

            migrationBuilder.DropForeignKey(
                name: "FK_InterestProposal_UserProfile_OriginUserId",
                table: "InterestProposal");

            migrationBuilder.DropForeignKey(
                name: "FK_TfeProposal_UserProfile_OriginUserId",
                table: "TfeProposal");

            migrationBuilder.AddForeignKey(
                name: "FK_InterestProposal_UserProfile_DestinationUserId",
                table: "InterestProposal",
                column: "DestinationUserId",
                principalTable: "UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InterestProposal_UserProfile_OriginUserId",
                table: "InterestProposal",
                column: "OriginUserId",
                principalTable: "UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TfeProposal_UserProfile_OriginUserId",
                table: "TfeProposal",
                column: "OriginUserId",
                principalTable: "UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InterestProposal_UserProfile_DestinationUserId",
                table: "InterestProposal");

            migrationBuilder.DropForeignKey(
                name: "FK_InterestProposal_UserProfile_OriginUserId",
                table: "InterestProposal");

            migrationBuilder.DropForeignKey(
                name: "FK_TfeProposal_UserProfile_OriginUserId",
                table: "TfeProposal");

            migrationBuilder.AddForeignKey(
                name: "FK_InterestProposal_UserProfile_DestinationUserId",
                table: "InterestProposal",
                column: "DestinationUserId",
                principalTable: "UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InterestProposal_UserProfile_OriginUserId",
                table: "InterestProposal",
                column: "OriginUserId",
                principalTable: "UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TfeProposal_UserProfile_OriginUserId",
                table: "TfeProposal",
                column: "OriginUserId",
                principalTable: "UserProfile",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
