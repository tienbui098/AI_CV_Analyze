using System.Collections.Generic;

namespace AI_CV_Analyze.Models
{
    public class JobSuggestionResult
    {
        // For backward compatibility (old ML output)
        public Dictionary<string, double> Suggestions { get; set; } = new();

        // New OpenAI-based output
        public string RecommendedJob { get; set; } = string.Empty;
        public int MatchPercentage { get; set; }
        public List<string> MatchedSkills { get; set; } = new();
        public List<string> MissingSkills { get; set; } = new();
        public string ImprovementPlan { get; set; } = string.Empty;
    }
} 