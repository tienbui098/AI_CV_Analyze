using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI_CV_Analyze.Migrations
{
    /// <inheritdoc />
    public partial class AddResumeDataAndResumeHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ResumeHistory_Score",
                table: "ResumeHistory");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ResumeData_Language",
                table: "ResumeData");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ResumeData_Status",
                table: "ResumeData");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_ResumeHistory_Score",
                table: "ResumeHistory",
                sql: "Score BETWEEN 0 AND 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ResumeData_Language",
                table: "ResumeData",
                sql: "Language IN ('English', 'Vietnamese')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ResumeData_Status",
                table: "ResumeData",
                sql: "Status IN ('Pending', 'Processing', 'Completed', 'Failed')");
        }
    }
}
