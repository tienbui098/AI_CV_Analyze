using System.Collections.Generic;

namespace AI_CV_Analyze.Models
{
    public class JobSuggestionResult
    {
        public Dictionary<string, double> Suggestions { get; set; } = new();
    }
} 