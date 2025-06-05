using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using AI_CV_Analyze.Models;
using Microsoft.Extensions.Configuration;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure;
using System.IO;
using System;

namespace AI_CV_Analyze.Services
{
    public class ResumeAnalysisService : IResumeAnalysisService
    {
        private readonly IConfiguration _configuration;
        private readonly DocumentAnalysisClient _formRecognizerClient;
        private readonly string _formRecognizerEndpoint;
        private readonly string _formRecognizerKey;

        public ResumeAnalysisService(IConfiguration configuration)
        {
            _configuration = configuration;
            _formRecognizerEndpoint = _configuration["AzureAI:FormRecognizerEndpoint"];
            _formRecognizerKey = _configuration["AzureAI:FormRecognizerKey"];
            _formRecognizerClient = new DocumentAnalysisClient(new Uri(_formRecognizerEndpoint), new AzureKeyCredential(_formRecognizerKey));
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
    }
} 