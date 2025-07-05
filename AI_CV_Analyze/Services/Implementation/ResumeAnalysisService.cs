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

namespace AI_CV_Analyze.Services
{
    public class ResumeAnalysisService : IResumeAnalysisService
    {
        private readonly IConfiguration _configuration;
        private readonly DocumentAnalysisClient _formRecognizerClient;
        private readonly string _formRecognizerEndpoint;
        private readonly string _formRecognizerKey;
        private readonly string _openAIEndpoint;
        private readonly string _openAIKey;
        private readonly string _openAIDeploymentName;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _jobRecommendationEndpoint;
        private readonly string _jobRecommendationApiKey;

        public ResumeAnalysisService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _formRecognizerEndpoint = _configuration["AzureAI:FormRecognizerEndpoint"];
            _formRecognizerKey = _configuration["AzureAI:FormRecognizerKey"];
            _formRecognizerClient = new DocumentAnalysisClient(new Uri(_formRecognizerEndpoint), new AzureKeyCredential(_formRecognizerKey));
            _openAIEndpoint = _configuration["AzureAI:OpenAIEndpoint"];
            _openAIKey = _configuration["AzureAI:OpenAIKey"];
            _openAIDeploymentName = _configuration["AzureAI:OpenAIDeploymentName"];
            _httpClientFactory = httpClientFactory;
            
            // Read job recommendation settings from configuration
            _jobRecommendationEndpoint = _configuration["JobRecommendation:Endpoint"];
            _jobRecommendationApiKey = _configuration["JobRecommendation:ApiKey"];
        }

        public async Task<ResumeAnalysisResult> AnalyzeResume(IFormFile cvFile)
        {
            using var stream = cvFile.OpenReadStream();
            var operation = await _formRecognizerClient.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-document", stream);
            var result = operation.Value;

            var analysisResult = new ResumeAnalysisResult
            {
                Content = result.Content,
                // Add more properties based on the analysis results
                AnalysisDate = DateTime.UtcNow,
                FileName = cvFile.FileName
            };

            // --- Section Extraction Logic ---
            if (!string.IsNullOrEmpty(result.Content))
            {
                string[] lines = result.Content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                string currentSection = "";
                var skills = new StringBuilder();
                var education = new StringBuilder();
                var experience = new StringBuilder();
                var profile = new StringBuilder();
                var objective = new StringBuilder();
                var languages = new StringBuilder();
                var project = new StringBuilder();
                // Optionally: add more sections (profile, languages, etc.)

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.ToUpper().StartsWith("SKILLS")) { currentSection = "skills"; continue; }
                    if (trimmed.ToUpper().StartsWith("EDUCATION")) { currentSection = "education"; continue; }
                    if (trimmed.ToUpper().StartsWith("EXPERIENCE") || trimmed.ToUpper().StartsWith("WORK EXPERIENCE")) { currentSection = "experience"; continue; }
                    if (trimmed.ToUpper().StartsWith("PROFILE")) { currentSection = "profile"; continue; }
                    if (trimmed.ToUpper().StartsWith("OBJECTIVE")) { currentSection = "objective"; continue; }
                    if (trimmed.ToUpper().StartsWith("LANGUAGES")) { currentSection = "languages"; continue; }
                    if (trimmed.ToUpper().StartsWith("PROJECT")) { currentSection = "project"; continue; }
                    // Optionally: add more section headers

                    switch (currentSection)
                    {
                        case "skills": skills.AppendLine(trimmed); break;
                        case "education": education.AppendLine(trimmed); break;
                        case "experience": experience.AppendLine(trimmed); break;
                        case "profile": profile.AppendLine(trimmed); break;
                        case "objective": objective.AppendLine(trimmed); break;
                        case "languages": languages.AppendLine(trimmed); break;
                        case "project": project.AppendLine(trimmed); break;
                    }
                }

                // Add proper spacing between sections
                analysisResult.Skills = skills.ToString().Trim();
                analysisResult.Education = education.ToString().Trim();
                analysisResult.Experience = experience.ToString().Trim();

                // Store additional sections in available fields or create new ones
                if (!string.IsNullOrEmpty(profile.ToString().Trim()))
                {
                    analysisResult.OverallAnalysis = profile.ToString().Trim();
                }

                if (!string.IsNullOrEmpty(objective.ToString().Trim()))
                {
                    // You might want to add a new property for Objective in the model
                    // For now, we'll append it to OverallAnalysis
                    if (!string.IsNullOrEmpty(analysisResult.OverallAnalysis))
                    {
                        analysisResult.OverallAnalysis += "\n\nOBJECTIVE\n" + objective.ToString().Trim();
                    }
                    else
                    {
                        analysisResult.OverallAnalysis = "OBJECTIVE\n" + objective.ToString().Trim();
                    }
                }

                if (!string.IsNullOrEmpty(languages.ToString().Trim()))
                {
                    // Store languages in KeyPhrases for now
                    analysisResult.KeyPhrases = languages.ToString().Trim();
                }

                if (!string.IsNullOrEmpty(project.ToString().Trim()))
                {
                    // Store projects in FormFields for now
                    analysisResult.FormFields = project.ToString().Trim();
                }
            }
            // --- End Section Extraction ---

            // TODO: Save to database
            // TODO: Add more detailed analysis using other Azure AI services

            return analysisResult;
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
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAIKey);

            var requestBody = new
            {
                model = "gpt-3.5-turbo", // hoặc model bạn được cấp phép
                messages = new[]
                {
                    new { role = "system", content = "Bạn là chuyên gia nhân sự, hãy đề xuất chỉnh sửa CV chuyên nghiệp, ngắn gọn, rõ ràng, tập trung vào điểm mạnh và loại bỏ điểm yếu. Hãy nhận xét một cách rõ ràng nhất." },
                    new { role = "user", content = cvContent }
                }
            };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");


            var endpoint = string.IsNullOrEmpty(_openAIEndpoint) ? "https://api.openai.com/v1/chat/completions" : _openAIEndpoint;

            var response = await SendWithRetryAsync(() => client.PostAsync(endpoint, content));

            if (response == null)
            {
                return "Bạn đang gửi quá nhiều yêu cầu tới AI hoặc có lỗi mạng. Vui lòng thử lại sau vài phút.";
            }
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return $"Lỗi OpenAI: {response.StatusCode} - {error}";
            }

            var responseString = await response.Content.ReadAsStringAsync();
            var doc = JObject.Parse(responseString);
            var suggestion = doc["choices"][0]["message"]["content"].ToString();
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