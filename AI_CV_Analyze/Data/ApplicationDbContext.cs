using AI_CV_Analyze.Models;
using Microsoft.EntityFrameworkCore;

namespace AI_CV_Analyze.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Resume> Resumes { get; set; }
        public DbSet<ResumeData> ResumeData { get; set; }
        public DbSet<ResumeAnalysis> ResumeAnalysis { get; set; }
        public DbSet<ResumeAnalysisResult> ResumeAnalysisResults { get; set; }
        public DbSet<JobCategory> JobCategories { get; set; }
        public DbSet<JobCategoryRecommendation> JobCategoryRecommendations { get; set; }
        public DbSet<ResumeHistory> ResumeHistory { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ràng buộc loại file cho Resume
            modelBuilder.Entity<Resume>()
                .HasCheckConstraint("CK_Resumes_FileType", "FileType IN ('PDF', 'JPG', 'PNG')");

            // Ràng buộc trạng thái phân tích cho Resume
            modelBuilder.Entity<Resume>()
                .HasCheckConstraint("CK_Resumes_AnalysisStatus", "AnalysisStatus IN ('Pending', 'Processing', 'Completed', 'Failed')");

            // Ràng buộc ngôn ngữ cho ResumeData
            modelBuilder.Entity<ResumeData>()
                .HasCheckConstraint("CK_ResumeData_Language", "Language IN ('English', 'Vietnamese')");

            // Ràng buộc trạng thái cho ResumeData
            modelBuilder.Entity<ResumeData>()
                .HasCheckConstraint("CK_ResumeData_Status", "Status IN ('Pending', 'Processing', 'Completed', 'Failed')");

            // Ràng buộc điểm số cho ResumeAnalysis
            modelBuilder.Entity<ResumeAnalysis>()
                .HasCheckConstraint("CK_ResumeAnalysis_Score", "Score BETWEEN 0 AND 100");

            // Ràng buộc điểm phù hợp cho JobCategoryRecommendation
            modelBuilder.Entity<JobCategoryRecommendation>()
                .HasCheckConstraint("CK_JobCategoryRecommendations_RelevanceScore", "RelevanceScore BETWEEN 0 AND 100");

            // Ràng buộc điểm số cho ResumeHistory
            modelBuilder.Entity<ResumeHistory>()
                .HasCheckConstraint("CK_ResumeHistory_Score", "Score BETWEEN 0 AND 100");

            modelBuilder.Entity<ResumeAnalysis>()
                .Property(r => r.LayoutScore).HasDefaultValue(0);
            modelBuilder.Entity<ResumeAnalysis>()
                .Property(r => r.SkillScore).HasDefaultValue(0);
            modelBuilder.Entity<ResumeAnalysis>()
                .Property(r => r.ExperienceScore).HasDefaultValue(0);
            modelBuilder.Entity<ResumeAnalysis>()
                .Property(r => r.EducationScore).HasDefaultValue(0);
            modelBuilder.Entity<ResumeAnalysis>()
                .Property(r => r.KeywordScore).HasDefaultValue(0);
            modelBuilder.Entity<ResumeAnalysis>()
                .Property(r => r.FormatScore).HasDefaultValue(0);
        }
    }
} 