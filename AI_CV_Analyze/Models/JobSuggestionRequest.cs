namespace AI_CV_Analyze.Models
{
    public class JobSuggestionRequest
    {
        public string Skills { get; set; }
        public string JobCategory { get; set; } = "Unknown";
        public string WorkExperience { get; set; } // New: work/project experience
    }
} 