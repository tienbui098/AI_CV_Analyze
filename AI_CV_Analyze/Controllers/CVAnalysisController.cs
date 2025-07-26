using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using AI_CV_Analyze.Models;
using AI_CV_Analyze.Services;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq; // Added for .Select()
using AI_CV_Analyze.Data;
using Microsoft.EntityFrameworkCore;
using DinkToPdf;
using DinkToPdf.Contracts;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AI_CV_Analyze.Controllers
{
    public class CVAnalysisController : Controller
    {
        private readonly IResumeAnalysisService _resumeAnalysisService;
        private readonly ApplicationDbContext _dbContext;
        private readonly IConverter _pdfConverter;

        public CVAnalysisController(IResumeAnalysisService resumeAnalysisService, ApplicationDbContext dbContext, IConverter pdfConverter)
        {
            _resumeAnalysisService = resumeAnalysisService;
            _dbContext = dbContext;
            _pdfConverter = pdfConverter;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AnalyzeCV(IFormFile cvFile)
        {
            // Xóa dữ liệu session liên quan đến CV cũ
            HttpContext.Session.Remove("ResumeAnalysisResult");
            HttpContext.Session.Remove("CVContent");
            HttpContext.Session.Remove("EditSuggestionsContent");
            HttpContext.Session.Remove("CVScoreResult");
            HttpContext.Session.Remove("JobRecommendations");
            HttpContext.Session.Remove("JobSuggestionResult");
            HttpContext.Session.Remove("JobSuggestionSkills");
            HttpContext.Session.Remove("FinalCVContent");

            if (cvFile == null || cvFile.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            // Lấy UserId từ claim (nếu có)
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            int? userId = null;
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int uid))
            {
                userId = uid;
            }

            try
            {
                ResumeAnalysisResult result;
                if (userId.HasValue)
                {
                    // Đăng nhập: lưu vào DB
                    result = await _resumeAnalysisService.AnalyzeResume(cvFile, userId.Value);
                }
                else
                {
                    // Không đăng nhập: chỉ phân tích, không lưu DB
                    result = await _resumeAnalysisService.AnalyzeResume(cvFile, 0, true); // skipDb = true
                }
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
                // Lưu đề xuất chỉnh sửa vào Session (nếu cần dùng lại)
                HttpContext.Session.SetString("EditSuggestions", JsonConvert.SerializeObject(suggestions));
                HttpContext.Session.SetString("EditSuggestionsContent", Content);
                // Lấy lại result từ session (hoặc DB nếu cần)
                var resultJson = HttpContext.Session.GetString("ResumeAnalysisResult");
                ResumeAnalysisResult result = null;
                if (!string.IsNullOrEmpty(resultJson))
                {
                    result = JsonConvert.DeserializeObject<ResumeAnalysisResult>(resultJson);
                }
                
                // Debug: Kiểm tra xem suggestions có được lấy không
                System.Diagnostics.Debug.WriteLine($"Suggestions: {suggestions}");
                System.Diagnostics.Debug.WriteLine($"Content: {Content}");
                
                return View("AnalysisResult", result);
            }
            catch (Exception ex)
            {
                return View("Error", new ErrorViewModel { Message = ex.Message });
            }
        }

        public IActionResult AnalysisResult(ResumeAnalysisResult result, int? resumeId = null)
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
            // Nếu vẫn chưa có result, thử lấy ResumeId từ query/session
            int rid = result?.ResumeId ?? resumeId ?? 0;
            // Lấy lại gợi ý công việc nếu có
            var jobRecsJson = HttpContext.Session.GetString("JobRecommendations");
            if (!string.IsNullOrEmpty(jobRecsJson))
            {
                var jobSuggestions = JsonConvert.DeserializeObject<Dictionary<string, double>>(jobRecsJson);
                ViewBag.JobRecommendations = jobSuggestions;
            }
            // Lấy điểm từng tiêu chí từ bảng ResumeAnalysis
            bool hasDbScore = false;
            if (rid > 0)
            {
                var analysis = _dbContext.ResumeAnalysis.AsNoTracking().FirstOrDefault(a => a.ResumeId == rid);
                if (analysis != null)
                {
                    ViewBag.LayoutScore = analysis.LayoutScore;
                    ViewBag.SkillScore = analysis.SkillScore;
                    ViewBag.ExperienceScore = analysis.ExperienceScore;
                    ViewBag.EducationScore = analysis.EducationScore;
                    ViewBag.KeywordScore = analysis.KeywordScore;
                    ViewBag.FormatScore = analysis.FormatScore;
                    ViewBag.TotalScore = analysis.Score;
                    hasDbScore = (analysis.LayoutScore > 0 || analysis.SkillScore > 0 || analysis.ExperienceScore > 0 || analysis.EducationScore > 0 || analysis.KeywordScore > 0 || analysis.FormatScore > 0);
                }
            }
            // Nếu không có điểm trong DB, thử lấy từ session (cho guest)
            if (!hasDbScore)
            {
                var scoreJson = HttpContext.Session.GetString("CVScoreResult");
                if (!string.IsNullOrEmpty(scoreJson))
                {
                    dynamic score = JsonConvert.DeserializeObject(scoreJson);
                    ViewBag.LayoutScore = (int?)score.layout ?? 0;
                    ViewBag.SkillScore = (int?)score.skill ?? 0;
                    ViewBag.ExperienceScore = (int?)score.experience ?? 0;
                    ViewBag.EducationScore = (int?)score.education ?? 0;
                    ViewBag.KeywordScore = (int?)score.keyword ?? 0;
                    ViewBag.FormatScore = (int?)score.format ?? 0;
                    ViewBag.TotalScore = (int?)score.total ?? 0;
                }
            }
            // Lấy suggestions và cvContent nếu có (từ session hoặc ViewBag)
            var suggestionsJson = HttpContext.Session.GetString("EditSuggestions");
            var content = HttpContext.Session.GetString("EditSuggestionsContent");
            if (!string.IsNullOrEmpty(suggestionsJson))
            {
                try
                {
                    // Thử deserialize từ JSON string
                    ViewBag.Suggestions = JsonConvert.DeserializeObject<string>(suggestionsJson);
                }
                catch
                {
                    // Nếu không phải JSON, lấy trực tiếp
                    ViewBag.Suggestions = suggestionsJson;
                }
                ViewBag.CVContent = content;
                
                // Debug: Kiểm tra ViewBag
                System.Diagnostics.Debug.WriteLine($"ViewBag.Suggestions: {ViewBag.Suggestions}");
                System.Diagnostics.Debug.WriteLine($"ViewBag.CVContent: {ViewBag.CVContent}");
            }
            return View(result);
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
        public async Task<IActionResult> RecommendJobs([FromBody] JobSuggestionRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Skills))
            {
                return BadRequest("No skills provided");
            }
            try
            {
                var result = await _resumeAnalysisService.GetJobSuggestionsAsync(request.Skills);
                HttpContext.Session.SetString("JobRecommendations", JsonConvert.SerializeObject(result.Suggestions));
                return Json(result.Suggestions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ScoreCV(int resumeId, string cvContent = null)
        {
            // Kiểm tra đăng nhập
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            bool isLoggedIn = userIdClaim != null && int.TryParse(userIdClaim.Value, out _);

            string content = cvContent;
            if (string.IsNullOrEmpty(content))
            {
                // Nếu có resumeId, lấy từ DB nếu có
                var analysisResult = _dbContext.ResumeAnalysisResults.AsNoTracking().FirstOrDefault(r => r.ResumeId == resumeId);
                if (analysisResult == null || string.IsNullOrEmpty(analysisResult.Content))
                    return BadRequest("No CV content available for scoring");
                content = analysisResult.Content;
            }
            try
            {
                var (layout, skill, experience, education, keyword, format, rawJson) = await _resumeAnalysisService.AnalyzeScoreWithOpenAI(content);
                if (isLoggedIn)
                {
                    // Lưu vào DB
                    var resumeAnalysis = _dbContext.ResumeAnalysis.FirstOrDefault(a => a.ResumeId == resumeId);
                    if (resumeAnalysis == null)
                    {
                        resumeAnalysis = new ResumeAnalysis
                        {
                            ResumeId = resumeId,
                            AnalysisDate = DateTime.UtcNow
                        };
                        _dbContext.ResumeAnalysis.Add(resumeAnalysis);
                    }
                    resumeAnalysis.LayoutScore = layout;
                    resumeAnalysis.SkillScore = skill;
                    resumeAnalysis.ExperienceScore = experience;
                    resumeAnalysis.EducationScore = education;
                    resumeAnalysis.KeywordScore = keyword;
                    resumeAnalysis.FormatScore = format;
                    resumeAnalysis.Score = layout + skill + experience + education + keyword + format;
                    resumeAnalysis.Suggestions = rawJson;
                    resumeAnalysis.AnalysisDate = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();
                }
                // Luôn lưu kết quả chấm điểm vào session (dù có đăng nhập hay không)
                var scoreResult = new {
                    layout,
                    skill,
                    experience,
                    education,
                    keyword,
                    format,
                    total = layout + skill + experience + education + keyword + format
                };
                HttpContext.Session.SetString("CVScoreResult", Newtonsoft.Json.JsonConvert.SerializeObject(scoreResult));
                // Trả về kết quả (dù có lưu hay không)
                return Json(new {
                    success = true,
                    layout,
                    skill,
                    experience,
                    education,
                    keyword,
                    format,
                    total = layout + skill + experience + education + keyword + format
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerateFinalCV(string cvContent, string suggestions)
        {
            if (string.IsNullOrEmpty(cvContent) || string.IsNullOrEmpty(suggestions))
            {
                return BadRequest("Thiếu nội dung CV hoặc đề xuất.");
            }
            try
            {
                var finalCV = await _resumeAnalysisService.GenerateFinalCV(cvContent, suggestions);
                HttpContext.Session.SetString("FinalCVContent", finalCV);
                return RedirectToAction("ShowFinalCV");
            }
            catch (Exception ex)
            {
                return View("Error", new ErrorViewModel { Message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult ShowFinalCV()
        {
            var finalCV = HttpContext.Session.GetString("FinalCVContent");
            if (string.IsNullOrEmpty(finalCV))
            {
                ViewBag.ErrorMessage = "Chưa có CV hoàn chỉnh.";
                return View();
            }
            ViewBag.FinalCV = finalCV;
            return View();
        }

        [HttpPost]
        public IActionResult PublishFinalCV()
        {
            var finalCV = HttpContext.Session.GetString("FinalCVContent");
            if (string.IsNullOrEmpty(finalCV))
            {
                return BadRequest("Không có CV để xuất bản.");
            }

            // Tạo PDF bằng QuestPDF
            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Arial"));
                    page.Content()
                        .Column(col =>
                        {
                            col.Item().PaddingBottom(20).Text("CV Hoàn Chỉnh").FontSize(18).Bold().AlignCenter();
                            // Hiển thị nội dung CV, giữ định dạng xuống dòng
                            var lines = finalCV.Split('\n');
                            foreach (var line in lines)
                            {
                                col.Item().Text(line);
                            }
                        });
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", "CV_Hoan_Chinh.pdf");
        }

        //// Xóa hoặc chuyển hướng các action liên quan đến EditSuggestions
        //[HttpGet]
        //public IActionResult EditSuggestions()
        //{
        //    // Chuyển hướng sang AnalysisResult
        //    return RedirectToAction("AnalysisResult");
        //}

        //[HttpGet]
        //public IActionResult HasEditSuggestions()
        //{
        //    // Có thể bỏ hoặc trả về false luôn
        //    return Json(new { hasSuggestions = false });
        //}
    }
}