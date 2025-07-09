//using AI_CV_Analyze.Models;
//using AI_CV_Analyze.Repositories.Interfaces;
//using AI_CV_Analyze.Services.Interfaces;
//using System;
//using System.Collections.Generic;
//using System.Text.Json;
//using System.Threading.Tasks;

//namespace AI_CV_Analyze.Services.Implementation
//{
//    public class ResumeAnalysisResultService : IResumeAnalysisResultService
//    {
//        private readonly IResumeAnalysisResultRepository _repository;

//        public ResumeAnalysisResultService(IResumeAnalysisResultRepository repository)
//        {
//            _repository = repository;
//        }

//        public async Task<ResumeAnalysisResult> GetAnalysisResultByIdAsync(int id)
//        {
//            return await _repository.GetByIdAsync(id);
//        }

//        public async Task<IEnumerable<ResumeAnalysisResult>> GetAllAnalysisResultsAsync()
//        {
//            return await _repository.GetAllAsync();
//        }

//        public async Task<ResumeAnalysisResult> CreateAnalysisResultAsync(ResumeAnalysisResult analysisResult)
//        {
//            analysisResult.AnalysisDate = DateTime.UtcNow;
//            return await _repository.CreateAsync(analysisResult);
//        }

//        public async Task<ResumeAnalysisResult> UpdateAnalysisResultAsync(ResumeAnalysisResult analysisResult)
//        {
//            return await _repository.UpdateAsync(analysisResult);
//        }

//        public async Task DeleteAnalysisResultAsync(int id)
//        {
//            await _repository.DeleteAsync(id);
//        }

//        public async Task<IEnumerable<ResumeAnalysisResult>> GetAnalysisResultsByResumeIdAsync(int resumeId)
//        {
//            return await _repository.GetByResumeIdAsync(resumeId);
//        }

//        public async Task<ResumeAnalysisResult> AnalyzeResumeAsync(int resumeId)
//        {
//            // TODO: Implement the actual resume analysis logic here
//            // This would typically involve:
//            // 1. Getting the resume data
//            // 2. Processing it through various AI services
//            // 3. Creating and saving the analysis result
//            throw new NotImplementedException("Resume analysis logic needs to be implemented");
//        }

//        public async Task<Dictionary<string, object>> GetAnalysisSummaryAsync(int analysisResultId)
//        {
//            var analysisResult = await _repository.GetByIdAsync(analysisResultId);
//            if (analysisResult == null)
//                throw new KeyNotFoundException($"Analysis result with ID {analysisResultId} not found");

//            var summary = new Dictionary<string, object>
//            {
//                ["AnalysisDate"] = analysisResult.AnalysisDate,
//                ["DocumentType"] = analysisResult.DocumentType,
//                ["ConfidenceScore"] = analysisResult.ConfidenceScore,
//                ["Language"] = analysisResult.Language,
//                ["SentimentScore"] = analysisResult.SentimentScore
//            };

//            // Parse JSON fields if they exist
//            if (!string.IsNullOrEmpty(analysisResult.SkillsAnalysis))
//                summary["Skills"] = JsonSerializer.Deserialize<object>(analysisResult.SkillsAnalysis);

//            if (!string.IsNullOrEmpty(analysisResult.OverallAnalysis))
//                summary["OverallAnalysis"] = JsonSerializer.Deserialize<object>(analysisResult.OverallAnalysis);

//            return summary;
//        }
//    }
//} 

using AI_CV_Analyze.Data;
using AI_CV_Analyze.Models;
using AI_CV_Analyze.Repositories.Interfaces;
using AI_CV_Analyze.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AI_CV_Analyze.Services.Implementation
{
    public class ResumeAnalysisResultService : IResumeAnalysisResultService
    {
        private readonly IResumeAnalysisResultRepository _repository;
        private readonly IResumeAnalysisService _analysisService;
        private readonly ApplicationDbContext _context;

        public ResumeAnalysisResultService(
            IResumeAnalysisResultRepository repository,
            IResumeAnalysisService analysisService,
            ApplicationDbContext context)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _analysisService = analysisService ?? throw new ArgumentNullException(nameof(analysisService));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<ResumeAnalysisResult> GetAnalysisResultByIdAsync(int id)
        {
            var result = await _repository.GetByIdAsync(id);
            return result ?? throw new KeyNotFoundException($"Analysis result with ID {id} not found.");
        }

        public async Task<IEnumerable<ResumeAnalysisResult>> GetAllAnalysisResultsAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<ResumeAnalysisResult> CreateAnalysisResultAsync(ResumeAnalysisResult analysisResult)
        {
            if (analysisResult == null)
                throw new ArgumentNullException(nameof(analysisResult));

            analysisResult.AnalysisDate = DateTime.UtcNow;
            analysisResult.AnalysisStatus = "Completed";
            return await _repository.CreateAsync(analysisResult);
        }

        public async Task<ResumeAnalysisResult> UpdateAnalysisResultAsync(ResumeAnalysisResult analysisResult)
        {
            if (analysisResult == null)
                throw new ArgumentNullException(nameof(analysisResult));

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
            try
            {
                var resume = await _context.Resumes
                    .FirstOrDefaultAsync(r => r.ResumeId == resumeId);

                if (resume == null)
                    throw new KeyNotFoundException($"Resume with ID {resumeId} not found.");

                resume.AnalysisStatus = "Processing";
                await _context.SaveChangesAsync();

                using var memoryStream = new MemoryStream(resume.FileData);
                var formFile = new FormFile(memoryStream, 0, memoryStream.Length, "cvFile", resume.FileName)
                {
                    ContentType = resume.FileType
                };

                var analysisResult = await _analysisService.AnalyzeResume(formFile);

                resume.AnalysisStatus = string.IsNullOrEmpty(analysisResult.ErrorMessage) ? "Completed" : "Failed";
                await _context.SaveChangesAsync();

                analysisResult.ResumeId = resumeId;
                return await CreateAnalysisResultAsync(analysisResult);
            }
            catch (Exception ex)
            {
                var resume = await _context.Resumes
                    .FirstOrDefaultAsync(r => r.ResumeId == resumeId);
                if (resume != null)
                {
                    resume.AnalysisStatus = "Failed";
                    await _context.SaveChangesAsync();
                }
                throw new Exception($"Failed to analyze resume with ID {resumeId}: {ex.Message}", ex);
            }
        }

        public async Task<Dictionary<string, object>> GetAnalysisSummaryAsync(int analysisResultId)
        {
            var analysisResult = await _repository.GetByIdAsync(analysisResultId);
            if (analysisResult == null)
                throw new KeyNotFoundException($"Analysis result with ID {analysisResultId} not found.");

            var summary = new Dictionary<string, object>
            {
                ["AnalysisDate"] = analysisResult.AnalysisDate,
                ["DocumentType"] = analysisResult.DocumentType ?? "Unknown",
                ["ConfidenceScore"] = analysisResult.ConfidenceScore ?? "0.0",
                ["Language"] = analysisResult.Language ?? "Unknown",
                ["SentimentScore"] = analysisResult.SentimentScore ?? "0.0",
                ["Name"] = analysisResult.Name ?? "Not extracted",
                ["Education"] = analysisResult.Education ?? "Not extracted",
                ["Skills"] = analysisResult.Skills ?? "Not extracted",
                ["Experience"] = analysisResult.Experience ?? "Not extracted",
                ["AnalysisStatus"] = analysisResult.AnalysisStatus ?? "Unknown"
            };

            try
            {
                if (!string.IsNullOrEmpty(analysisResult.SkillsAnalysis) && IsValidJson(analysisResult.SkillsAnalysis))
                    summary["SkillsAnalysis"] = JsonSerializer.Deserialize<object>(analysisResult.SkillsAnalysis);
                else
                    summary["SkillsAnalysis"] = analysisResult.SkillsAnalysis ?? "Not available";

                if (!string.IsNullOrEmpty(analysisResult.OverallAnalysis) && IsValidJson(analysisResult.OverallAnalysis))
                    summary["OverallAnalysis"] = JsonSerializer.Deserialize<object>(analysisResult.OverallAnalysis);
                else
                    summary["OverallAnalysis"] = analysisResult.OverallAnalysis ?? "Not available";

                if (!string.IsNullOrEmpty(analysisResult.FormFields) && IsValidJson(analysisResult.FormFields))
                    summary["FormFields"] = JsonSerializer.Deserialize<object>(analysisResult.FormFields);
                else
                    summary["FormFields"] = analysisResult.FormFields ?? "Not available";

                if (!string.IsNullOrEmpty(analysisResult.KeyPhrases) && IsValidJson(analysisResult.KeyPhrases))
                    summary["KeyPhrases"] = JsonSerializer.Deserialize<object>(analysisResult.KeyPhrases);
                else
                    summary["KeyPhrases"] = analysisResult.KeyPhrases ?? "Not available";

                if (!string.IsNullOrEmpty(analysisResult.Entities) && IsValidJson(analysisResult.Entities))
                    summary["Entities"] = JsonSerializer.Deserialize<object>(analysisResult.Entities);
                else
                    summary["Entities"] = analysisResult.Entities ?? "Not available";
            }
            catch (JsonException ex)
            {
                summary["Error"] = $"Failed to parse JSON fields: {ex.Message}";
            }

            return summary;
        }

        public async Task<IEnumerable<ResumeAnalysisResult>> SearchAnalysisResultsAsync(string? searchTerm, DateTime? startDate, DateTime? endDate)
        {
            return await _repository.SearchAsync(searchTerm, startDate, endDate);
        }

        public async Task<JobSuggestionResult> GetJobSuggestionsForAnalysisAsync(int analysisResultId)
        {
            var analysisResult = await _repository.GetByIdAsync(analysisResultId);
            if (analysisResult == null)
                throw new KeyNotFoundException($"Analysis result with ID {analysisResultId} not found.");

            return await _analysisService.GetJobSuggestionsAsync(analysisResult.Skills);
        }

        private bool IsValidJson(string? strInput)
        {
            if (string.IsNullOrWhiteSpace(strInput)) return false;
            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) ||
                (strInput.StartsWith("[") && strInput.EndsWith("]")))
            {
                try
                {
                    JsonDocument.Parse(strInput);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }
    }
}
