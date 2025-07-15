using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using AI_CV_Analyze.Models;
using AI_CV_Analyze.Services;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq; // Added for .Select()

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
                // Lưu kết quả phân tích vào Session
                HttpContext.Session.SetString("ResumeAnalysisResult", JsonConvert.SerializeObject(result));
                HttpContext.Session.SetString("CVContent", result.Content);
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
                // Lưu đề xuất chỉnh sửa vào Session
                HttpContext.Session.SetString("EditSuggestions", JsonConvert.SerializeObject(suggestions));
                HttpContext.Session.SetString("EditSuggestionsContent", Content);
                return RedirectToAction("EditSuggestions");
            }
            catch (Exception ex)
            {
                return View("Error", new ErrorViewModel { Message = ex.Message });
            }
        }

        public IActionResult AnalysisResult(ResumeAnalysisResult result)
        {
            // Nếu không có model truyền vào, lấy từ Session
            if (result == null || string.IsNullOrEmpty(result.Content))
            {
                var resultJson = HttpContext.Session.GetString("ResumeAnalysisResult");
                if (!string.IsNullOrEmpty(resultJson))
                {
                    result = JsonConvert.DeserializeObject<ResumeAnalysisResult>(resultJson);
                }
            }
            // Lấy lại gợi ý công việc nếu có
            var jobRecsJson = HttpContext.Session.GetString("JobRecommendations");
            if (!string.IsNullOrEmpty(jobRecsJson))
            {
                ViewBag.JobRecommendations = JsonConvert.DeserializeObject<List<JobRecommendationDto>>(jobRecsJson);
            }
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
            var skills = HttpContext.Session.GetString("JobSuggestionSkills");
            var resultsJson = HttpContext.Session.GetString("JobSuggestionResult");
            var model = new JobSuggestionRequest { Skills = skills };
            if (!string.IsNullOrEmpty(resultsJson))
            {
                var results = JsonConvert.DeserializeObject<Dictionary<string, double>>(resultsJson);
                ViewBag.Results = results;
            }
            return View(model);
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
                // Lưu kết quả gợi ý công việc vào Session
                HttpContext.Session.SetString("JobSuggestionResult", JsonConvert.SerializeObject(result.Suggestions));
                HttpContext.Session.SetString("JobSuggestionSkills", model.Skills);
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error getting job suggestions: {ex.Message}";
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> RecommendJobs([FromBody] SkillsRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Skills))
            {
                return BadRequest("No skills provided");
            }
            try
            {
                var result = await _resumeAnalysisService.GetJobSuggestionsAsync(request.Skills);
                // Map to a simple array of { title, description }
                var jobs = (result.Suggestions ?? new Dictionary<string, double>())
                    .Select(s => new {
                        title = s.Key,
                        score = s.Value
                    }).ToList();
                // Save to session
                HttpContext.Session.SetString("JobRecommendations", Newtonsoft.Json.JsonConvert.SerializeObject(jobs));
                return Json(jobs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        public class SkillsRequest
        {
            public string Skills { get; set; }
        }

        [HttpGet]
        public IActionResult EditSuggestions()
        {
            var suggestionsJson = HttpContext.Session.GetString("EditSuggestions");
            var content = HttpContext.Session.GetString("EditSuggestionsContent");
            if (!string.IsNullOrEmpty(suggestionsJson))
            {
                ViewBag.Suggestions = JsonConvert.DeserializeObject<string>(suggestionsJson);
                ViewBag.CVContent = content;
            }
            else
            {
                ViewBag.Suggestions = null;
                ViewBag.CVContent = null;
                ViewBag.ErrorMessage = "Bạn chưa dùng chức năng đề xuất chỉnh sửa CV.";
            }
            return View();
        }

        [HttpGet]
        public IActionResult HasEditSuggestions()
        {
            var suggestionsJson = HttpContext.Session.GetString("EditSuggestions");
            bool hasSuggestions = !string.IsNullOrEmpty(suggestionsJson);
            return Json(new { hasSuggestions });
        }

        public class JobRecommendationDto
        {
            public string title { get; set; }
            public double score { get; set; }
        }
    }
}