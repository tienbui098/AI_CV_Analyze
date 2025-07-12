using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI_CV_Analyze.Migrations
{
    /// <inheritdoc />
    public partial class FixSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobCategories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobCategories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Resumes",
                columns: table => new
                {
                    ResumeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FileData = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    UploadDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    AnalysisStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resumes", x => x.ResumeId);
                    table.CheckConstraint("CK_Resumes_AnalysisStatus", "AnalysisStatus IN ('Pending', 'Processing', 'Completed', 'Failed')");
                    table.CheckConstraint("CK_Resumes_FileType", "FileType IN ('PDF', 'JPG', 'PNG')");
                    table.ForeignKey(
                        name: "FK_Resumes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobCategoryRecommendations",
                columns: table => new
                {
                    RecommendationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResumeId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    RelevanceScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobCategoryRecommendations", x => x.RecommendationId);
                    table.CheckConstraint("CK_JobCategoryRecommendations_RelevanceScore", "RelevanceScore BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_JobCategoryRecommendations_JobCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "JobCategories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobCategoryRecommendations_Resumes_ResumeId",
                        column: x => x.ResumeId,
                        principalTable: "Resumes",
                        principalColumn: "ResumeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResumeAnalysis",
                columns: table => new
                {
                    AnalysisId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResumeId = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Suggestions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnalysisDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResumeAnalysis", x => x.AnalysisId);
                    table.CheckConstraint("CK_ResumeAnalysis_Score", "Score BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_ResumeAnalysis_Resumes_ResumeId",
                        column: x => x.ResumeId,
                        principalTable: "Resumes",
                        principalColumn: "ResumeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResumeAnalysisResults",
                columns: table => new
                {
                    AnalysisResultId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResumeId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnalysisDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AnalysisStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DetectedText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageAnalysis = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FormFields = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConfidenceScore = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KeyPhrases = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Entities = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SentimentScore = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Skills = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SkillsAnalysis = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Experience = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Education = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverallAnalysis = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResumeAnalysisResults", x => x.AnalysisResultId);
                    table.ForeignKey(
                        name: "FK_ResumeAnalysisResults_Resumes_ResumeId",
                        column: x => x.ResumeId,
                        principalTable: "Resumes",
                        principalColumn: "ResumeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResumeData",
                columns: table => new
                {
                    ResumeDataId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResumeId = table.Column<int>(type: "int", nullable: false),
                    ExtractedData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResumeData", x => x.ResumeDataId);
                    table.CheckConstraint("CK_ResumeData_Language", "Language IN ('English', 'Vietnamese')");
                    table.CheckConstraint("CK_ResumeData_Status", "Status IN ('Pending', 'Processing', 'Completed', 'Failed')");
                    table.ForeignKey(
                        name: "FK_ResumeData_Resumes_ResumeId",
                        column: x => x.ResumeId,
                        principalTable: "Resumes",
                        principalColumn: "ResumeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResumeHistory",
                columns: table => new
                {
                    HistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResumeId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FileData = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    UploadDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResumeHistory", x => x.HistoryId);
                    table.CheckConstraint("CK_ResumeHistory_Score", "Score BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_ResumeHistory_Resumes_ResumeId",
                        column: x => x.ResumeId,
                        principalTable: "Resumes",
                        principalColumn: "ResumeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobCategoryRecommendations_CategoryId",
                table: "JobCategoryRecommendations",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_JobCategoryRecommendations_ResumeId",
                table: "JobCategoryRecommendations",
                column: "ResumeId");

            migrationBuilder.CreateIndex(
                name: "IX_ResumeAnalysis_ResumeId",
                table: "ResumeAnalysis",
                column: "ResumeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResumeAnalysisResults_ResumeId",
                table: "ResumeAnalysisResults",
                column: "ResumeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResumeData_ResumeId",
                table: "ResumeData",
                column: "ResumeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResumeHistory_ResumeId",
                table: "ResumeHistory",
                column: "ResumeId");

            migrationBuilder.CreateIndex(
                name: "IX_Resumes_UserId",
                table: "Resumes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobCategoryRecommendations");

            migrationBuilder.DropTable(
                name: "ResumeAnalysis");

            migrationBuilder.DropTable(
                name: "ResumeAnalysisResults");

            migrationBuilder.DropTable(
                name: "ResumeData");

            migrationBuilder.DropTable(
                name: "ResumeHistory");

            migrationBuilder.DropTable(
                name: "JobCategories");

            migrationBuilder.DropTable(
                name: "Resumes");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
