using AI_CV_Analyze.Models;
using System.Threading.Tasks;

namespace AI_CV_Analyze.Services.Interfaces
{
    public interface IJobRecommendationService
    {
        Task<JobSuggestionResult> GetJobSuggestionsAsync(string skills, string workExperience);
    }
} 