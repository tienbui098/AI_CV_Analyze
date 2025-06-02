using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AI_CV_Analyze.Models
{
    public class JobCategoryRecommendation
    {
        [Key]
        public int RecommendationId { get; set; }

        [Required]
        public int ResumeId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Range(0, 100)]
        public decimal RelevanceScore { get; set; }

        // Navigation properties
        [ForeignKey("ResumeId")]
        public virtual Resume Resume { get; set; }

        [ForeignKey("CategoryId")]
        public virtual JobCategory JobCategory { get; set; }
    }
} 