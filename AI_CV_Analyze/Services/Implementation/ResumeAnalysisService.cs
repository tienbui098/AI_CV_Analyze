using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using AI_CV_Analyze.Models;
using AI_CV_Analyze.Models.DTOs;
using AI_CV_Analyze.Repositories.Interfaces;
using AI_CV_Analyze.Services.Interfaces;
using System.IO;
using System;
using System.Linq;

namespace AI_CV_Analyze.Services
{
    public class ResumeAnalysisService : IResumeAnalysisService
    {
        private readonly IResumeRepository _resumeRepository;
        private readonly IResumeAnalysisResultRepository _analysisResultRepository;
        private readonly IFileValidationService _fileValidationService;
        private readonly IPdfProcessingService _pdfProcessingService;
        private readonly IDocumentAnalysisService _documentAnalysisService;
        private readonly IScoreAnalysisService _scoreAnalysisService;
        private readonly ICVEditService _cvEditService;
        private readonly IJobRecommendationService _jobRecommendationService;


        public ResumeAnalysisService(
            IResumeRepository resumeRepository,
            IResumeAnalysisResultRepository analysisResultRepository,
            IFileValidationService fileValidationService,
            IPdfProcessingService pdfProcessingService,
            IDocumentAnalysisService documentAnalysisService,
            IScoreAnalysisService scoreAnalysisService,
            ICVEditService cvEditService,
            IJobRecommendationService jobRecommendationService)
        {
            _resumeRepository = resumeRepository;
            _analysisResultRepository = analysisResultRepository;
            _fileValidationService = fileValidationService;
            _pdfProcessingService = pdfProcessingService;
            _documentAnalysisService = documentAnalysisService;
            _scoreAnalysisService = scoreAnalysisService;
            _cvEditService = cvEditService;
            _jobRecommendationService = jobRecommendationService;
        }



        public async Task<(int Layout, int Skill, int Experience, int Education, int Keyword, int Format, string RawJson)> AnalyzeScoreWithOpenAI(string cvContent)
        {
            var scoreResult = await _scoreAnalysisService.AnalyzeScoreAsync(cvContent);
            return (scoreResult.Layout, scoreResult.Skill, scoreResult.Experience, scoreResult.Education, scoreResult.Keyword, scoreResult.Format, scoreResult.RawJson);
        }

        public async Task<ResumeAnalysisResult> AnalyzeResume(IFormFile cvFile, int userId, bool skipDb = false)
        {
            if (cvFile == null || cvFile.Length == 0)
                throw new ArgumentException("CV file is null or empty.", nameof(cvFile));

            if (!_fileValidationService.IsValidFileType(cvFile))
            {
                var supportedTypes = new[] { "application/pdf", "image/png", "image/jpeg" };
                throw new ArgumentException($"Unsupported file type. Supported types are: {string.Join(", ", supportedTypes)}", nameof(cvFile));
            }

            // Get file information
            string fileName = cvFile.FileName ?? "unknown_file";
            string fileType = _fileValidationService.GetFileType(cvFile);
            byte[] fileBytes = _fileValidationService.GetFileBytes(cvFile);

            Resume resume = null;
            int resumeId = 0;
            if (!skipDb)
            {
                // Create Resume entity
                resume = new Resume
                {
                    UserId = userId,
                    FileName = fileName,
                    FileType = fileType,
                    FileData = fileBytes,
                    UploadDate = DateTime.UtcNow,
                    Version = 1,
                    AnalysisStatus = "Pending"
                };
                resume = await _resumeRepository.CreateAsync(resume);
                resumeId = resume.ResumeId;
            }

            Stream processedStream;
            try
            {
                if (cvFile.ContentType == "application/pdf")
                {
                    using var originalStream = new MemoryStream(fileBytes);
                    processedStream = await _pdfProcessingService.ConvertMultiPagePdfToSingleImageAsync(originalStream);
                }
                else
                {
                    processedStream = new MemoryStream(fileBytes);
                }
            }
            catch (Exception ex)
            {
                var errorResult = new ResumeAnalysisResult
                {
                    AnalysisDate = DateTime.UtcNow,
                    FileName = fileName,
                    AnalysisStatus = "Failed",
                    ErrorMessage = $"Error processing file: {ex.Message}",
                    ResumeId = resumeId
                };
                
                // Lưu error result vào database nếu không skip
                if (!skipDb && resumeId > 0)
                {
                    try
                    {
                        errorResult = await _analysisResultRepository.CreateAsync(errorResult);
                    }
                    catch (Exception dbEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error saving error result: {dbEx.Message}");
                    }
                }
                
                return errorResult;
            }

            var analysisResult = new ResumeAnalysisResult
            {
                AnalysisDate = DateTime.UtcNow,
                FileName = fileName,
                AnalysisStatus = "Pending",
                ResumeId = resumeId
            };

            try
            {
                analysisResult = await _documentAnalysisService.AnalyzeDocumentAsync(processedStream);
                analysisResult.FileName = fileName;
                analysisResult.ResumeId = resumeId;
            }
            catch (Exception ex)
            {
                analysisResult.AnalysisStatus = "Failed";
                analysisResult.ErrorMessage = $"Analysis error: {ex.Message}";
            }

            // Lưu vào database nếu không skip
            if (!skipDb && resumeId > 0)
            {
                try
                {
                    analysisResult = await _analysisResultRepository.CreateAsync(analysisResult);
                }
                catch (Exception ex)
                {
                    // Log error nhưng không throw để không ảnh hưởng đến flow chính
                    System.Diagnostics.Debug.WriteLine($"Error saving ResumeAnalysisResult: {ex.Message}");
                }
            }

            return analysisResult;
        }


        public async Task<JobSuggestionResult> GetJobSuggestionsAsync(string skills, string workExperience)
        {
            return await _jobRecommendationService.GetJobSuggestionsAsync(skills, workExperience);
        }

        public async Task<string> GetCVEditSuggestions(string cvContent)
        {
            return await _cvEditService.GetCVEditSuggestionsAsync(cvContent);
        }

        public async Task<string> GenerateFinalCV(string cvContent, string suggestions)
        {
            return await _cvEditService.GenerateFinalCVAsync(cvContent, suggestions);
        }



    }
}