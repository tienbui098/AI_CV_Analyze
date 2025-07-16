using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI_CV_Analyze.Migrations
{
    /// <inheritdoc />
    public partial class add_tieu_chi_cham_diem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EducationScore",
                table: "ResumeAnalysis",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExperienceScore",
                table: "ResumeAnalysis",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FormatScore",
                table: "ResumeAnalysis",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "KeywordScore",
                table: "ResumeAnalysis",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LayoutScore",
                table: "ResumeAnalysis",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SkillScore",
                table: "ResumeAnalysis",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EducationScore",
                table: "ResumeAnalysis");

            migrationBuilder.DropColumn(
                name: "ExperienceScore",
                table: "ResumeAnalysis");

            migrationBuilder.DropColumn(
                name: "FormatScore",
                table: "ResumeAnalysis");

            migrationBuilder.DropColumn(
                name: "KeywordScore",
                table: "ResumeAnalysis");

            migrationBuilder.DropColumn(
                name: "LayoutScore",
                table: "ResumeAnalysis");

            migrationBuilder.DropColumn(
                name: "SkillScore",
                table: "ResumeAnalysis");
        }
    }
}
