using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchService.Data.Migrations
{
    /// <inheritdoc />
    public partial class MatchUpdate07 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExpirationDate",
                table: "TfeProposal",
                newName: "CreationDate");

            migrationBuilder.AddColumn<DateOnly>(
                name: "CreationDate",
                table: "Tfe",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreationDate",
                table: "Tfe");

            migrationBuilder.RenameColumn(
                name: "CreationDate",
                table: "TfeProposal",
                newName: "ExpirationDate");
        }
    }
}
