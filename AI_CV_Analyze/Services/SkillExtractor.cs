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
                 "social media", "email marketing", "content writing", "seo", "copywriting",
                "excel", "python", "power bi", "sql", "data visualization", "pandas",
                "data analysis", "quickbooks", "finance", "tax", "accounting",
                "bookkeeping", "payroll", "statistics", "problem solving",
                "customer service", "crm", "communication", "clinical pharmacy",
                "prescription", "compounding", "medication", "pharmacy",
                "user research", "prototyping", "adobe xd", "figma", "wireframing",
                "css", "illustrator", "nlp", "tensorflow", "pytorch",
                "deep learning", "model training", "scikit-learn", "machine learning",
                "next.js", "typescript", "frontend", "tailwind", "spring boot",
                "mysql", "hibernate", "restful api", "java", "postgresql",
                "web development", "react", "redux", "html", "c#",
                "rest api", "sql server", "asp.net", "git", "mvc",
                "javascript", "drug design"


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