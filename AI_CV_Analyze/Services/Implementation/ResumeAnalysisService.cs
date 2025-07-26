using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using AI_CV_Analyze.Models;
using Microsoft.Extensions.Configuration;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure;
using System.IO;
using System;
using Microsoft.Extensions.Logging;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net.Http;
using PdfiumViewer;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;
using AI_CV_Analyze.Data;
using System.Security.Claims;
using System.Linq;

namespace AI_CV_Analyze.Services
{
    public class ResumeAnalysisService : IResumeAnalysisService
    {
        private readonly IConfiguration _configuration;
        private readonly DocumentAnalysisClient? _formRecognizerClient;
        private readonly string _formRecognizerEndpoint;
        private readonly string _formRecognizerKey;
        private readonly string _customModelId;
        private readonly string _openAIEndpoint;
        private readonly string _openAIKey;
        private readonly string _openAIDeploymentName;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _jobRecommendationEndpoint;
        private readonly string _jobRecommendationApiKey;
        private readonly ApplicationDbContext _dbContext;


        public ResumeAnalysisService(IConfiguration configuration, IHttpClientFactory httpClientFactory, ApplicationDbContext dbContext)
        {
            _configuration = configuration;
            _formRecognizerEndpoint = _configuration["AzureAI:FormRecognizerEndpoint"];
            _formRecognizerKey = _configuration["AzureAI:FormRecognizerKey"];
            _openAIEndpoint = _configuration["AzureAI:OpenAIEndpoint"];
            _openAIKey = _configuration["AzureAI:OpenAIKey"];
            _openAIDeploymentName = _configuration["AzureAI:OpenAIDeploymentName"];
            _httpClientFactory = httpClientFactory;
            _customModelId = _configuration["AzureAI:CustomModelId"];
            _jobRecommendationEndpoint = _configuration["JobRecommendation:Endpoint"];
            _jobRecommendationApiKey = _configuration["JobRecommendation:ApiKey"];
            _dbContext = dbContext;

            // Initialize FormRecognizer client only if configuration is available
            if (!string.IsNullOrEmpty(_formRecognizerEndpoint) && !string.IsNullOrEmpty(_formRecognizerKey))
            {
                _formRecognizerClient = new DocumentAnalysisClient(new Uri(_formRecognizerEndpoint), new AzureKeyCredential(_formRecognizerKey));
            }
            else
            {
                Console.WriteLine("Warning: FormRecognizer configuration is incomplete. Please check your appsettings.json");
                _formRecognizerClient = null;
            }

            // Validate OpenAI configuration
            if (string.IsNullOrEmpty(_openAIEndpoint) || string.IsNullOrEmpty(_openAIKey) || string.IsNullOrEmpty(_openAIDeploymentName))
            {
                Console.WriteLine("Warning: OpenAI configuration is incomplete. Please check your appsettings.json");
            }
        }

        public class PdfPreprocessor
        {
            public static async Task<Stream> ConvertMultiPagePdfToSingleImageAsync(Stream pdfStream)
            {
                try
                {
                    // Load PDF
                    using var pdfDocument = PdfDocument.Load(pdfStream);
                    int pageCount = pdfDocument.PageCount;

                if (pageCount == 1)
                {
                    // Nếu chỉ có 1 trang, render thành 1 ảnh luôn để xử lý thống nhất
                    using var bitmap = pdfDocument.Render(0, 2480, 3508, true);
                    using var ms = new MemoryStream();
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Position = 0;
                    return new MemoryStream(ms.ToArray());  // return bản sao m
                }

                var renderedImages = new List<Image<Rgba32>>();

                // Render từng trang thành ảnh
                for (int i = 0; i < pageCount; i++)
                {
                    using var bitmap = pdfDocument.Render(i, 2480, 3508, true); // A4 300dpi
                    using var ms = new MemoryStream();
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Position = 0;

                    var image = await Image.LoadAsync<Rgba32>(ms);
                    renderedImages.Add(image);
                }

                // Tính kích thước ảnh tổng (chiều dọc)
                int totalHeight = renderedImages.Sum(img => img.Height);
                int maxWidth = renderedImages.Max(img => img.Width);

                // Tạo ảnh trống để ghép
                using var finalImage = new Image<Rgba32>(maxWidth, totalHeight);
                int yOffset = 0;

                foreach (var image in renderedImages)
                {
                    finalImage.Mutate(ctx => ctx.DrawImage(image, new Point(0, yOffset), 1f));
                    yOffset += image.Height;
                }

                // Lưu ảnh ghép thành stream
                var outputStream = new MemoryStream();
                await finalImage.SaveAsync(outputStream, new PngEncoder());
                outputStream.Position = 0;

                return outputStream;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error processing PDF: {ex.Message}", ex);
                }
            }
        }

        // Hàm gọi Azure OpenAI để chấm điểm từng tiêu chí
        public async Task<(int Layout, int Skill, int Experience, int Education, int Keyword, int Format, string RawJson)> AnalyzeScoreWithOpenAI(string cvContent)
        {
            if (string.IsNullOrWhiteSpace(cvContent))
                return (0, 0, 0, 0, 0, 0, "");

            // Check if OpenAI configuration is available
            if (string.IsNullOrEmpty(_openAIEndpoint) || string.IsNullOrEmpty(_openAIKey) || string.IsNullOrEmpty(_openAIDeploymentName))
            {
                return (0, 0, 0, 0, 0, 0, "Configuration error: Missing Azure OpenAI information");
            }

            string endpoint = _openAIEndpoint;
            string apiKey = _openAIKey;
            string modelName = _openAIDeploymentName;

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(2);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            // Prompt yêu cầu AI trả về JSON điểm từng tiêu chí
            string jsonExample = "{\"layout\": số, \"skill\": số, \"experience\": số, \"education\": số, \"keyword\": số, \"format\": số}";
            string prompt = @"Bạn là chuyên gia nhân sự. Hãy chấm điểm CV dưới đây theo các tiêu chí sau (0-10, số nguyên, càng cao càng tốt):
- layout: Bố cục & trình bày
- skill: Kỹ năng
- experience: Kinh nghiệm (nếu không có thì là project)
- education: Học vấn
- keyword: Từ khóa phù hợp
- format: Hình thức/định dạng
Trả về kết quả đúng chuẩn JSON như sau: " + jsonExample + @"
CV:
" + cvContent;

            var requestBody = new
            {
                model = modelName,
                messages = new[]
                {
                    new { role = "system", content = "Bạn là chuyên gia nhân sự, hãy chấm điểm CV theo các tiêu chí và trả về JSON." },
                    new { role = "user", content = prompt }
                },
                max_tokens = 300,
                temperature = 0.2
            };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await SendWithRetryAsync(() => client.PostAsync(endpoint, content));
                if (response == null)
                    return (0, 0, 0, 0, 0, 0, "Không nhận được phản hồi từ AI");
                    
                var responseString = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseString))
                    return (0, 0, 0, 0, 0, 0, "Phản hồi từ AI rỗng");
                    
                JObject doc;
                try
                {
                    doc = JObject.Parse(responseString);
                }
                catch (JsonReaderException)
                {
                    return (0, 0, 0, 0, 0, 0, "Lỗi parsing JSON từ AI");
                }
                
                var answer = doc["choices"]?[0]?["message"]?["content"]?.ToString();
                if (string.IsNullOrWhiteSpace(answer))
                    return (0, 0, 0, 0, 0, 0, "AI không trả về kết quả chấm điểm");
                    
                // Tìm JSON trong answer
                string jsonScore = answer.Trim();
                int start = jsonScore.IndexOf('{');
                int end = jsonScore.LastIndexOf('}');
                if (start >= 0 && end > start)
                    jsonScore = jsonScore.Substring(start, end - start + 1);
                    
                JObject scoreObj;
                try
                {
                    scoreObj = JObject.Parse(jsonScore);
                }
                catch (JsonReaderException)
                {
                    return (0, 0, 0, 0, 0, 0, "Lỗi parsing JSON điểm từ AI");
                }
                int layout = scoreObj.Value<int?>("layout") ?? 0;
                int skill = scoreObj.Value<int?>("skill") ?? 0;
                int experience = scoreObj.Value<int?>("experience") ?? 0;
                int education = scoreObj.Value<int?>("education") ?? 0;
                int keyword = scoreObj.Value<int?>("keyword") ?? 0;
                int format = scoreObj.Value<int?>("format") ?? 0;
                return (layout, skill, experience, education, keyword, format, jsonScore);
            }
            catch (Exception ex)
            {
                return (0, 0, 0, 0, 0, 0, $"Lỗi khi chấm điểm: {ex.Message}");
            }
        }

        public async Task<ResumeAnalysisResult> AnalyzeResume(IFormFile cvFile, int userId, bool skipDb = false)
        {
            if (cvFile == null || cvFile.Length == 0)
                throw new ArgumentException("CV file is null or empty.", nameof(cvFile));

            var supportedTypes = new[] { "application/pdf", "image/png", "image/jpeg" };
            string contentType = cvFile.ContentType ?? "";
            if (!supportedTypes.Contains(contentType.ToLowerInvariant()))
                throw new ArgumentException($"Unsupported file type. Supported types are: {string.Join(", ", supportedTypes)}", nameof(cvFile));

            // Đọc file thành byte[]
            byte[] fileBytes;
            using (var ms = new MemoryStream())
            {
                await cvFile.CopyToAsync(ms);
                fileBytes = ms.ToArray();
            }

            // Xác định loại file (PDF/JPG/PNG)
            string fileName = cvFile.FileName ?? "unknown_file";
            string ext = Path.GetExtension(fileName)?.ToLower();
            string fileType = "PDF";
            if (ext == ".jpg" || ext == ".jpeg") fileType = "JPG";
            else if (ext == ".png") fileType = "PNG";

            Resume resume = null;
            int resumeId = 0;
            if (!skipDb)
            {
                // Tạo entity Resume
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
                _dbContext.Resumes.Add(resume);
                await _dbContext.SaveChangesAsync();
                resumeId = resume.ResumeId;
            }

            Stream processedStream;
            try
            {
                if (contentType == "application/pdf")
                {
                    using var originalStream = new MemoryStream(fileBytes);
                    processedStream = await PdfPreprocessor.ConvertMultiPagePdfToSingleImageAsync(originalStream);
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
                return errorResult;
            }

            var analysisResult = new ResumeAnalysisResult
            {
                AnalysisDate = DateTime.UtcNow,
                FileName = fileName,
                AnalysisStatus = "Pending",
                ResumeId = resumeId // Gán ResumeId nếu có, nếu không thì = 0
            };

            try
            {
                if (_formRecognizerClient == null)
                {
                    analysisResult.AnalysisStatus = "Failed";
                    analysisResult.ErrorMessage = "FormRecognizer client is not configured. Please check your Azure AI configuration in appsettings.json";
                    return analysisResult;
                }

                if (string.IsNullOrEmpty(_customModelId))
                {
                    analysisResult.AnalysisStatus = "Failed";
                    analysisResult.ErrorMessage = "Custom model ID is not configured. Please check your Azure AI configuration in appsettings.json";
                    return analysisResult;
                }

                var operation = await _formRecognizerClient.AnalyzeDocumentAsync(WaitUntil.Completed, _customModelId, processedStream);
                var result = operation.Value;

                analysisResult.Content = result.Content;
                ExtractCustomFields(result, analysisResult);
                analysisResult.AnalysisStatus = string.IsNullOrEmpty(analysisResult.ErrorMessage) ? "Completed" : "Failed";

                // --- Gọi AI chấm điểm từng tiêu chí ---
                var (layout, skill, experience, education, keyword, format, rawJson) = await AnalyzeScoreWithOpenAI(analysisResult.Content);
                if (!skipDb && resume != null)
                {
                    // Lưu vào bảng ResumeAnalysis
                    var resumeAnalysis = new ResumeAnalysis
                    {
                        ResumeId = resume.ResumeId,
                        Score = layout + skill + experience + education + keyword + format, // Tổng điểm
                        LayoutScore = layout,
                        SkillScore = skill,
                        ExperienceScore = experience,
                        EducationScore = education,
                        KeywordScore = keyword,
                        FormatScore = format,
                        Suggestions = rawJson, // Có thể lưu lại JSON gốc để debug/hiển thị
                        AnalysisDate = DateTime.UtcNow
                    };
                    _dbContext.ResumeAnalysis.Add(resumeAnalysis);
                    await _dbContext.SaveChangesAsync();
                }
                // --- END ---
            }
            catch (RequestFailedException ex)
            {
                analysisResult.AnalysisStatus = "Failed";
                analysisResult.ErrorMessage = $"Lỗi từ Document Intelligence: {ex.Message} (Status Code: {ex.Status})";
            }
            catch (Exception ex)
            {
                analysisResult.AnalysisStatus = "Failed";
                analysisResult.ErrorMessage = $"Lỗi phân tích CV: {ex.Message}";
            }

            // Không lưu ResumeAnalysisResult vào DB nếu skipDb
            return analysisResult;
        }


        private void ExtractCustomFields(AnalyzeResult result, ResumeAnalysisResult analysisResult)
        {
            if (result.Documents == null || !result.Documents.Any())
            {
                analysisResult.ErrorMessage = "Không tìm thấy dữ liệu từ mô hình.";
                return;
            }

            var document = result.Documents.First();

            foreach (var field in document.Fields)
            {
                if (field.Key == null)
                    continue;
                    
                string value = GetFieldValue(field.Value);
                float? confidence = field.Value?.Confidence;

                switch (field.Key.ToLower())
                {
                    case "name":
                    case "họ và tên":
                    case "họ tên":
                        analysisResult.Name = value;
                        break;
                    case "email":
                    case "e-mail":
                        analysisResult.Email = value;
                        break;
                    case "skills":
                    case "skill":
                    case "kỹ năng":
                    case "kĩ năng":
                        analysisResult.Skills = value;
                        break;
                    case "experience":
                    case "kinh nghiệm":
                        analysisResult.Experience = value;
                        break;
                    case "projects":
                    case "project":
                    case "dự án":
                        analysisResult.Project = value;
                        break;
                    case "education":
                    case "học vấn":
                        analysisResult.Education = value;
                        break;
                    case "language proficiency":
                    case "language":
                    case "trình độ ngoại ngữ":
                    case "trình độ ngôn ngữ":
                    case "ngoại ngữ":
                    case "ngôn ngữ":
                        analysisResult.LanguageProficiency = value;
                        break;
                    case "certificate":
                    case "chứng chỉ":
                        analysisResult.Certificate = value;
                        break;
                    case "achievement":
                    case "thành tựu":
                    case "honor":
                    case "giải thưởng":
                    case "achievements":
                    case "honors":
                    case "awards":
                    case "award":
                    case "honors and awards":
                        analysisResult.Achievement = value;
                        break;

                }
            }

            if (string.IsNullOrEmpty(analysisResult.Name) && string.IsNullOrEmpty(analysisResult.Email) &&
                string.IsNullOrEmpty(analysisResult.Skills) && string.IsNullOrEmpty(analysisResult.Experience) &&
                string.IsNullOrEmpty(analysisResult.Project) && string.IsNullOrEmpty(analysisResult.Education))
            {
                analysisResult.ErrorMessage = "No fields could be extracted (Name, Email, Skills, Experience, Projects, Education, LanguageProficiency, Certificate, Achivement, Course) from model.";
            }
        }


        private string GetFieldValue(DocumentField field)
        {
            if (field == null || field.Value == null)
                return null;

            try
            {
                switch (field.FieldType)
                {
                    case DocumentFieldType.String:
                        return field.Value?.AsString() ?? "";

                    case DocumentFieldType.List:
                        var sb = new StringBuilder();
                        foreach (var item in field.Value.AsList())
                        {
                            var fieldValue = GetFieldValue(item);
                            if (!string.IsNullOrEmpty(fieldValue))
                            {
                                sb.AppendLine(fieldValue);
                            }
                        }
                        return sb.ToString().Trim();

                    case DocumentFieldType.Dictionary:
                        // Handle dictionary type if needed
                        return null;

                    default:
                        return field.Content;
                }
            }
            catch
            {
                // Có thể log hoặc xử lý riêng nếu cần, hiện tại chỉ return null
                return null;
            }
        }



        public async Task<JobSuggestionResult> GetJobSuggestionsAsync(string skills, string workExperience)
        {
            // Use Azure OpenAI only
            if (string.IsNullOrEmpty(_openAIEndpoint) || string.IsNullOrEmpty(_openAIKey) || string.IsNullOrEmpty(_openAIDeploymentName))
            {
                return new JobSuggestionResult { ImprovementPlan = "Configuration error: Missing Azure OpenAI information. Please check the AzureAI configuration in appsettings.json" };
            }

            if (string.IsNullOrWhiteSpace(skills))
            {
                return new JobSuggestionResult { ImprovementPlan = "Error: Skills parameter is required." };
            }

            string endpoint = _openAIEndpoint;
            string apiKey = _openAIKey;
            string modelName = _openAIDeploymentName;

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(2);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            // Prompt for OpenAI
            string prompt = $@"Given the following skills and work/project experience, recommend the most suitable job. Return only:\n- The recommended job title\n- The match percentage (integer, 0-100)\n- The most important skill to improve for a better match (if any; if none, say 'None' and percentage is 100%)\n\nFormat:\nRecommended Job: <job title>\nMatch Percentage: <number>%\nSkill to Improve: <skill or 'None'>\n\nSkills: {skills}\nWork/Project Experience: {workExperience ?? "None"}";

            var requestBody = new
            {
                model = modelName,
                messages = new[]
                {
                    new { role = "system", content = "You are an AI assistant specialized in resume analysis and job recommendation. Always provide structured, professional career advice with skill match percentage and improvement suggestions." },
                    new { role = "user", content = prompt }
                },
                max_tokens = 1000,
                temperature = 0.2
            };

            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await SendWithRetryAsync(() => client.PostAsync(endpoint, content));
                if (response == null)
                {
                    return new JobSuggestionResult { ImprovementPlan = "You are sending too many requests to the AI or there is a network error. Please try again in a few minutes." };
                }

                var responseString = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseString))
                {
                    return new JobSuggestionResult { ImprovementPlan = "Empty response received from AI." };
                }

                JObject doc;
                try
                {
                    doc = JObject.Parse(responseString);
                }
                catch (JsonReaderException)
                {
                    return new JobSuggestionResult { ImprovementPlan = "Error parsing JSON response from AI." };
                }
                
                var answer = doc["choices"]?[0]?["message"]?["content"]?.ToString();
                if (string.IsNullOrWhiteSpace(answer))
                {
                    return new JobSuggestionResult { ImprovementPlan = "No suggestions were received from AI. Please try again." };
                }

                // Parse the answer using regex or simple string parsing
                var result = new JobSuggestionResult();
                try
                {
                    // Recommended Job
                    var jobMatch = System.Text.RegularExpressions.Regex.Match(answer, @"Recommended Job:\s*(.+)");
                    if (jobMatch.Success) result.RecommendedJob = jobMatch.Groups[1].Value.Trim();

                    // Match Percentage
                    var percentMatch = System.Text.RegularExpressions.Regex.Match(answer, @"Match Percentage:\s*(\d+)%");
                    if (percentMatch.Success) result.MatchPercentage = int.Parse(percentMatch.Groups[1].Value);

                    // Skill to Improve
                    var skillImproveMatch = System.Text.RegularExpressions.Regex.Match(answer, @"Skill to Improve:\s*(.+)");
                    if (skillImproveMatch.Success)
                    {
                        var skillImprove = skillImproveMatch.Groups[1].Value.Trim();
                        if (!string.Equals(skillImprove, "None", StringComparison.OrdinalIgnoreCase))
                            result.MissingSkills = new List<string> { skillImprove };
                        else
                            result.MissingSkills = new List<string>();
                    }
                    else
                    {
                        result.MissingSkills = new List<string>();
                    }
                    // MatchedSkills is not used in this simple output
                    result.MatchedSkills = null;
                    // ImprovementPlan is not used in this simple output
                    result.ImprovementPlan = null;
                }
                catch
                {
                    // Fallback: just return the raw answer in ImprovementPlan
                    result.ImprovementPlan = answer;
                }

                return result;
            }
            catch (Exception ex)
            {
                return new JobSuggestionResult { ImprovementPlan = $"Error getting job suggestions: {ex.Message}" };
            }
        }

        public async Task<string> GetCVEditSuggestions(string cvContent)
        {
            // Use Azure OpenAI only
            if (string.IsNullOrEmpty(_openAIEndpoint) || string.IsNullOrEmpty(_openAIKey) || string.IsNullOrEmpty(_openAIDeploymentName))
            {
                return "Configuration error: Missing Azure OpenAI information. Please check the AzureAI configuration in appsettings.json";
            }

            if (string.IsNullOrWhiteSpace(cvContent))
            {
                return "Error: CV content is empty or null.";
            }

            string endpoint = _openAIEndpoint;
            string apiKey = _openAIKey;
            string modelName = _openAIDeploymentName;

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(2); // Set timeout to 2 minutes
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var requestBody = new
            {
                model = modelName,
                messages = new[]
                {
                    new {
                        role = "system",
                        content = "You are a human resources expert with over 10 years of experience in recruitment and CV evaluation. Analyze the CV in detail and provide specific and professional suggestions. Focus on:\n\n" +
                                  "1. CV structure and layout\n" +
                                  "2. Content and presentation\n" +
                                  "3. Strengths to highlight\n" +
                                  "4. Weaknesses to improve\n" +
                                  "5. Important keywords and skills\n" +
                                  "6. Job description writing style\n" +
                                  "7. Formatting and visual presentation\n\n" +
                                  "Please give clear, specific, and actionable feedback."
                    },
                    new { role = "user", content = cvContent }
                },
                max_tokens = 2000,
                temperature = 0.7
            };

            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine($"==== OpenAI Request ====");
                Console.WriteLine($"Endpoint: {endpoint}");
                Console.WriteLine($"Model: {modelName}");
                Console.WriteLine($"Request JSON: {json}");

                var response = await SendWithRetryAsync(() => client.PostAsync(endpoint, content));

                if (response == null)
                {
                    return "You are sending too many requests to the AI or there is a network error. Please try again in a few minutes.";
                }

                Console.WriteLine($"==== OpenAI Response Status: {response.StatusCode} ====");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"==== OpenAI Error Response: {error} ====");

                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        return "Authentication error: Invalid API key or incorrect endpoint. Please check the OpenAI configuration in appsettings.json";
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return "Error: Model or deployment not found. Please check the model/deployment name in configuration.";
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        return "Error: Too many requests. Please try again in a few minutes.";
                    }
                    else
                    {
                        return $"OpenAI Error: {response.StatusCode} - {error}";
                    }
                }

                var responseString = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseString))
                {
                    return "Empty response received from AI.";
                }

                Console.WriteLine($"==== OpenAI Success Response: {responseString} ====");

                JObject doc;
                try
                {
                    doc = JObject.Parse(responseString);
                }
                catch (JsonReaderException)
                {
                    return "Error parsing JSON response from AI.";
                }
                
                var suggestion = doc["choices"]?[0]?["message"]?["content"]?.ToString();

                if (string.IsNullOrEmpty(suggestion))
                {
                    return "No suggestions were received from AI. Please try again.";
                }

                return suggestion;
            }
            catch (Exception ex)
            {
                return $"Error getting CV edit suggestions: {ex.Message}";
            }
        }

        public async Task<string> GenerateFinalCV(string cvContent, string suggestions)
        {
            if (string.IsNullOrEmpty(_openAIEndpoint) || string.IsNullOrEmpty(_openAIKey) || string.IsNullOrEmpty(_openAIDeploymentName))
            {
                return "Configuration error: Missing Azure OpenAI information. Please check the AzureAI configuration in appsettings.json";
            }

            if (string.IsNullOrWhiteSpace(cvContent))
            {
                return "Error: CV content is empty or null.";
            }

            if (string.IsNullOrWhiteSpace(suggestions))
            {
                return "Error: Suggestions are empty or null.";
            }

            string endpoint = _openAIEndpoint;
            string apiKey = _openAIKey;
            string modelName = _openAIDeploymentName;

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(2);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            string prompt = $@"You are an HR expert. Please revise the following CV according to the suggestions to create a complete, professional, and optimized version. Only return the edited CV content without any additional explanations.Edit suggestions: {suggestions} Original CV: {cvContent}";

            var requestBody = new
            {
                model = modelName,
                messages = new[]
                {
                    new { role = "system", content = "You are an HR expert, please revise the CV based on the suggestions." },
                    new { role = "user", content = prompt }
                },
                max_tokens = 2000,
                temperature = 0.5
            };

            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await SendWithRetryAsync(() => client.PostAsync(endpoint, content));
                if (response == null)
                    return "No response received from AI.";

                var responseString = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseString))
                    return "Empty response received from AI.";

                JObject doc;
                try
                {
                    doc = JObject.Parse(responseString);
                }
                catch (JsonReaderException)
                {
                    return "Error parsing JSON response from AI.";
                }
                
                var finalCV = doc["choices"]?[0]?["message"]?["content"]?.ToString();

                if (string.IsNullOrEmpty(finalCV))
                    return "AI did not return the edited CV content.";

                return finalCV.Trim();
            }
            catch (Exception ex)
            {
                return $"Error generating final CV: {ex.Message}";
            }
        }


        // Hàm retry request vô hạn
        private async Task<HttpResponseMessage?> SendWithRetryAsync(Func<Task<HttpResponseMessage>> sendRequest, int delayMs = 2000)
        {
            while (true)
            {
                try
                {
                    var response = await sendRequest();
                    if (response.IsSuccessStatusCode || response.StatusCode != System.Net.HttpStatusCode.TooManyRequests)
                    {
                        return response;
                    }
                }
                catch (HttpRequestException)
                {
                    // Có thể log lỗi nếu cần
                }
                await Task.Delay(delayMs);
            }
        }
    }
}