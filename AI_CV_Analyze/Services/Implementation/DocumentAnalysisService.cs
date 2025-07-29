using AI_CV_Analyze.Configuration;
using AI_CV_Analyze.Models;
using AI_CV_Analyze.Services.Interfaces;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI_CV_Analyze.Services.Implementation
{
    public class DocumentAnalysisService : IDocumentAnalysisService
    {
        private readonly DocumentAnalysisClient? _formRecognizerClient;
        private readonly string _customModelId;

        public DocumentAnalysisService(IConfiguration configuration)
        {
            var formRecognizerEndpoint = configuration["AzureAI:FormRecognizerEndpoint"];
            var formRecognizerKey = configuration["AzureAI:FormRecognizerKey"];
            _customModelId = configuration["AzureAI:CustomModelId"];

            // Initialize FormRecognizer client
            if (!string.IsNullOrEmpty(formRecognizerEndpoint) && !string.IsNullOrEmpty(formRecognizerKey))
            {
                _formRecognizerClient = new DocumentAnalysisClient(new Uri(formRecognizerEndpoint), new AzureKeyCredential(formRecognizerKey));
            }
        }

        public async Task<ResumeAnalysisResult> AnalyzeDocumentAsync(Stream documentStream)
        {
            var analysisResult = new ResumeAnalysisResult
            {
                AnalysisDate = DateTime.UtcNow,
                AnalysisStatus = "Pending"
            };

            try
            {
                if (_formRecognizerClient == null)
                {
                    analysisResult.AnalysisStatus = "Failed";
                    analysisResult.ErrorMessage = "FormRecognizer client is not configured.";
                    return analysisResult;
                }

                if (string.IsNullOrEmpty(_customModelId))
                {
                    analysisResult.AnalysisStatus = "Failed";
                    analysisResult.ErrorMessage = "Custom model ID is not configured.";
                    return analysisResult;
                }

                var operation = await _formRecognizerClient.AnalyzeDocumentAsync(WaitUntil.Completed, _customModelId, documentStream);
                var result = operation.Value;

                analysisResult.Content = result.Content;
                ExtractCustomFields(result, analysisResult);
                analysisResult.AnalysisStatus = string.IsNullOrEmpty(analysisResult.ErrorMessage) ? "Completed" : "Failed";
            }
            catch (RequestFailedException ex)
            {
                analysisResult.AnalysisStatus = "Failed";
                analysisResult.ErrorMessage = $"Document Intelligence error: {ex.Message} (Status Code: {ex.Status})";
            }
            catch (Exception ex)
            {
                analysisResult.AnalysisStatus = "Failed";
                analysisResult.ErrorMessage = $"Analysis error: {ex.Message}";
            }

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
                analysisResult.ErrorMessage = "No fields could be extracted from model.";
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
                        return null;

                    default:
                        return field.Content;
                }
            }
            catch
            {
                return null;
            }
        }
    }
} 