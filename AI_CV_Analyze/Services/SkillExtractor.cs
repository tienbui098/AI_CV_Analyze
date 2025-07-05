using System;
using System.Linq;

namespace AI_CV_Analyze.Services
{
    public static class SkillExtractor
    {
        public static string ExtractSkillsFromText(string rawText)
        {
            var knownSkills = new[]
            {
                "accounting", "adobe xd", "asp.net", "bookkeeping", "c#", "clinical pharmacy", "communication",
                "compounding", "content writing", "copywriting", "crm", "css", "customer service", "data analysis",
                "data visualization", "deep learning", "drug design", "email marketing", "excel", "figma", "finance",
                "frontend", "git", "google analytics", "hibernate", "html", "illustrator", "java", "javascript", "machine learning"
                // Add more skills here as needed
            };

            var text = rawText?.ToLowerInvariant() ?? "";
            var matched = knownSkills
                .Where(skill => text.Contains(skill))
                .Distinct();

            return string.Join(", ", matched);
        }
    }
} 