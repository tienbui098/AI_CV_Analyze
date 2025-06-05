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
                return View("AnalysisResult", result);
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