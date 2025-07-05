using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using AI_CV_Analyze.Models;
using AI_CV_Analyze.Services;
using System;

namespace AI_CV_Analyze.Controllers
{
    public class CVAnalysisController : Controller
    {
        private readonly IResumeAnalysisService _resumeAnalysisService;

        public CVAnalysisController(IResumeAnalysisService resumeAnalysisService)
        {
            _resumeAnalysisService = resumeAnalysisService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AnalyzeCV(IFormFile cvFile)
        {
            if (cvFile == null || cvFile.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            try
            {
                var result = await _resumeAnalysisService.AnalyzeResume(cvFile);
                // Gửi nội dung CV cho OpenAI để lấy đề xuất chỉnh sửa
                var suggestions = await _resumeAnalysisService.GetCVEditSuggestions(result.Content);
                ViewBag.Suggestions = suggestions;
                
                // Get job recommendations based on CV skills section
                try
                {
                    // Use the extracted skills section instead of full content
                    var skillsContent = !string.IsNullOrEmpty(result.Skills) ? result.Skills : result.Content;
                    var jobRecommendations = await _resumeAnalysisService.GetJobSuggestionsAsync(skillsContent);
                    ViewBag.JobRecommendations = jobRecommendations.Suggestions;
                }
                catch (Exception jobEx)
                {
                    // Log the error but don't fail the entire analysis
                    ViewBag.JobRecommendationsError = jobEx.Message;
                }
                
                return View("AnalysisResult", result);
            }
            catch (Exception ex)
            {
                return View("Error", new ErrorViewModel { Message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetEditSuggestions(string Content)
        {
            if (string.IsNullOrEmpty(Content))
            {
                return BadRequest("Không có nội dung CV để đề xuất chỉnh sửa.");
            }
            try
            {
                var suggestions = await _resumeAnalysisService.GetCVEditSuggestions(Content);
                ViewBag.Suggestions = suggestions;
                ViewBag.CVContent = Content;
                return View("EditSuggestions");
            }
            catch (Exception ex)
            {
                return View("Error", new ErrorViewModel { Message = ex.Message });
            }
        }

        public IActionResult AnalysisResult(ResumeAnalysisResult result)
        {
            return View(result);
        }

        // New method to get job recommendations from CV content via AJAX
        [HttpPost]
        public async Task<IActionResult> GetJobRecommendationsFromCV(string cvContent, string skillsContent = null)
        {
            if (string.IsNullOrWhiteSpace(cvContent))
            {
                return BadRequest("No CV content provided");
            }

            try
            {
                // Use skills section if available, otherwise use full content
                var contentToAnalyze = !string.IsNullOrEmpty(skillsContent) ? skillsContent : cvContent;
                var result = await _resumeAnalysisService.GetJobSuggestionsAsync(contentToAnalyze);
                return Json(new { success = true, suggestions = result.Suggestions });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // Job prediction methods
        [HttpGet]
        public IActionResult JobPrediction()
        {
            ViewBag.ErrorMessage = null;
            return View(new JobSuggestionRequest());
        }

        [HttpPost]
        public async Task<IActionResult> JobPrediction(JobSuggestionRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.Skills))
            {
                ViewBag.ErrorMessage = "Please enter your skills.";
                return View(model);
            }

            try
            {
                var result = await _resumeAnalysisService.GetJobSuggestionsAsync(model.Skills);
                ViewBag.Results = result.Suggestions;
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error getting job suggestions: {ex.Message}";
                return View(model);
            }
        }
    }
}