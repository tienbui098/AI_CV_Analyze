//using AI_CV_Analyze.Models;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace AI_CV_Analyze.Repositories.Interfaces
//{
//    public interface IResumeAnalysisResultRepository
//    {
//        Task<ResumeAnalysisResult> GetByIdAsync(int id);
//        Task<IEnumerable<ResumeAnalysisResult>> GetAllAsync();
//        Task<ResumeAnalysisResult> CreateAsync(ResumeAnalysisResult analysisResult);
//        Task<ResumeAnalysisResult> UpdateAsync(ResumeAnalysisResult analysisResult);
//        Task DeleteAsync(int id);
//        Task<IEnumerable<ResumeAnalysisResult>> GetByResumeIdAsync(int resumeId);
//    }
//} 

using AI_CV_Analyze.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AI_CV_Analyze.Repositories.Interfaces
{
    public interface IResumeAnalysisResultRepository
    {
        Task<ResumeAnalysisResult> GetByIdAsync(int id);
        Task<IEnumerable<ResumeAnalysisResult>> GetAllAsync();
        Task<ResumeAnalysisResult> CreateAsync(ResumeAnalysisResult analysisResult);
        Task<ResumeAnalysisResult> UpdateAsync(ResumeAnalysisResult analysisResult);
        Task DeleteAsync(int id);
        Task<IEnumerable<ResumeAnalysisResult>> GetByResumeIdAsync(int resumeId);
        Task<IEnumerable<ResumeAnalysisResult>> SearchAsync(string? searchTerm, DateTime? startDate, DateTime? endDate);
    }
}