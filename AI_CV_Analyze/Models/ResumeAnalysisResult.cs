using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AI_CV_Analyze.Models
{
    public class ResumeAnalysisResult
    {
        [Key]
        public int AnalysisResultId { get; set; }

        [Required]
        public int ResumeId { get; set; }

        // Basic Information
        public string FileName { get; set; }
        public string Content { get; set; }
        public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
        public string AnalysisStatus { get; set; }
        public string ErrorMessage { get; set; }

        // Kết quả phân tích từ Computer Vision
        public string DetectedText { get; set; } // Văn bản được phát hiện từ hình ảnh
        public string ImageAnalysis { get; set; } // Kết quả phân tích hình ảnh dạng JSON

        // Kết quả phân tích từ Form Recognizer
        public string FormFields { get; set; } // Các trường form được trích xuất dạng JSON
        public string DocumentType { get; set; } // Loại tài liệu (CV, Resume, etc.)
        public string ConfidenceScore { get; set; } // Điểm tin cậy của việc nhận dạng

        // Kết quả phân tích từ Text Analytics
        public string KeyPhrases { get; set; } // Các cụm từ khóa quan trọng dạng JSON
        public string Entities { get; set; } // Các thực thể được nhận dạng dạng JSON
        public string SentimentScore { get; set; } // Điểm đánh giá cảm xúc
        public string Language { get; set; } // Ngôn ngữ của văn bản

        // Kết quả phân tích từ OpenAI
        public string Skills { get; set; } // Phân tích kỹ năng
        public string Name { get; set; } // Tên người dùng
        public string Email { get; set; } // Email người dùng
        public string SkillsAnalysis { get; set; } // Phân tích kỹ năng dạng JSON
        public string Experience { get; set; } // Phân tích kinh nghiệm
        public string Project { get; set; } // Phân tích dự án
        public string Education { get; set; } // Phân tích học vấn
        public string OverallAnalysis { get; set; } // Phân tích tổng thể dạng JSON

        // Liên kết với bảng Resume
        [ForeignKey("ResumeId")]
        public virtual Resume Resume { get; set; }
    }
}
