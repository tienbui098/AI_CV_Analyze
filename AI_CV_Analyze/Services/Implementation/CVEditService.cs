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
    public class CVEditService : ICVEditService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _openAIEndpoint;
        private readonly string _openAIKey;
        private readonly string _openAIDeploymentName;

        public CVEditService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            var (endpoint, key, deployment) = OpenAIConfiguration.GetOpenAIConfig(configuration);
            _openAIEndpoint = endpoint;
            _openAIKey = key;
            _openAIDeploymentName = deployment;
        }

        public async Task<string> GetCVEditSuggestionsAsync(string cvContent)
        {
            if (string.IsNullOrEmpty(_openAIEndpoint) || string.IsNullOrEmpty(_openAIKey) || string.IsNullOrEmpty(_openAIDeploymentName))
            {
                return "Configuration error: Missing Azure OpenAI information.";
            }

            if (string.IsNullOrWhiteSpace(cvContent))
            {
                return "Error: CV content is empty or null.";
            }

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(2);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAIKey);

            var requestBody = new
            {
                model = _openAIDeploymentName,
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
                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await HttpRetryHelper.SendWithRetryAsync(() => client.PostAsync(_openAIEndpoint, content));
                if (response == null)
                {
                    return "You are sending too many requests to the AI or there is a network error. Please try again in a few minutes.";
                }

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return $"OpenAI Error: {response.StatusCode} - {error}";
                }

                var responseString = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseString))
                {
                    return "Empty response received from AI.";
                }

                JObject doc = JObject.Parse(responseString);
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

        public async Task<string> GenerateFinalCVAsync(string cvContent, string suggestions)
        {
            if (string.IsNullOrEmpty(_openAIEndpoint) || string.IsNullOrEmpty(_openAIKey) || string.IsNullOrEmpty(_openAIDeploymentName))
            {
                return "Configuration error: Missing Azure OpenAI information.";
            }

            if (string.IsNullOrWhiteSpace(cvContent))
            {
                return "Error: CV content is empty or null.";
            }

            if (string.IsNullOrWhiteSpace(suggestions))
            {
                return "Error: Suggestions are empty or null.";
            }

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(2);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAIKey);

            string prompt = $@"You are an HR expert. Please revise the following CV according to the suggestions to create a complete, professional, and optimized version. Only return the edited CV content without any additional explanations.Edit suggestions: {suggestions} Original CV: {cvContent}";

            var requestBody = new
            {
                model = _openAIDeploymentName,
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
                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await HttpRetryHelper.SendWithRetryAsync(() => client.PostAsync(_openAIEndpoint, content));
                if (response == null)
                    return "No response received from AI.";

                var responseString = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseString))
                    return "Empty response received from AI.";

                JObject doc = JObject.Parse(responseString);
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


    }
} 