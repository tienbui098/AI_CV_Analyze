using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AI_CV_Analyze.Models
{
    public class ResumeData
    {
        [Key]
        public int ResumeDataId { get; set; }

        [Required]
        public int ResumeId { get; set; }

        public string ExtractedData { get; set; }

        [StringLength(50)]
        public string Language { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        // Navigation property
        [ForeignKey("ResumeId")]
        public virtual Resume Resume { get; set; }
    }
} 