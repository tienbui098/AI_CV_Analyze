using Microsoft.Extensions.Configuration;

namespace AI_CV_Analyze.Services.Utilities
{
    public static class OpenAIConfiguration
    {
        public static (string Endpoint, string Key, string DeploymentName) GetOpenAIConfig(IConfiguration configuration)
        {
            return (
                configuration["AzureAI:OpenAIEndpoint"],
                configuration["AzureAI:OpenAIKey"],
                configuration["AzureAI:OpenAIDeploymentName"]
            );
        }

        public static bool IsOpenAIConfigured(IConfiguration configuration)
        {
            var (endpoint, key, deployment) = GetOpenAIConfig(configuration);
            return !string.IsNullOrEmpty(endpoint) && 
                   !string.IsNullOrEmpty(key) && 
                   !string.IsNullOrEmpty(deployment);
        }
    }
} 