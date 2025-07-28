using AI_CV_Analyze.Models;
using System.IO;
using System.Threading.Tasks;

namespace AI_CV_Analyze.Services.Interfaces
{
    public interface IDocumentAnalysisService
    {
        Task<ResumeAnalysisResult> AnalyzeDocumentAsync(Stream documentStream);
    }
} 