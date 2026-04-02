using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MatchService.Data.Migrations
{
    /// <inheritdoc />
    public partial class MatchUpdate01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InterestProposal",
                columns: table => new
                {
                    OriginUserId = table.Column<string>(type: "text", nullable: false),
                    DestinationUserId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterestProposal", x => new { x.OriginUserId, x.DestinationUserId });
                    table.ForeignKey(
                        name: "FK_InterestProposal_UserProfile_DestinationUserId",
                        column: x => x.DestinationUserId,
                        principalTable: "UserProfile",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InterestProposal_UserProfile_OriginUserId",
                        column: x => x.OriginUserId,
                        principalTable: "UserProfile",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tag",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tag", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tfe",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AuthorId = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Title = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    EstimatedDelivery = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tfe", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tfe_UserProfile_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "UserProfile",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TfeProposal",
                columns: table => new
                {
                    OriginUserId = table.Column<string>(type: "text", nullable: false),
                    TFEId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ExpirationDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TfeProposal", x => new { x.OriginUserId, x.TFEId });
                    table.ForeignKey(
                        name: "FK_TfeProposal_Tfe_TFEId",
                        column: x => x.TFEId,
                        principalTable: "Tfe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TfeProposal_UserProfile_OriginUserId",
                        column: x => x.OriginUserId,
                        principalTable: "UserProfile",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TfeTopic",
                columns: table => new
                {
                    TFEId = table.Column<int>(type: "integer", nullable: false),
                    TagId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TfeTopic", x => new { x.TFEId, x.TagId });
                    table.ForeignKey(
                        name: "FK_TfeTopic_Tag_TagId",
                        column: x => x.TagId,
                        principalTable: "Tag",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TfeTopic_Tfe_TFEId",
                        column: x => x.TFEId,
                        principalTable: "Tfe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterestProposal_DestinationUserId",
                table: "InterestProposal",
                column: "DestinationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tfe_AuthorId",
                table: "Tfe",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_TfeProposal_TFEId",
                table: "TfeProposal",
                column: "TFEId");

            migrationBuilder.CreateIndex(
                name: "IX_TfeTopic_TagId",
                table: "TfeTopic",
                column: "TagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterestProposal");

            migrationBuilder.DropTable(
                name: "TfeProposal");

            migrationBuilder.DropTable(
                name: "TfeTopic");

            migrationBuilder.DropTable(
                name: "Tag");

            migrationBuilder.DropTable(
                name: "Tfe");
        }
    }
}
