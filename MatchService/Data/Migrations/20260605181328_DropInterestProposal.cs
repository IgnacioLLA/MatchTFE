using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchService.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropInterestProposal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterestProposal");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InterestProposal",
                columns: table => new
                {
                    OriginUserId = table.Column<string>(type: "text", nullable: false),
                    DestinationUserId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterestProposal", x => new { x.OriginUserId, x.DestinationUserId });
                    table.ForeignKey(
                        name: "FK_InterestProposal_UserProfile_DestinationUserId",
                        column: x => x.DestinationUserId,
                        principalTable: "UserProfile",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterestProposal_UserProfile_OriginUserId",
                        column: x => x.OriginUserId,
                        principalTable: "UserProfile",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterestProposal_DestinationUserId",
                table: "InterestProposal",
                column: "DestinationUserId");
        }
    }
}
