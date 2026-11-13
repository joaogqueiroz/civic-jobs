using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivicJobs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class JobPostingApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "JobPostings",
                type: "TEXT",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "JobPostings");
        }
    }
}
