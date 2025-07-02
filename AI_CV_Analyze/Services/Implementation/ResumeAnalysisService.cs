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
using System.Text.Json;

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

        public async Task<string> GetCVEditSuggestions(string cvContent)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAIKey);
            client.DefaultRequestHeaders.Add("api-key", _openAIKey);
            var requestBody = new
            {
                messages = new[]
                {
                    new { role = "system", content = "Bạn là chuyên gia nhân sự, hãy đề xuất chỉnh sửa CV chuyên nghiệp, ngắn gọn, rõ ràng, tập trung vào điểm mạnh và loại bỏ điểm yếu.Hãy nhận xét một cách rõ ràng nhất." },
                    new { role = "user", content = cvContent }
                }
            };
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(_openAIEndpoint, content);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseString);
            var suggestion = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            return suggestion;
        }
    }
} 