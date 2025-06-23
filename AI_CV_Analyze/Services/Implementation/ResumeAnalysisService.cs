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