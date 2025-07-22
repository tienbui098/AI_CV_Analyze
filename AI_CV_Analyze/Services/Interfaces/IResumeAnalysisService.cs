using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using AI_CV_Analyze.Models;

namespace AI_CV_Analyze.Services
{
    public interface IResumeAnalysisService
    {
        Task<ResumeAnalysisResult> AnalyzeResume(IFormFile cvFile, int userId, bool skipDb = false);
        Task<string> GetCVEditSuggestions(string cvContent);
        Task<JobSuggestionResult> GetJobSuggestionsAsync(string skills);
        Task<(int Layout, int Skill, int Experience, int Education, int Keyword, int Format, string RawJson)> AnalyzeScoreWithOpenAI(string cvContent);
        Task<string> GenerateFinalCV(string cvContent, string suggestions);
    }
}
