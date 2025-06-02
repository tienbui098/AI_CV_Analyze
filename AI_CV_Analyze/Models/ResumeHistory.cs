using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AI_CV_Analyze.Models
{
    public class ResumeHistory
    {
        [Key]
        public int HistoryId { get; set; }

        [Required]
        public int ResumeId { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; }

        [Required]
        public byte[] FileData { get; set; }

        [Required]
        public int Version { get; set; }

        [Range(0, 100)]
        public int Score { get; set; }

        public DateTime UploadDate { get; set; }

        // Navigation property
        [ForeignKey("ResumeId")]
        public virtual Resume Resume { get; set; }
    }
} 