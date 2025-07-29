using AI_CV_Analyze.Models.DTOs;
using AI_CV_Analyze.Services.Interfaces;
using AI_CV_Analyze.Services.Utilities;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AI_CV_Analyze.Services.Implementation
{
    public class ScoreAnalysisService : IScoreAnalysisService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _openAIEndpoint;
        private readonly string _openAIKey;
        private readonly string _openAIDeploymentName;

        public ScoreAnalysisService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            var (endpoint, key, deployment) = OpenAIConfiguration.GetOpenAIConfig(configuration);
            _openAIEndpoint = endpoint;
            _openAIKey = key;
            _openAIDeploymentName = deployment;
        }

        public async Task<ScoreAnalysisDto> AnalyzeScoreAsync(string cvContent)
        {
            if (string.IsNullOrWhiteSpace(cvContent))
                return new ScoreAnalysisDto();

            if (string.IsNullOrEmpty(_openAIEndpoint) || string.IsNullOrEmpty(_openAIKey) || string.IsNullOrEmpty(_openAIDeploymentName))
            {
                return new ScoreAnalysisDto { RawJson = "Configuration error: Missing Azure OpenAI information" };
            }

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(2);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAIKey);

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
                model = _openAIDeploymentName,
                messages = new[]
                {
                    new { role = "system", content = "Bạn là chuyên gia nhân sự, hãy chấm điểm CV theo các tiêu chí và trả về JSON." },
                    new { role = "user", content = prompt }
                },
                max_tokens = 300,
                temperature = 0.2
            };

            try
            {
                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await HttpRetryHelper.SendWithRetryAsync(() => client.PostAsync(_openAIEndpoint, content));
                if (response == null)
                    return new ScoreAnalysisDto { RawJson = "Không nhận được phản hồi từ AI" };

                var responseString = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseString))
                    return new ScoreAnalysisDto { RawJson = "Phản hồi từ AI rỗng" };

                JObject doc = JObject.Parse(responseString);
                var answer = doc["choices"]?[0]?["message"]?["content"]?.ToString();
                if (string.IsNullOrWhiteSpace(answer))
                    return new ScoreAnalysisDto { RawJson = "AI không trả về kết quả chấm điểm" };

                // Tìm JSON trong answer
                string jsonScore = answer.Trim();
                int start = jsonScore.IndexOf('{');
                int end = jsonScore.LastIndexOf('}');
                if (start >= 0 && end > start)
                    jsonScore = jsonScore.Substring(start, end - start + 1);

                JObject scoreObj = JObject.Parse(jsonScore);
                return new ScoreAnalysisDto
                {
                    Layout = scoreObj.Value<int?>("layout") ?? 0,
                    Skill = scoreObj.Value<int?>("skill") ?? 0,
                    Experience = scoreObj.Value<int?>("experience") ?? 0,
                    Education = scoreObj.Value<int?>("education") ?? 0,
                    Keyword = scoreObj.Value<int?>("keyword") ?? 0,
                    Format = scoreObj.Value<int?>("format") ?? 0,
                    RawJson = jsonScore
                };
            }
            catch (Exception ex)
            {
                return new ScoreAnalysisDto { RawJson = $"Lỗi khi chấm điểm: {ex.Message}" };
            }
        }


    }
} 