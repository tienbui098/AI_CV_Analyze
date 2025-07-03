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
    }
} 