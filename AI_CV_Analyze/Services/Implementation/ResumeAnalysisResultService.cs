using AI_CV_Analyze.Models;
using AI_CV_Analyze.Repositories.Interfaces;
using AI_CV_Analyze.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace AI_CV_Analyze.Services.Implementation
{
    public class ResumeAnalysisResultService : IResumeAnalysisResultService
    {
        private readonly IResumeAnalysisResultRepository _repository;

        public ResumeAnalysisResultService(IResumeAnalysisResultRepository repository)
        {
            _repository = repository;
        }

        public async Task<ResumeAnalysisResult> GetAnalysisResultByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<ResumeAnalysisResult>> GetAllAnalysisResultsAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<ResumeAnalysisResult> CreateAnalysisResultAsync(ResumeAnalysisResult analysisResult)
        {
            analysisResult.AnalysisDate = DateTime.UtcNow;
            return await _repository.CreateAsync(analysisResult);
        }

        public async Task<ResumeAnalysisResult> UpdateAnalysisResultAsync(ResumeAnalysisResult analysisResult)
        {
            return await _repository.UpdateAsync(analysisResult);
        }

        public async Task DeleteAnalysisResultAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }

        public async Task<IEnumerable<ResumeAnalysisResult>> GetAnalysisResultsByResumeIdAsync(int resumeId)
        {
            return await _repository.GetByResumeIdAsync(resumeId);
        }

        public async Task<ResumeAnalysisResult> AnalyzeResumeAsync(int resumeId)
        {
            // TODO: Implement the actual resume analysis logic here
            // This would typically involve:
            // 1. Getting the resume data
            // 2. Processing it through various AI services
            // 3. Creating and saving the analysis result
            throw new NotImplementedException("Resume analysis logic needs to be implemented");
        }

        public async Task<Dictionary<string, object>> GetAnalysisSummaryAsync(int analysisResultId)
        {
            var analysisResult = await _repository.GetByIdAsync(analysisResultId);
            if (analysisResult == null)
                throw new KeyNotFoundException($"Analysis result with ID {analysisResultId} not found");

            var summary = new Dictionary<string, object>
            {
                ["AnalysisDate"] = analysisResult.AnalysisDate,
                ["DocumentType"] = analysisResult.DocumentType,
                ["ConfidenceScore"] = analysisResult.ConfidenceScore,
                ["Language"] = analysisResult.Language,
                ["SentimentScore"] = analysisResult.SentimentScore
            };

            // Parse JSON fields if they exist
            if (!string.IsNullOrEmpty(analysisResult.SkillsAnalysis))
                summary["Skills"] = JsonSerializer.Deserialize<object>(analysisResult.SkillsAnalysis);

            if (!string.IsNullOrEmpty(analysisResult.OverallAnalysis))
                summary["OverallAnalysis"] = JsonSerializer.Deserialize<object>(analysisResult.OverallAnalysis);

            return summary;
        }
    }
} 