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

        // Navigation property
        [ForeignKey("ResumeId")]
        public virtual Resume Resume { get; set; }
    }
} 