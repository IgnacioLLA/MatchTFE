using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchService.Data.Migrations
{
    /// <inheritdoc />
    public partial class MatchUpdate09 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE \"TfeProposal\"
                ALTER COLUMN \"Status\" TYPE integer
                USING CASE lower(\"Status\")
                    WHEN 'pending' THEN 1
                    WHEN 'accepted' THEN 2
                    WHEN 'rejected' THEN 3
                    WHEN 'expired' THEN 0
                    ELSE \"Status\"::integer
                END;

                ALTER TABLE \"Tfe\"
                ALTER COLUMN \"Status\" TYPE integer
                USING CASE lower(\"Status\")
                    WHEN 'open' THEN 1
                    WHEN 'completed' THEN 2
                    WHEN 'cancelled' THEN 0
                    ELSE \"Status\"::integer
                END;

                ALTER TABLE \"InterestProposal\"
                ALTER COLUMN \"Status\" TYPE integer
                USING CASE lower(\"Status\")
                    WHEN 'pending' THEN 1
                    WHEN 'accepted' THEN 2
                    WHEN 'rejected' THEN 3
                    WHEN 'expired' THEN 0
                    ELSE \"Status\"::integer
                END;" );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE \"TfeProposal\"
                ALTER COLUMN \"Status\" TYPE text
                USING CASE \"Status\"
                    WHEN 1 THEN 'Pending'
                    WHEN 2 THEN 'Accepted'
                    WHEN 3 THEN 'Rejected'
                    WHEN 0 THEN 'Expired'
                    ELSE \"Status\"::text
                END;

                ALTER TABLE \"Tfe\"
                ALTER COLUMN \"Status\" TYPE text
                USING CASE \"Status\"
                    WHEN 1 THEN 'Open'
                    WHEN 2 THEN 'Completed'
                    WHEN 0 THEN 'Cancelled'
                    ELSE \"Status\"::text
                END;

                ALTER TABLE \"InterestProposal\"
                ALTER COLUMN \"Status\" TYPE text
                USING CASE \"Status\"
                    WHEN 1 THEN 'Pending'
                    WHEN 2 THEN 'Accepted'
                    WHEN 3 THEN 'Rejected'
                    WHEN 0 THEN 'Expired'
                    ELSE \"Status\"::text
                END;" );
        }
    }
}
