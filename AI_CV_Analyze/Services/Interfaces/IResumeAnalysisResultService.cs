//using AI_CV_Analyze.Models;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace AI_CV_Analyze.Services.Interfaces
//{
//    public interface IResumeAnalysisResultService
//    {
//        Task<ResumeAnalysisResult> GetAnalysisResultByIdAsync(int id);
//        Task<IEnumerable<ResumeAnalysisResult>> GetAllAnalysisResultsAsync();
//        Task<ResumeAnalysisResult> CreateAnalysisResultAsync(ResumeAnalysisResult analysisResult);
//        Task<ResumeAnalysisResult> UpdateAnalysisResultAsync(ResumeAnalysisResult analysisResult);
//        Task DeleteAnalysisResultAsync(int id);
//        Task<IEnumerable<ResumeAnalysisResult>> GetAnalysisResultsByResumeIdAsync(int resumeId);

//        // Additional business logic methods
//        Task<ResumeAnalysisResult> AnalyzeResumeAsync(int resumeId);
//        Task<Dictionary<string, object>> GetAnalysisSummaryAsync(int analysisResultId);
//    }
//} 

using AI_CV_Analyze.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AI_CV_Analyze.Services.Interfaces
{
    public interface IResumeAnalysisResultService
    {
        Task<ResumeAnalysisResult> GetAnalysisResultByIdAsync(int id);
        Task<IEnumerable<ResumeAnalysisResult>> GetAllAnalysisResultsAsync();
        Task<ResumeAnalysisResult> CreateAnalysisResultAsync(ResumeAnalysisResult analysisResult);
        Task<ResumeAnalysisResult> UpdateAnalysisResultAsync(ResumeAnalysisResult analysisResult);
        Task DeleteAnalysisResultAsync(int id);
        Task<IEnumerable<ResumeAnalysisResult>> GetAnalysisResultsByResumeIdAsync(int resumeId);
        Task<ResumeAnalysisResult> AnalyzeResumeAsync(int resumeId);
        Task<Dictionary<string, object>> GetAnalysisSummaryAsync(int analysisResultId);
        Task<IEnumerable<ResumeAnalysisResult>> SearchAnalysisResultsAsync(string? searchTerm, DateTime? startDate, DateTime? endDate);
        Task<JobSuggestionResult> GetJobSuggestionsForAnalysisAsync(int analysisResultId);
    }
}