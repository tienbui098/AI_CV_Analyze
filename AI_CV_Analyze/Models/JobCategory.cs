using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AI_CV_Analyze.Models
{
    public class JobCategory
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(255)]
        public string Description { get; set; }

        // Navigation property
        public virtual ICollection<JobCategoryRecommendation> JobCategoryRecommendations { get; set; }
    }
} 