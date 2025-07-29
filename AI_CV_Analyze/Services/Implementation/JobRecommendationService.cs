using AI_CV_Analyze.Models;
using AI_CV_Analyze.Services.Interfaces;
using AI_CV_Analyze.Services.Utilities;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AI_CV_Analyze.Services.Implementation
{
    public class JobRecommendationService : IJobRecommendationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _openAIEndpoint;
        private readonly string _openAIKey;
        private readonly string _openAIDeploymentName;

        public JobRecommendationService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            var (endpoint, key, deployment) = OpenAIConfiguration.GetOpenAIConfig(configuration);
            _openAIEndpoint = endpoint;
            _openAIKey = key;
            _openAIDeploymentName = deployment;
        }

        public async Task<JobSuggestionResult> GetJobSuggestionsAsync(string skills, string workExperience)
        {
            if (string.IsNullOrEmpty(_openAIEndpoint) || string.IsNullOrEmpty(_openAIKey) || string.IsNullOrEmpty(_openAIDeploymentName))
            {
                return new JobSuggestionResult { ImprovementPlan = "Configuration error: Missing Azure OpenAI information." };
            }

            if (string.IsNullOrWhiteSpace(skills))
            {
                return new JobSuggestionResult { ImprovementPlan = "Error: Skills parameter is required." };
            }

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(2);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAIKey);

            string prompt = $@"Given the following skills and work/project experience, recommend the most suitable job. Return only:
- The recommended job title
- The match percentage (integer, 0-100)
- The most important skill to improve for a better match (if any; if none, say 'None' and percentage is 100%)

Format:
Recommended Job: <job title>
Match Percentage: <number>%
Skill to Improve: <skill or 'None'>

Skills: {skills}
Work/Project Experience: {workExperience ?? "None"}";

            var requestBody = new
            {
                model = _openAIDeploymentName,
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
                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await HttpRetryHelper.SendWithRetryAsync(() => client.PostAsync(_openAIEndpoint, content));
                if (response == null)
                {
                    return new JobSuggestionResult { ImprovementPlan = "You are sending too many requests to the AI or there is a network error. Please try again in a few minutes." };
                }

                var responseString = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseString))
                {
                    return new JobSuggestionResult { ImprovementPlan = "Empty response received from AI." };
                }

                JObject doc = JObject.Parse(responseString);
                var answer = doc["choices"]?[0]?["message"]?["content"]?.ToString();
                if (string.IsNullOrWhiteSpace(answer))
                {
                    return new JobSuggestionResult { ImprovementPlan = "No suggestions were received from AI. Please try again." };
                }

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
                    result.MatchedSkills = null;
                    result.ImprovementPlan = null;
                }
                catch
                {
                    result.ImprovementPlan = answer;
                }

                return result;
            }
            catch (Exception ex)
            {
                return new JobSuggestionResult { ImprovementPlan = $"Error getting job suggestions: {ex.Message}" };
            }
        }


    }
} 