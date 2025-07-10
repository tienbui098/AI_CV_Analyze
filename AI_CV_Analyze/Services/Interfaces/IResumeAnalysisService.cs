using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using AI_CV_Analyze.Models;

namespace AI_CV_Analyze.Services
{
    public interface IResumeAnalysisService
    {
        Task<ResumeAnalysisResult> AnalyzeResume(IFormFile cvFile);
        Task<string> GetCVEditSuggestions(string cvContent);
        Task<JobSuggestionResult> GetJobSuggestionsAsync(string skills);
    }
}
