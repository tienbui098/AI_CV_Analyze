using System;

namespace AI_CV_Analyze.Models.DTOs
{
    public class ResumeAnalysisDto
    {
        public int AnalysisResultId { get; set; }
        public int ResumeId { get; set; }
        public string FileName { get; set; }
        public string Content { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Skills { get; set; }
        public string Experience { get; set; }
        public string Project { get; set; }
        public string Education { get; set; }
        public string LanguageProficiency { get; set; }
        public string Certificate { get; set; }
        public string Achievement { get; set; }
        public string AnalysisStatus { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime AnalysisDate { get; set; }
    }
} 