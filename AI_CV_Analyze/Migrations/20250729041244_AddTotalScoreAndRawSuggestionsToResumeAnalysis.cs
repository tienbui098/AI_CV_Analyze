using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI_CV_Analyze.Migrations
{
    /// <inheritdoc />
    public partial class AddTotalScoreAndRawSuggestionsToResumeAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RawSuggestions",
                table: "ResumeAnalysis",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TotalScore",
                table: "ResumeAnalysis",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RawSuggestions",
                table: "ResumeAnalysis");

            migrationBuilder.DropColumn(
                name: "TotalScore",
                table: "ResumeAnalysis");
        }
    }
}
