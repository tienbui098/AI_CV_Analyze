using System.Threading.Tasks;

namespace AI_CV_Analyze.Services.Interfaces
{
    public interface ICVEditService
    {
        Task<string> GetCVEditSuggestionsAsync(string cvContent);
        Task<string> GenerateFinalCVAsync(string cvContent, string suggestions);
    }
} 