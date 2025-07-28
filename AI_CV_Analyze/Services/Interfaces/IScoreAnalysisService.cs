using AI_CV_Analyze.Models.DTOs;
using System.Threading.Tasks;

namespace AI_CV_Analyze.Services.Interfaces
{
    public interface IScoreAnalysisService
    {
        Task<ScoreAnalysisDto> AnalyzeScoreAsync(string cvContent);
    }
} 