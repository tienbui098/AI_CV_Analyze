using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AI_CV_Analyze.Models
{
    public class ResumeAnalysis
    {
        [Key]
        public int AnalysisId { get; set; }

        [Required]
        public int ResumeId { get; set; }

        [Range(0, 100)]
        public int Score { get; set; }

        public string Suggestions { get; set; }

        public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;

        [Range(0, 10)]
        public int LayoutScore { get; set; } // Bố cục & trình bày

        [Range(0, 10)]
        public int SkillScore { get; set; } // Kỹ năng

        [Range(0, 10)]
        public int ExperienceScore { get; set; } // Kinh nghiệm/Project

        [Range(0, 10)]
        public int EducationScore { get; set; } // Học vấn

        [Range(0, 10)]
        public int KeywordScore { get; set; } // Từ khóa phù hợp

        [Range(0, 10)]
        public int FormatScore { get; set; } // Hình thức/định dạng

        // Navigation property
        [ForeignKey("ResumeId")]
        public virtual Resume Resume { get; set; }
    }
} 