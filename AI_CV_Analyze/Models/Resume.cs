using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AI_CV_Analyze.Models
{
    public class Resume
    {
        [Key]
        public int ResumeId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; }

        [Required]
        [StringLength(10)]
        public string FileType { get; set; }

        [Required]
        public byte[] FileData { get; set; }

        public DateTime UploadDate { get; set; } = DateTime.UtcNow;

        public int Version { get; set; } = 1;

        // Analysis Status
        [StringLength(20)]
        public string AnalysisStatus { get; set; } = "Pending"; // Pending, Processing, Completed, Failed

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
        public virtual ResumeData ResumeData { get; set; }
        public virtual ResumeAnalysis ResumeAnalysis { get; set; }
        public virtual ResumeAnalysisResult ResumeAnalysisResult { get; set; }
        public virtual ICollection<JobCategoryRecommendation> JobCategoryRecommendations { get; set; }
        public virtual ICollection<ResumeHistory> ResumeHistories { get; set; }
    }
}
