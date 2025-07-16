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
        private readonly ApplicationDbContext _dbContext;


        public ResumeAnalysisService(IConfiguration configuration, IHttpClientFactory httpClientFactory, ApplicationDbContext dbContext)
        {
            _configuration = configuration;
            _formRecognizerEndpoint = _configuration["AzureAI:FormRecognizerEndpoint"];
            _formRecognizerKey = _configuration["AzureAI:FormRecognizerKey"];
            _formRecognizerClient = new DocumentAnalysisClient(new Uri(_formRecognizerEndpoint), new AzureKeyCredential(_formRecognizerKey));
            _openAIEndpoint = _configuration["AzureAI:OpenAIEndpoint"];
            _openAIKey = _configuration["AzureAI:OpenAIKey"];
            _openAIDeploymentName = _configuration["AzureAI:OpenAIDeploymentName"];
            _httpClientFactory = httpClientFactory;
            _customModelId = _configuration["AzureAI:CustomModelId"];
            _jobRecommendationEndpoint = _configuration["JobRecommendation:Endpoint"];
            _jobRecommendationApiKey = _configuration["JobRecommendation:ApiKey"];
            _dbContext = dbContext;

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
        }

        // Hàm gọi Azure OpenAI để chấm điểm từng tiêu chí
        public async Task<(int Layout, int Skill, int Experience, int Education, int Keyword, int Format, string RawJson)> AnalyzeScoreWithOpenAI(string cvContent)
        {
            if (string.IsNullOrWhiteSpace(cvContent))
                return (0, 0, 0, 0, 0, 0, "");

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

            var response = await SendWithRetryAsync(() => client.PostAsync(endpoint, content));
            if (response == null)
                throw new Exception("Không nhận được phản hồi từ AI");
            var responseString = await response.Content.ReadAsStringAsync();
            var doc = JObject.Parse(responseString);
            var answer = doc["choices"]?[0]?["message"]?["content"]?.ToString();
            if (string.IsNullOrWhiteSpace(answer))
                throw new Exception("AI không trả về kết quả chấm điểm");
            // Tìm JSON trong answer
            string jsonScore = answer.Trim();
            int start = jsonScore.IndexOf('{');
            int end = jsonScore.LastIndexOf('}');
            if (start >= 0 && end > start)
                jsonScore = jsonScore.Substring(start, end - start + 1);
            var scoreObj = JObject.Parse(jsonScore);
            int layout = scoreObj.Value<int?>("layout") ?? 0;
            int skill = scoreObj.Value<int?>("skill") ?? 0;
            int experience = scoreObj.Value<int?>("experience") ?? 0;
            int education = scoreObj.Value<int?>("education") ?? 0;
            int keyword = scoreObj.Value<int?>("keyword") ?? 0;
            int format = scoreObj.Value<int?>("format") ?? 0;
            return (layout, skill, experience, education, keyword, format, jsonScore);
        }

        public async Task<ResumeAnalysisResult> AnalyzeResume(IFormFile cvFile, int userId, bool skipDb = false)
        {
            if (cvFile == null || cvFile.Length == 0)
                throw new ArgumentException("CV file is null or empty.", nameof(cvFile));

            var supportedTypes = new[] { "application/pdf", "image/png", "image/jpeg" };
            if (!supportedTypes.Contains(cvFile.ContentType.ToLowerInvariant()))
                throw new ArgumentException($"Unsupported file type. Supported types are: {string.Join(", ", supportedTypes)}", nameof(cvFile));

            // Đọc file thành byte[]
            byte[] fileBytes;
            using (var ms = new MemoryStream())
            {
                await cvFile.CopyToAsync(ms);
                fileBytes = ms.ToArray();
            }

            // Xác định loại file (PDF/JPG/PNG)
            string ext = Path.GetExtension(cvFile.FileName)?.ToLower();
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
                    FileName = cvFile.FileName,
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
            if (cvFile.ContentType == "application/pdf")
            {
                using var originalStream = new MemoryStream(fileBytes);
                processedStream = await PdfPreprocessor.ConvertMultiPagePdfToSingleImageAsync(originalStream);
            }
            else
            {
                processedStream = new MemoryStream(fileBytes);
            }

            var analysisResult = new ResumeAnalysisResult
            {
                AnalysisDate = DateTime.UtcNow,
                FileName = cvFile.FileName,
                AnalysisStatus = "Pending",
                ResumeId = resumeId // Gán ResumeId nếu có, nếu không thì = 0
            };

            try
            {
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
                }
            }

            if (string.IsNullOrEmpty(analysisResult.Name) && string.IsNullOrEmpty(analysisResult.Email) &&
                string.IsNullOrEmpty(analysisResult.Skills) && string.IsNullOrEmpty(analysisResult.Experience) &&
                string.IsNullOrEmpty(analysisResult.Project) && string.IsNullOrEmpty(analysisResult.Education))
            {
                analysisResult.ErrorMessage = "Không trích xuất được bất kỳ trường nào (Name, Email, Skills, Experience, Projects, Education) từ mô hình.";
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
                        return field.Value.AsString();

                    case DocumentFieldType.List:
                        var sb = new StringBuilder();
                        foreach (var item in field.Value.AsList())
                        {
                            sb.AppendLine(GetFieldValue(item));
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



        public async Task<JobSuggestionResult> GetJobSuggestionsAsync(string skills)
        {
            string extractedSkills = SkillExtractor.ExtractSkillsFromText(skills); // 'skills' is raw CV text

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
            Console.WriteLine("==== Job Recommendation Request JSON ====");
            Console.WriteLine(json);
            var client = new HttpClient();
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _jobRecommendationApiKey);

            var response = await client.PostAsync(_jobRecommendationEndpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"==== Job Recommendation Response Status: {response.StatusCode} ====");
            Console.WriteLine(responseBody);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"AzureML Error: {response.StatusCode}\n\n{responseBody}");

            var result = new JobSuggestionResult();
            var parsed = JObject.Parse(responseBody);

            // Compatibility: map WebServiceOutput0 to output1 if needed
            var results = parsed["Results"] as JObject;
            if (results != null && results["output1"] == null && results["WebServiceOutput0"] != null)
            {
                results["output1"] = results["WebServiceOutput0"];
            }

            // Parse response safely
            var output = parsed["Results"]?["output1"] as JArray;
            if (output == null || output.Count == 0)
                throw new Exception("❌ Missing or invalid 'output1' in Azure ML response.");

            var scores = output[0] as JObject;
            if (scores == null)
                throw new Exception("❌ Unexpected format: first item in 'output1' is not a JSON object.");

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

        public async Task<string> GetCVEditSuggestions(string cvContent)
        {
            // Chỉ sử dụng Azure OpenAI
            if (string.IsNullOrEmpty(_openAIEndpoint) || string.IsNullOrEmpty(_openAIKey) || string.IsNullOrEmpty(_openAIDeploymentName))
            {
                return "Lỗi cấu hình: Thiếu thông tin Azure OpenAI. Vui lòng kiểm tra cấu hình AzureAI trong appsettings.json";
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
                model = modelName, // Sử dụng model name đã xác định
                messages = new[]
                {
                            new { role = "system", content = "Bạn là chuyên gia nhân sự có kinh nghiệm 10+ năm trong lĩnh vực tuyển dụng và đánh giá CV. Hãy phân tích CV một cách chi tiết và đưa ra các đề xuất chỉnh sửa cụ thể, chuyên nghiệp. Tập trung vào:\r\n\r\n1. Cấu trúc và bố cục CV\r\n2. Nội dung và cách trình bày\r\n3. Điểm mạnh cần nhấn mạnh\r\n4. Điểm yếu cần cải thiện\r\n5. Từ khóa và kỹ năng quan trọng\r\n6. Cách viết mô tả công việc\r\n7. Định dạng và trình bày\r\n\r\nHãy đưa ra nhận xét rõ ràng, cụ thể và có thể thực hiện được. " },

                            new { role = "user", content = cvContent }
                        },
                max_tokens = 2000,
                temperature = 0.7
            };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Sử dụng endpoint đã xác định

            Console.WriteLine($"==== OpenAI Request ====");
            Console.WriteLine($"Endpoint: {endpoint}");
            Console.WriteLine($"Model: {modelName}");
            Console.WriteLine($"Request JSON: {json}");

            var response = await SendWithRetryAsync(() => client.PostAsync(endpoint, content));

            if (response == null)
            {
                return "Bạn đang gửi quá nhiều yêu cầu tới AI hoặc có lỗi mạng. Vui lòng thử lại sau vài phút.";
            }

            Console.WriteLine($"==== OpenAI Response Status: {response.StatusCode} ====");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"==== OpenAI Error Response: {error} ====");

                // Provide more specific error messages
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return "Lỗi xác thực: API key không hợp lệ hoặc endpoint không đúng. Vui lòng kiểm tra cấu hình OpenAI trong appsettings.json";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return "Lỗi: Model hoặc deployment không tìm thấy. Vui lòng kiểm tra tên model/deployment trong cấu hình";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    return "Lỗi: Quá nhiều yêu cầu. Vui lòng thử lại sau vài phút";
                }
                else
                {
                    return $"Lỗi OpenAI: {response.StatusCode} - {error}";
                }
            }

            var responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"==== OpenAI Success Response: {responseString} ====");

            var doc = JObject.Parse(responseString);
            var suggestion = doc["choices"]?[0]?["message"]?["content"]?.ToString();

            if (string.IsNullOrEmpty(suggestion))
            {
                return "Không thể nhận được đề xuất từ AI. Vui lòng thử lại.";
            }

            return suggestion;
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