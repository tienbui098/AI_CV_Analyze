using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using AI_CV_Analyze.Models;
using AI_CV_Analyze.Services;
using System;
using System.Text;
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
            // Xóa toàn bộ thông tin cũ trong session trước khi phân tích CV mới
            HttpContext.Session.Remove("ResumeAnalysisResult");
            HttpContext.Session.Remove("CVContent");
            HttpContext.Session.Remove("EditSuggestions");
            HttpContext.Session.Remove("EditSuggestionsContent");
            HttpContext.Session.Remove("CVScoreResult");
            HttpContext.Session.Remove("JobRecommendations");
            HttpContext.Session.Remove("JobSuggestionResult");
            HttpContext.Session.Remove("JobSuggestionSkills");
            HttpContext.Session.Remove("JobSuggestionWorkExperience");
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
                    
                    // Lưu thêm thông tin ResumeData nếu chưa có
                    var existingResumeData = _dbContext.ResumeData.FirstOrDefault(rd => rd.ResumeId == result.ResumeId);
                    if (existingResumeData == null)
                    {
                        var resumeData = new ResumeData
                        {
                            ResumeId = result.ResumeId,
                            ExtractedData = result.Content,
                            Language = "English", // Có thể detect language sau
                            Status = "Completed"
                        };
                        _dbContext.ResumeData.Add(resumeData);
                        await _dbContext.SaveChangesAsync();
                    }
                }
                else
                {
                    // Không đăng nhập: chỉ phân tích, không lưu DB
                    result = await _resumeAnalysisService.AnalyzeResume(cvFile, 0, true); // skipDb = true
                }
                HttpContext.Session.SetString("ResumeAnalysisResult", JsonConvert.SerializeObject(result, GetJsonSettings()));
                HttpContext.Session.SetString("CVContent", result.Content ?? "");
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
                HttpContext.Session.SetString("EditSuggestions", JsonConvert.SerializeObject(suggestions, GetJsonSettings()));
                HttpContext.Session.SetString("EditSuggestionsContent", Content);
                
                // Lưu edit suggestions vào database nếu user đã đăng nhập
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    // Lấy resumeId từ session
                    var resultJson = HttpContext.Session.GetString("ResumeAnalysisResult");
                    if (!string.IsNullOrEmpty(resultJson))
                    {
                        var result = JsonConvert.DeserializeObject<ResumeAnalysisResult>(resultJson);
                        if (result != null && result.ResumeId > 0)
                        {
                            // Cập nhật ResumeAnalysis với suggestions
                            var resumeAnalysis = _dbContext.ResumeAnalysis.FirstOrDefault(a => a.ResumeId == result.ResumeId);
                            if (resumeAnalysis != null)
                            {
                                resumeAnalysis.Suggestions = suggestions;
                                resumeAnalysis.AnalysisDate = DateTime.UtcNow;
                                await _dbContext.SaveChangesAsync();
                            }
                        }
                    }
                }
                
                // Lấy lại result từ session (hoặc DB nếu cần)
                var resultJson2 = HttpContext.Session.GetString("ResumeAnalysisResult");
                ResumeAnalysisResult result2 = null;
                if (!string.IsNullOrEmpty(resultJson2))
                {
                    result2 = JsonConvert.DeserializeObject<ResumeAnalysisResult>(resultJson2);
                }
                
                // Lấy lại kết quả job recommendation từ session để không bị mất
                var jobSessionJson = HttpContext.Session.GetString("JobRecommendations");
                if (!string.IsNullOrEmpty(jobSessionJson))
                {
                    try
                    {
                        var jobSuggestionResult = JsonConvert.DeserializeObject<JobSuggestionResult>(jobSessionJson);
                        ViewBag.JobRecommendations = jobSuggestionResult;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error deserializing JobSuggestionResult in GetEditSuggestions: {ex.Message}");
                    }
                }
                
                // Lấy lại kết quả chấm điểm CV từ session để không bị mất
                var scoreJson = HttpContext.Session.GetString("CVScoreResult");
                if (!string.IsNullOrEmpty(scoreJson))
                {
                    try
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
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error deserializing CVScoreResult in GetEditSuggestions: {ex.Message}");
                    }
                }
                
                // Debug: Kiểm tra xem suggestions có được lấy không
                System.Diagnostics.Debug.WriteLine($"Suggestions: {suggestions}");
                System.Diagnostics.Debug.WriteLine($"Content: {Content}");
                
                return View("AnalysisResult", result2);
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

            // Always generate job recommendations from extracted sections
            string skills = null;
            string workExp = null;
            if (result != null)
            {
                var skillsList = new List<string>();
                if (!string.IsNullOrWhiteSpace(result.Skills)) skillsList.Add(result.Skills);
                if (!string.IsNullOrWhiteSpace(result.LanguageProficiency)) skillsList.Add(result.LanguageProficiency);
                skills = string.Join(", ", skillsList);
                var workList = new List<string>();
                if (!string.IsNullOrWhiteSpace(result.Experience)) workList.Add(result.Experience);
                if (!string.IsNullOrWhiteSpace(result.Project)) workList.Add(result.Project);
                workExp = string.Join("\n", workList);
            }
            
            // Ưu tiên lấy kết quả job recommendation từ session trước
            JobSuggestionResult jobSuggestionResult = null;
            var jobSessionJson = HttpContext.Session.GetString("JobRecommendations");
            System.Diagnostics.Debug.WriteLine($"JobRecommendations from session: {jobSessionJson}");
            if (!string.IsNullOrEmpty(jobSessionJson))
            {
                try
                {
                    jobSuggestionResult = JsonConvert.DeserializeObject<JobSuggestionResult>(jobSessionJson);
                    System.Diagnostics.Debug.WriteLine($"Successfully deserialized JobSuggestionResult from session");
                }
                catch (Exception ex)
                {
                    // Nếu deserialize lỗi, tiếp tục tạo mới
                    System.Diagnostics.Debug.WriteLine($"Error deserializing JobSuggestionResult: {ex.Message}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No JobRecommendations found in session");
            }
            
            // KHÔNG tự động tạo job recommendation mới nếu không có trong session
            // Chỉ tạo khi người dùng thực sự sử dụng chức năng gợi ý việc làm
            //if (jobSuggestionResult == null && !string.IsNullOrWhiteSpace(skills))
            //{
            //    System.Diagnostics.Debug.WriteLine("Creating new job recommendation from skills");
            //    jobSuggestionResult = _resumeAnalysisService.GetJobSuggestionsAsync(skills, workExp).GetAwaiter().GetResult();
            //}
            
            ViewBag.JobRecommendations = jobSuggestionResult;

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
                

            }
            return View(result);
        }

        

        // Job prediction methods
        [HttpGet]
        public IActionResult JobPrediction()
        {
            ViewBag.ErrorMessage = null;
            var skills = HttpContext.Session.GetString("JobSuggestionSkills");
            var workExp = HttpContext.Session.GetString("JobSuggestionWorkExperience");
            var resultJson = HttpContext.Session.GetString("JobRecommendations");
            var model = new JobSuggestionRequest { Skills = skills, WorkExperience = workExp };
            if (!string.IsNullOrEmpty(resultJson))
            {
                var result = JsonConvert.DeserializeObject<AI_CV_Analyze.Models.JobSuggestionResult>(resultJson);
                ViewBag.JobSuggestionResult = result;
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> JobPrediction(JobSuggestionRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.Skills))
            {
                ViewBag.ErrorMessage = "Please enter your skills.";
                // Nếu có kết quả cũ trong session thì hiển thị lại
                var resultJson = HttpContext.Session.GetString("JobRecommendations");
                if (!string.IsNullOrEmpty(resultJson))
                {
                    var result = JsonConvert.DeserializeObject<AI_CV_Analyze.Models.JobSuggestionResult>(resultJson);
                    ViewBag.JobSuggestionResult = result;
                }
                return View(model);
            }

            try
            {
                var result = await _resumeAnalysisService.GetJobSuggestionsAsync(model.Skills, model.WorkExperience);
                ViewBag.JobSuggestionResult = result;
                // Lưu kết quả gợi ý việc làm vào session với key "JobRecommendations"
                HttpContext.Session.SetString("JobRecommendations", JsonConvert.SerializeObject(result, GetJsonSettings()));
                HttpContext.Session.SetString("JobSuggestionSkills", model.Skills);
                HttpContext.Session.SetString("JobSuggestionWorkExperience", model.WorkExperience);
                
                // Lưu job recommendations vào database nếu user đã đăng nhập
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    // Lấy resumeId từ session hoặc tạo mới
                    var resumeResultJson = HttpContext.Session.GetString("ResumeAnalysisResult");
                    int? resumeId = null;
                    if (!string.IsNullOrEmpty(resumeResultJson))
                    {
                        var resumeResult = JsonConvert.DeserializeObject<ResumeAnalysisResult>(resumeResultJson);
                        resumeId = resumeResult.ResumeId;
                    }
                    
                    if (resumeId.HasValue)
                    {
                        // Lưu cả RecommendedJob và Suggestions (nếu có)
                        if (!string.IsNullOrEmpty(result.RecommendedJob))
                        {
                            await SaveJobRecommendation(resumeId.Value, result.RecommendedJob, result.MatchPercentage, result.ImprovementPlan);
                        }
                        
                        // Lưu các suggestions từ Dictionary nếu có
                        if (result.Suggestions != null && result.Suggestions.Count > 0)
                        {
                            foreach (var suggestion in result.Suggestions)
                            {
                                await SaveJobRecommendation(resumeId.Value, suggestion.Key, (int)(suggestion.Value * 100), suggestion.Key);
                            }
                        }
                    }
                }
                
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error getting job suggestions: {ex.Message}";
                // Nếu có kết quả cũ trong session thì hiển thị lại
                var resultJson = HttpContext.Session.GetString("JobRecommendations");
                if (!string.IsNullOrEmpty(resultJson))
                {
                    var result = JsonConvert.DeserializeObject<AI_CV_Analyze.Models.JobSuggestionResult>(resultJson);
                    ViewBag.JobSuggestionResult = result;
                }
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
                var result = await _resumeAnalysisService.GetJobSuggestionsAsync(request.Skills, request.WorkExperience);
                HttpContext.Session.SetString("JobRecommendations", JsonConvert.SerializeObject(result, GetJsonSettings()));
                
                // Lưu job recommendations vào database nếu user đã đăng nhập
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    // Lấy resumeId từ session
                    var resumeResultJson = HttpContext.Session.GetString("ResumeAnalysisResult");
                    int? resumeId = null;
                    if (!string.IsNullOrEmpty(resumeResultJson))
                    {
                        var resumeResult = JsonConvert.DeserializeObject<ResumeAnalysisResult>(resumeResultJson);
                        resumeId = resumeResult.ResumeId;
                    }
                    
                    if (resumeId.HasValue)
                    {
                        // Lưu cả RecommendedJob và Suggestions (nếu có)
                        if (!string.IsNullOrEmpty(result.RecommendedJob))
                        {
                            await SaveJobRecommendation(resumeId.Value, result.RecommendedJob, result.MatchPercentage, result.ImprovementPlan);
                        }
                        
                        // Lưu các suggestions từ Dictionary nếu có
                        if (result.Suggestions != null && result.Suggestions.Count > 0)
                        {
                            foreach (var suggestion in result.Suggestions)
                            {
                                await SaveJobRecommendation(resumeId.Value, suggestion.Key, (int)(suggestion.Value * 100), suggestion.Key);
                            }
                        }
                    }
                }
                
                return Json(result);
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
                HttpContext.Session.SetString("CVScoreResult", JsonConvert.SerializeObject(scoreResult, GetJsonSettings()));
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
                
                // Lưu final CV vào database nếu user đã đăng nhập
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    // Lấy resumeId từ session
                    var resultJson = HttpContext.Session.GetString("ResumeAnalysisResult");
                    if (!string.IsNullOrEmpty(resultJson))
                    {
                        var result = JsonConvert.DeserializeObject<ResumeAnalysisResult>(resultJson);
                        if (result != null && result.ResumeId > 0)
                        {
                            // Lưu vào ResumeHistory
                            var resumeHistory = new ResumeHistory
                            {
                                ResumeId = result.ResumeId,
                                FileName = $"Final_CV_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                                FileData = System.Text.Encoding.UTF8.GetBytes(finalCV),
                                UploadDate = DateTime.UtcNow,
                                Version = 1,
                                Score = 100 // Final CV được coi như hoàn hảo
                            };
                            _dbContext.ResumeHistory.Add(resumeHistory);
                            await _dbContext.SaveChangesAsync();
                        }
                    }
                }
                
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
        public async Task<IActionResult> PublishFinalCV()
        {
            var finalCV = HttpContext.Session.GetString("FinalCVContent");
            if (string.IsNullOrEmpty(finalCV))
            {
                return BadRequest("Không có CV để xuất bản.");
            }

            // Lưu thông tin publish vào database nếu user đã đăng nhập
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                // Lấy resumeId từ session
                var resultJson = HttpContext.Session.GetString("ResumeAnalysisResult");
                if (!string.IsNullOrEmpty(resultJson))
                {
                    var result = JsonConvert.DeserializeObject<ResumeAnalysisResult>(resultJson);
                    if (result != null && result.ResumeId > 0)
                    {
                        // Cập nhật ResumeAnalysis với trạng thái published
                        var resumeAnalysis = _dbContext.ResumeAnalysis.FirstOrDefault(a => a.ResumeId == result.ResumeId);
                        if (resumeAnalysis != null)
                        {
                            resumeAnalysis.Suggestions = resumeAnalysis.Suggestions + "\n\n--- PUBLISHED ---\nPublished Date: " + DateTime.UtcNow.ToString();
                            await _dbContext.SaveChangesAsync();
                        }
                        
                        // Lưu thêm vào ResumeHistory với trạng thái published
                        var publishedResume = new ResumeHistory
                        {
                            ResumeId = result.ResumeId,
                            FileName = $"Published_CV_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                            FileData = System.Text.Encoding.UTF8.GetBytes(finalCV),
                            UploadDate = DateTime.UtcNow,
                            Version = 2, // Version cao hơn để phân biệt với final CV
                            Score = 100
                        };
                        _dbContext.ResumeHistory.Add(publishedResume);
                        await _dbContext.SaveChangesAsync();
                    }
                }
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
                            var lines = (finalCV ?? "").Split('\n');
                            foreach (var line in lines)
                            {
                                col.Item().Text(line ?? "");
                            }
                        });
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", "CV_Hoan_Chinh.pdf");
        }

        [HttpPost]
        public IActionResult ClearJobRecommendationSession()
        {
            HttpContext.Session.Remove("JobRecommendations");
            return Ok();
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

        /// <summary>
        /// Helper method để lưu job recommendation vào database
        /// </summary>
        private JsonSerializerSettings GetJsonSettings()
        {
            return new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            };
        }

        private async Task SaveJobRecommendation(int resumeId, string jobTitle, int matchPercentage, string description)
        {
            try
            {
                // Tìm hoặc tạo job category
                var jobCategory = _dbContext.JobCategories.FirstOrDefault(c => c.Name == jobTitle);
                if (jobCategory == null)
                {
                    jobCategory = new JobCategory
                    {
                        Name = jobTitle,
                        Description = description ?? jobTitle
                    };
                    _dbContext.JobCategories.Add(jobCategory);
                    await _dbContext.SaveChangesAsync();
                }
                
                // Kiểm tra xem recommendation đã tồn tại chưa
                var existingRecommendation = _dbContext.JobCategoryRecommendations
                    .FirstOrDefault(r => r.ResumeId == resumeId && r.CategoryId == jobCategory.CategoryId);
                
                if (existingRecommendation == null)
                {
                    // Tạo job recommendation mới
                    var recommendation = new JobCategoryRecommendation
                    {
                        ResumeId = resumeId,
                        CategoryId = jobCategory.CategoryId,
                        RelevanceScore = matchPercentage / 100.0m // Convert percentage to decimal
                    };
                    _dbContext.JobCategoryRecommendations.Add(recommendation);
                    await _dbContext.SaveChangesAsync();
                }
                else
                {
                    // Cập nhật relevance score nếu cần
                    if (existingRecommendation.RelevanceScore != matchPercentage / 100.0m)
                    {
                        existingRecommendation.RelevanceScore = matchPercentage / 100.0m;
                        await _dbContext.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error nhưng không throw để không ảnh hưởng đến flow chính
                System.Diagnostics.Debug.WriteLine($"Error saving job recommendation: {ex.Message}");
            }
        }
    }
}