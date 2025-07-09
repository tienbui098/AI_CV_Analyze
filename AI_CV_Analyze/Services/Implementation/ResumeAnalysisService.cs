//using Microsoft.AspNetCore.Http;
//using System.Threading.Tasks;
//using AI_CV_Analyze.Models;
//using Microsoft.Extensions.Configuration;
//using Azure.AI.FormRecognizer.DocumentAnalysis;
//using Azure;
//using System.IO;
//using System;
//using System.Net.Http;
//using System.Net.Http.Headers;
//using System.Text;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
//using System.Collections.Generic;

//namespace AI_CV_Analyze.Services
//{
//    public class ResumeAnalysisService : IResumeAnalysisService
//    {
//        private readonly IConfiguration _configuration;
//        private readonly DocumentAnalysisClient _formRecognizerClient;
//        private readonly string _formRecognizerEndpoint;
//        private readonly string _formRecognizerKey;
//        private readonly string _openAIEndpoint;
//        private readonly string _openAIKey;
//        private readonly string _openAIDeploymentName;
//        private readonly IHttpClientFactory _httpClientFactory;
//        private readonly string _jobRecommendationEndpoint;
//        private readonly string _jobRecommendationApiKey;

//        public ResumeAnalysisService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
//        {
//            _configuration = configuration;
//            _formRecognizerEndpoint = _configuration["AzureAI:FormRecognizerEndpoint"];
//            _formRecognizerKey = _configuration["AzureAI:FormRecognizerKey"];
//            _formRecognizerClient = new DocumentAnalysisClient(new Uri(_formRecognizerEndpoint), new AzureKeyCredential(_formRecognizerKey));
//            _openAIEndpoint = _configuration["AzureAI:OpenAIEndpoint"];
//            _openAIKey = _configuration["AzureAI:OpenAIKey"];
//            _openAIDeploymentName = _configuration["AzureAI:OpenAIDeploymentName"];
//            _httpClientFactory = httpClientFactory;

//            // Read job recommendation settings from configuration
//            _jobRecommendationEndpoint = _configuration["JobRecommendation:Endpoint"];
//            _jobRecommendationApiKey = _configuration["JobRecommendation:ApiKey"];

//            // Validate OpenAI configuration
//            if (string.IsNullOrEmpty(_openAIEndpoint) || string.IsNullOrEmpty(_openAIKey) || string.IsNullOrEmpty(_openAIDeploymentName))
//            {
//                Console.WriteLine("Warning: OpenAI configuration is incomplete. Please check your appsettings.json");
//            }
//        }

//        public async Task<ResumeAnalysisResult> AnalyzeResume(IFormFile cvFile)
//        {
//            using var stream = cvFile.OpenReadStream();
//            var operation = await _formRecognizerClient.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-document", stream);
//            var result = operation.Value;

//            var analysisResult = new ResumeAnalysisResult
//            {
//                Content = result.Content,
//                // Add more properties based on the analysis results
//                AnalysisDate = DateTime.UtcNow,
//                FileName = cvFile.FileName
//            };

//            // --- Section Extraction Logic ---
//            if (!string.IsNullOrEmpty(result.Content))
//            {
//                string[] lines = result.Content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
//                string currentSection = "";
//                var skills = new StringBuilder();
//                var education = new StringBuilder();
//                var experience = new StringBuilder();
//                var profile = new StringBuilder();
//                var objective = new StringBuilder();
//                var languages = new StringBuilder();
//                var project = new StringBuilder();
//                // Optionally: add more sections (profile, languages, etc.)

//                foreach (var line in lines)
//                {
//                    var trimmed = line.Trim();
//                    if (trimmed.ToUpper().StartsWith("SKILLS")) { currentSection = "skills"; continue; }
//                    if (trimmed.ToUpper().StartsWith("EDUCATION")) { currentSection = "education"; continue; }
//                    if (trimmed.ToUpper().StartsWith("EXPERIENCE") || trimmed.ToUpper().StartsWith("WORK EXPERIENCE")) { currentSection = "experience"; continue; }
//                    if (trimmed.ToUpper().StartsWith("PROFILE")) { currentSection = "profile"; continue; }
//                    if (trimmed.ToUpper().StartsWith("OBJECTIVE")) { currentSection = "objective"; continue; }
//                    if (trimmed.ToUpper().StartsWith("LANGUAGES")) { currentSection = "languages"; continue; }
//                    if (trimmed.ToUpper().StartsWith("PROJECT")) { currentSection = "project"; continue; }
//                    // Optionally: add more section headers

//                    switch (currentSection)
//                    {
//                        case "skills": skills.AppendLine(trimmed); break;
//                        case "education": education.AppendLine(trimmed); break;
//                        case "experience": experience.AppendLine(trimmed); break;
//                        case "profile": profile.AppendLine(trimmed); break;
//                        case "objective": objective.AppendLine(trimmed); break;
//                        case "languages": languages.AppendLine(trimmed); break;
//                        case "project": project.AppendLine(trimmed); break;
//                    }
//                }

//                // Add proper spacing between sections
//                analysisResult.Skills = skills.ToString().Trim();
//                analysisResult.Education = education.ToString().Trim();
//                analysisResult.Experience = experience.ToString().Trim();

//                // Store additional sections in available fields or create new ones
//                if (!string.IsNullOrEmpty(profile.ToString().Trim()))
//                {
//                    analysisResult.OverallAnalysis = profile.ToString().Trim();
//                }

//                if (!string.IsNullOrEmpty(objective.ToString().Trim()))
//                {
//                    // You might want to add a new property for Objective in the model
//                    // For now, we'll append it to OverallAnalysis
//                    if (!string.IsNullOrEmpty(analysisResult.OverallAnalysis))
//                    {
//                        analysisResult.OverallAnalysis += "\n\nOBJECTIVE\n" + objective.ToString().Trim();
//                    }
//                    else
//                    {
//                        analysisResult.OverallAnalysis = "OBJECTIVE\n" + objective.ToString().Trim();
//                    }
//                }

//                if (!string.IsNullOrEmpty(languages.ToString().Trim()))
//                {
//                    // Store languages in KeyPhrases for now
//                    analysisResult.KeyPhrases = languages.ToString().Trim();
//                }

//                if (!string.IsNullOrEmpty(project.ToString().Trim()))
//                {
//                    // Store projects in FormFields for now
//                    analysisResult.FormFields = project.ToString().Trim();
//                }
//            }
//            // --- End Section Extraction ---

//            // TODO: Save to database
//            // TODO: Add more detailed analysis using other Azure AI services

//            return analysisResult;
//        }

//        public async Task<JobSuggestionResult> GetJobSuggestionsAsync(string skills)
//        {
//            string extractedSkills = SkillExtractor.ExtractSkillsFromText(skills); // 'skills' is raw CV text

//            var requestData = new
//            {
//                Inputs = new
//                {
//                    input1 = new[]
//                    {
//                        new { Skills = extractedSkills, JobCategory = "unknown" }
//                    }
//                }
//            };

//            var json = JsonConvert.SerializeObject(requestData);
//            Console.WriteLine("==== Job Recommendation Request JSON ====");
//            Console.WriteLine(json);
//            var client = new HttpClient();
//            var content = new StringContent(json, Encoding.UTF8, "application/json");
//            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

//            client.DefaultRequestHeaders.Authorization =
//                new AuthenticationHeaderValue("Bearer", _jobRecommendationApiKey);

//            var response = await client.PostAsync(_jobRecommendationEndpoint, content);
//            var responseBody = await response.Content.ReadAsStringAsync();

//            Console.WriteLine($"==== Job Recommendation Response Status: {response.StatusCode} ====");
//            Console.WriteLine(responseBody);

//            if (!response.IsSuccessStatusCode)
//                throw new HttpRequestException($"AzureML Error: {response.StatusCode}\n\n{responseBody}");

//            var result = new JobSuggestionResult();
//            var parsed = JObject.Parse(responseBody);

//            // Compatibility: map WebServiceOutput0 to output1 if needed
//            var results = parsed["Results"] as JObject;
//            if (results != null && results["output1"] == null && results["WebServiceOutput0"] != null)
//            {
//                results["output1"] = results["WebServiceOutput0"];
//            }

//            // Parse response safely
//            var output = parsed["Results"]?["output1"] as JArray;
//            if (output == null || output.Count == 0)
//                throw new Exception("❌ Missing or invalid 'output1' in Azure ML response.");

//            var scores = output[0] as JObject;
//            if (scores == null)
//                throw new Exception("❌ Unexpected format: first item in 'output1' is not a JSON object.");

//            foreach (var prop in scores.Properties())
//            {
//                if (prop.Name.StartsWith("Scored Probabilities_"))
//                {
//                    string jobTitle = prop.Name.Replace("Scored Probabilities_", "");
//                    double probability = prop.Value.Value<double>();

//                    if (probability >= 0.1)
//                        result.Suggestions[jobTitle] = probability;
//                }
//            }

//            return result;
//        }

//        public async Task<string> GetCVEditSuggestions(string cvContent)
//        {
//            // Chỉ sử dụng Azure OpenAI
//            if (string.IsNullOrEmpty(_openAIEndpoint) || string.IsNullOrEmpty(_openAIKey) || string.IsNullOrEmpty(_openAIDeploymentName))
//            {
//                return "Lỗi cấu hình: Thiếu thông tin Azure OpenAI. Vui lòng kiểm tra cấu hình AzureAI trong appsettings.json";
//            }

//            string endpoint = _openAIEndpoint;
//            string apiKey = _openAIKey;
//            string modelName = _openAIDeploymentName;

//            var client = _httpClientFactory.CreateClient();
//            client.Timeout = TimeSpan.FromMinutes(2); // Set timeout to 2 minutes
//            client.DefaultRequestHeaders.Clear();
//            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

//            var requestBody = new
//            {
//                model = modelName, // Sử dụng model name đã xác định
//                messages = new[]
//                {
//                    new { role = "system", content = "Bạn là chuyên gia nhân sự có kinh nghiệm 10+ năm trong lĩnh vực tuyển dụng và đánh giá CV. Hãy phân tích CV một cách chi tiết và đưa ra các đề xuất chỉnh sửa cụ thể, chuyên nghiệp. Tập trung vào:\r\n\r\n1. Cấu trúc và bố cục CV\r\n2. Nội dung và cách trình bày\r\n3. Điểm mạnh cần nhấn mạnh\r\n4. Điểm yếu cần cải thiện\r\n5. Từ khóa và kỹ năng quan trọng\r\n6. Cách viết mô tả công việc\r\n7. Định dạng và trình bày\r\n\r\nHãy đưa ra nhận xét rõ ràng, cụ thể và có thể thực hiện được. " },

//                    new { role = "user", content = cvContent }
//                },
//                max_tokens = 2000,
//                temperature = 0.7
//            };
//            var json = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
//            var content = new StringContent(json, Encoding.UTF8, "application/json");

//            // Sử dụng endpoint đã xác định

//            Console.WriteLine($"==== OpenAI Request ====");
//            Console.WriteLine($"Endpoint: {endpoint}");
//            Console.WriteLine($"Model: {modelName}");
//            Console.WriteLine($"Request JSON: {json}");

//            var response = await SendWithRetryAsync(() => client.PostAsync(endpoint, content));

//            if (response == null)
//            {
//                return "Bạn đang gửi quá nhiều yêu cầu tới AI hoặc có lỗi mạng. Vui lòng thử lại sau vài phút.";
//            }

//            Console.WriteLine($"==== OpenAI Response Status: {response.StatusCode} ====");

//            if (!response.IsSuccessStatusCode)
//            {
//                var error = await response.Content.ReadAsStringAsync();
//                Console.WriteLine($"==== OpenAI Error Response: {error} ====");

//                // Provide more specific error messages
//                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
//                {
//                    return "Lỗi xác thực: API key không hợp lệ hoặc endpoint không đúng. Vui lòng kiểm tra cấu hình OpenAI trong appsettings.json";
//                }
//                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
//                {
//                    return "Lỗi: Model hoặc deployment không tìm thấy. Vui lòng kiểm tra tên model/deployment trong cấu hình";
//                }
//                else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
//                {
//                    return "Lỗi: Quá nhiều yêu cầu. Vui lòng thử lại sau vài phút";
//                }
//                else
//                {
//                    return $"Lỗi OpenAI: {response.StatusCode} - {error}";
//                }
//            }

//            var responseString = await response.Content.ReadAsStringAsync();
//            Console.WriteLine($"==== OpenAI Success Response: {responseString} ====");

//            var doc = JObject.Parse(responseString);
//            var suggestion = doc["choices"]?[0]?["message"]?["content"]?.ToString();

//            if (string.IsNullOrEmpty(suggestion))
//            {
//                return "Không thể nhận được đề xuất từ AI. Vui lòng thử lại.";
//            }

//            return suggestion;
//        }

//        // Hàm retry request vô hạn
//        private async Task<HttpResponseMessage?> SendWithRetryAsync(Func<Task<HttpResponseMessage>> sendRequest, int delayMs = 2000)
//        {
//            while (true)
//            {
//                try
//                {
//                    var response = await sendRequest();
//                    if (response.IsSuccessStatusCode || response.StatusCode != System.Net.HttpStatusCode.TooManyRequests)
//                    {
//                        return response;
//                    }
//                }
//                catch (HttpRequestException)
//                {
//                    // Có thể log lỗi nếu cần
//                }
//                await Task.Delay(delayMs);
//            }
//        }
//    }
//}

using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using AI_CV_Analyze.Models;
using Microsoft.Extensions.Configuration;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure;
using System.IO;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace AI_CV_Analyze.Services
{
    public class ResumeAnalysisService : IResumeAnalysisService
    {
        private readonly IConfiguration _configuration;
        private readonly DocumentAnalysisClient _formRecognizerClient;
        private readonly string _formRecognizerEndpoint;
        private readonly string _formRecognizerKey;
        private readonly string _customModelId;
        private readonly string _openAIEndpoint;
        private readonly string _openAIKey;
        private readonly string _openAIDeploymentName;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _jobRecommendationEndpoint;
        private readonly string _jobRecommendationApiKey;
        private readonly ILogger<ResumeAnalysisService> _logger;

        public ResumeAnalysisService(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<ResumeAnalysisService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _formRecognizerEndpoint = _configuration["AzureAI:FormRecognizerEndpoint"] ?? throw new InvalidOperationException("FormRecognizerEndpoint is missing.");
            _formRecognizerKey = _configuration["AzureAI:FormRecognizerKey"] ?? throw new InvalidOperationException("FormRecognizerKey is missing.");
            _customModelId = _configuration["AzureAI:CustomModelId"] ?? throw new InvalidOperationException("CustomModelId is missing.");
            _openAIEndpoint = _configuration["AzureAI:OpenAIEndpoint"] ?? string.Empty;
            _openAIKey = _configuration["AzureAI:OpenAIKey"] ?? string.Empty;
            _openAIDeploymentName = _configuration["AzureAI:OpenAIDeploymentName"] ?? string.Empty;
            _jobRecommendationEndpoint = _configuration["JobRecommendation:Endpoint"] ?? string.Empty;
            _jobRecommendationApiKey = _configuration["JobRecommendation:ApiKey"] ?? string.Empty;

            _formRecognizerClient = new DocumentAnalysisClient(new Uri(_formRecognizerEndpoint), new AzureKeyCredential(_formRecognizerKey));
        }

        public async Task<ResumeAnalysisResult> AnalyzeResume(IFormFile cvFile)
        {
            if (cvFile == null || cvFile.Length == 0)
                throw new ArgumentException("CV file is null or empty.");

            using var stream = cvFile.OpenReadStream();
            var analysisResult = new ResumeAnalysisResult
            {
                AnalysisDate = DateTime.UtcNow,
                FileName = cvFile.FileName,
                AnalysisStatus = "Processing"
            };

            try
            {
                var operation = await _formRecognizerClient.AnalyzeDocumentAsync(WaitUntil.Completed, _customModelId, stream);
                var result = operation.Value;

                analysisResult.Content = result.Content;
                analysisResult.DocumentType = result.Documents.FirstOrDefault()?.DocumentType;
                analysisResult.ConfidenceScore = result.Documents.FirstOrDefault()?.Confidence.ToString();

                _logger.LogInformation("Raw content: {Content}", result.Content);
                ExtractCustomFields(result, analysisResult);
                _logger.LogInformation("After custom fields: Name={Name}, Email={Email}, Education={Education}, Skills={Skills}, Experience={Experience}",
                    analysisResult.Name, analysisResult.Email, analysisResult.Education, analysisResult.Skills, analysisResult.Experience);

                if (string.IsNullOrEmpty(analysisResult.Name) || string.IsNullOrEmpty(analysisResult.Education) ||
                    string.IsNullOrEmpty(analysisResult.Skills) || string.IsNullOrEmpty(analysisResult.Experience) ||
                    string.IsNullOrEmpty(analysisResult.Email))
                {
                    _logger.LogInformation("Falling back to raw content extraction.");
                    ExtractFromRawContent(result.Content, analysisResult);
                }

                if (string.IsNullOrEmpty(analysisResult.Name))
                {
                    analysisResult.Name = ExtractNameHeuristic(result);
                }

                ValidateAnalysisResult(analysisResult);
                _logger.LogInformation("Final extracted result: Name={Name}, Email={Email}, Education={Education}, Skills={Skills}, Experience={Experience}",
                    analysisResult.Name, analysisResult.Email, analysisResult.Education, analysisResult.Skills, analysisResult.Experience);

                analysisResult.AnalysisStatus = string.IsNullOrEmpty(analysisResult.ErrorMessage) ? "Completed" : "Failed";
            }
            catch (Exception ex)
            {
                analysisResult.AnalysisStatus = "Failed";
                analysisResult.ErrorMessage = $"Lỗi phân tích CV: {ex.Message}";
                _logger.LogError(ex, "Error analyzing resume: {Message}", ex.Message);
            }

            return analysisResult;
        }

        private void ExtractCustomFields(AnalyzeResult result, ResumeAnalysisResult analysisResult)
        {
            if (result.Documents == null || !result.Documents.Any())
            {
                _logger.LogWarning("No documents found in AnalyzeResult.");
                return;
            }

            var document = result.Documents.First();
            _logger.LogInformation("Total fields detected: {Count}", document.Fields.Count);
            foreach (var field in document.Fields)
            {
                _logger.LogInformation("Field detected: Key={Key}, Value={Value}",
                    field.Key, field.Value?.Content ?? field.Value?.ToString());
                switch (field.Key)
                {
                    case "Name":
                    case "name":
                        analysisResult.Name = field.Value?.Content ?? field.Value?.ToString();
                        break;
                    case "Email":
                    case "email":
                        analysisResult.Email = field.Value?.Content ?? field.Value?.ToString();
                        break;
                    case "Education":
                    case "education":
                    case "Học vấn":
                        analysisResult.Education = field.Value?.Content ?? field.Value?.ToString();
                        break;
                    case "Skills":
                    case "Skill":
                    case "skills":
                    case "Kỹ năng":
                        analysisResult.Skills = field.Value?.Content ?? field.Value?.ToString();
                        break;
                    case "Experience":
                    case "experience":
                    case "Kinh nghiệm":
                    case "Work Experience":
                        analysisResult.Experience = field.Value?.Content ?? field.Value?.ToString();
                        break;
                }
            }
        }

        private void ExtractFromRawContent(string? content, ResumeAnalysisResult analysisResult)
        {
            if (string.IsNullOrEmpty(content)) return;

            string[] lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            string currentSection = "";
            var skills = new StringBuilder();
            var education = new StringBuilder();
            var experience = new StringBuilder();
            int linesCollected = 0;
            const int maxLinesPerSection = 15; // Tăng giới hạn để thu thập nhiều hơn

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                if (IsSectionHeader(trimmed))
                {
                    currentSection = GetSectionName(trimmed);
                    linesCollected = 0;
                    continue;
                }

                if (!string.IsNullOrEmpty(currentSection) && linesCollected < maxLinesPerSection)
                {
                    switch (currentSection)
                    {
                        case "skills":
                            if (string.IsNullOrEmpty(analysisResult.Skills)) skills.AppendLine(trimmed);
                            break;
                        case "education":
                            if (string.IsNullOrEmpty(analysisResult.Education)) education.AppendLine(trimmed);
                            break;
                        case "experience":
                            if (string.IsNullOrEmpty(analysisResult.Experience)) experience.AppendLine(trimmed);
                            break;
                    }
                    linesCollected++;
                }
                else
                {
                    if (string.IsNullOrEmpty(analysisResult.Name) && !trimmed.Contains("@"))
                    {
                        if (trimmed.ToLower().Contains("name") || trimmed.ToLower().Contains("họ tên"))
                            analysisResult.Name = trimmed.Replace("Name:", "").Replace("Họ tên:", "").Trim();
                        else if (linesCollected == 0)
                            analysisResult.Name = trimmed;
                    }
                    if (string.IsNullOrEmpty(analysisResult.Email))
                    {
                        var emailMatch = Regex.Match(trimmed, @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b");
                        if (emailMatch.Success)
                            analysisResult.Email = emailMatch.Value;
                    }
                }
            }

            analysisResult.Skills = analysisResult.Skills ?? skills.ToString().Trim();
            analysisResult.Education = analysisResult.Education ?? education.ToString().Trim();
            analysisResult.Experience = analysisResult.Experience ?? experience.ToString().Trim();
        }

        private bool IsSectionHeader(string line)
        {
            return line.Equals("Skills", StringComparison.OrdinalIgnoreCase) ||
                   line.Equals("Skills:", StringComparison.OrdinalIgnoreCase) ||
                   line.Equals("Kỹ năng", StringComparison.OrdinalIgnoreCase) ||
                   line.Equals("Kỹ năng:", StringComparison.OrdinalIgnoreCase) ||
                   line.Equals("Education", StringComparison.OrdinalIgnoreCase) ||
                   line.Equals("Education:", StringComparison.OrdinalIgnoreCase) ||
                   line.Equals("Học vấn", StringComparison.OrdinalIgnoreCase) ||
                   line.Equals("Học vấn:", StringComparison.OrdinalIgnoreCase) ||
                   line.Equals("Experience", StringComparison.OrdinalIgnoreCase) ||
                   line.Equals("Experience:", StringComparison.OrdinalIgnoreCase) ||
                   line.Equals("Work Experience", StringComparison.OrdinalIgnoreCase) ||
                   line.Equals("Work Experience:", StringComparison.OrdinalIgnoreCase) ||
                   line.Equals("Kinh nghiệm", StringComparison.OrdinalIgnoreCase) ||
                   line.Equals("Kinh nghiệm:", StringComparison.OrdinalIgnoreCase);
        }

        private string GetSectionName(string line)
        {
            if (line.Equals("Skills", StringComparison.OrdinalIgnoreCase) || line.Equals("Skills:", StringComparison.OrdinalIgnoreCase) ||
                line.Equals("Kỹ năng", StringComparison.OrdinalIgnoreCase) || line.Equals("Kỹ năng:", StringComparison.OrdinalIgnoreCase))
                return "skills";
            if (line.Equals("Education", StringComparison.OrdinalIgnoreCase) || line.Equals("Education:", StringComparison.OrdinalIgnoreCase) ||
                line.Equals("Học vấn", StringComparison.OrdinalIgnoreCase) || line.Equals("Học vấn:", StringComparison.OrdinalIgnoreCase))
                return "education";
            if (line.Equals("Experience", StringComparison.OrdinalIgnoreCase) || line.Equals("Experience:", StringComparison.OrdinalIgnoreCase) ||
                line.Equals("Work Experience", StringComparison.OrdinalIgnoreCase) || line.Equals("Work Experience:", StringComparison.OrdinalIgnoreCase) ||
                line.Equals("Kinh nghiệm", StringComparison.OrdinalIgnoreCase) || line.Equals("Kinh nghiệm:", StringComparison.OrdinalIgnoreCase))
                return "experience";
            return "";
        }

        private string? ExtractNameHeuristic(AnalyzeResult result)
        {
            if (result.Paragraphs == null || !result.Paragraphs.Any()) return null;

            var firstParagraph = result.Paragraphs.First();
            if (firstParagraph.BoundingRegions != null && firstParagraph.BoundingRegions.Any())
            {
                var region = firstParagraph.BoundingRegions.First();
                if (region.BoundingPolygon != null && region.BoundingPolygon.Any() && region.BoundingPolygon[0].Y < 100)
                {
                    var lines = firstParagraph.Content?.Trim().Split('\n');
                    return lines?.FirstOrDefault(l => l.ToLower().Contains("name") || l.ToLower().Contains("họ tên"))?.Replace("Name:", "").Replace("Họ tên:", "").Trim() ??
                           lines?.FirstOrDefault(l => !l.Contains("@"));
                }
            }

            var contentLines = result.Content?.Split('\n');
            return contentLines?.FirstOrDefault(l => l.ToLower().Contains("name") || l.ToLower().Contains("họ tên"))?.Replace("Name:", "").Replace("Họ tên:", "").Trim() ??
                   contentLines?.FirstOrDefault(l => !l.Contains("@"));
        }

        private void ValidateAnalysisResult(ResumeAnalysisResult analysisResult)
        {
            var errors = new StringBuilder(analysisResult.ErrorMessage ?? string.Empty);
            if (string.IsNullOrEmpty(analysisResult.Name))
                errors.AppendLine("Không thể trích xuất tên ứng viên.");
            if (string.IsNullOrEmpty(analysisResult.Education))
                errors.AppendLine("Không thể trích xuất mục Education.");
            if (string.IsNullOrEmpty(analysisResult.Skills))
                errors.AppendLine("Không thể trích xuất mục Skills.");
            if (!string.IsNullOrEmpty(analysisResult.Experience) && analysisResult.Experience == analysisResult.Skills)
                errors.AppendLine("Mục Experience dường như bị trộn lẫn với Skills.");
            if (string.IsNullOrEmpty(analysisResult.Email))
                errors.AppendLine("Không thể trích xuất email.");

            analysisResult.ErrorMessage = errors.Length > 0 ? errors.ToString() : null;
        }

        public async Task<JobSuggestionResult> GetJobSuggestionsAsync(string? skills)
        {
            if (string.IsNullOrEmpty(skills))
                throw new ArgumentException("Skills cannot be null or empty.");

            try
            {
                string extractedSkills = CVSkillExtractor.ExtractSkillsFromText(skills);

                var requestData = new
                {
                    Inputs = new
                    {
                        input1 = new[]
                        {
                            new { Skills = extractedSkills, JobCategory = "unknown" }
                        }
                    }
                };

                var json = JsonConvert.SerializeObject(requestData);
                var client = _httpClientFactory.CreateClient();
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _jobRecommendationApiKey);

                var response = await client.PostAsync(_jobRecommendationEndpoint, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"AzureML Error: {response.StatusCode}\n{responseBody}");

                var result = new JobSuggestionResult();
                var parsed = JObject.Parse(responseBody);

                var results = parsed["Results"] as JObject;
                if (results != null && results["output1"] == null && results["WebServiceOutput0"] != null)
                {
                    results["output1"] = results["WebServiceOutput0"];
                }

                var output = parsed["Results"]?["output1"] as JArray;
                if (output == null || output.Count == 0)
                    throw new Exception("Missing or invalid 'output1' in Azure ML response.");

                var scores = output[0] as JObject;
                if (scores == null)
                    throw new Exception("Unexpected format: first item in 'output1' is not a JSON object.");

                foreach (var prop in scores.Properties())
                {
                    if (prop.Name.StartsWith("Scored Probabilities_"))
                    {
                        string jobTitle = prop.Name.Replace("Scored Probabilities_", "");
                        double probability = prop.Value.Value<double>();

                        if (probability >= 0.1)
                            result.Suggestions[jobTitle] = probability;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get job suggestions: {ex.Message}", ex);
            }
        }

        public async Task<string> GetCVEditSuggestions(string? cvContent)
        {
            if (string.IsNullOrEmpty(cvContent))
                return "CV content cannot be null or empty.";

            if (string.IsNullOrEmpty(_openAIEndpoint) || string.IsNullOrEmpty(_openAIKey) || string.IsNullOrEmpty(_openAIDeploymentName))
                return "Lỗi cấu hình: Thiếu thông tin Azure OpenAI.";

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromMinutes(2);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAIKey);

                var requestBody = new
                {
                    model = _openAIDeploymentName,
                    messages = new[]
                    {
                        new { role = "system", content = "Bạn là chuyên gia nhân sự có kinh nghiệm 10+ năm trong lĩnh vực tuyển dụng và đánh giá CV. Hãy phân tích CV một cách chi tiết và đưa ra các đề xuất chỉnh sửa cụ thể, chuyên nghiệp. Tập trung vào:\r\n1. Cấu trúc và bố cục CV\r\n2. Nội dung và cách trình bày\r\n3. Điểm mạnh cần nhấn mạnh\r\n4. Điểm yếu cần cải thiện\r\n5. Từ khóa và kỹ năng quan trọng\r\n6. Cách viết mô tả công việc\r\n7. Định dạng và trình bày\r\nHãy đưa ra nhận xét rõ ràng, cụ thể và có thể thực hiện được." },
                        new { role = "user", content = cvContent }
                    },
                    max_tokens = 2000,
                    temperature = 0.7
                };

                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await SendWithRetryAsync(() => client.PostAsync(_openAIEndpoint, content));

                if (response == null)
                    return "Quá nhiều yêu cầu tới AI hoặc lỗi mạng. Vui lòng thử lại sau.";

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        return "Lỗi xác thực: API key không hợp lệ hoặc endpoint không đúng.";
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        return "Lỗi: Model hoặc deployment không tìm thấy.";
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                        return "Lỗi: Quá nhiều yêu cầu. Vui lòng thử lại sau.";
                    return $"Lỗi OpenAI: {response.StatusCode} - {error}";
                }

                var responseString = await response.Content.ReadAsStringAsync();
                var doc = JObject.Parse(responseString);
                var suggestion = doc["choices"]?[0]?["message"]?["content"]?.ToString();

                return string.IsNullOrEmpty(suggestion) ? "Không thể nhận được đề xuất từ AI." : suggestion;
            }
            catch (Exception ex)
            {
                return $"Lỗi khi lấy đề xuất chỉnh sửa CV: {ex.Message}";
            }
        }

        public async Task<bool> CheckServiceStatusAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _formRecognizerKey);
                var response = await client.GetAsync($"{_formRecognizerEndpoint}/formrecognizer/info");
                if (!response.IsSuccessStatusCode) return false;

                if (!string.IsNullOrEmpty(_openAIEndpoint) && !string.IsNullOrEmpty(_openAIKey))
                {
                    client = _httpClientFactory.CreateClient();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAIKey);
                    response = await client.GetAsync($"{_openAIEndpoint}/models");
                    if (!response.IsSuccessStatusCode) return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<HttpResponseMessage?> SendWithRetryAsync(Func<Task<HttpResponseMessage>> sendRequest, int delayMs = 2000)
        {
            int maxRetries = 3;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var response = await sendRequest();
                    if (response.IsSuccessStatusCode || response.StatusCode != System.Net.HttpStatusCode.TooManyRequests)
                        return response;
                }
                catch (HttpRequestException)
                {
                }
                await Task.Delay(delayMs);
            }
            return null;
        }
    }

    public static class CVSkillExtractor
    {
        public static string ExtractSkillsFromText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var skills = new List<string> { "Java", "Python", "C#", "SQL", "HTML", "CSS", "JavaScript" };
            return string.Join(", ", skills.Where(s => text.ToLower().Contains(s.ToLower())));
        }
    }
}