using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace AI_CV_Analyze.Services.Utilities
{
    public static class HttpRetryHelper
    {
        public static async Task<HttpResponseMessage?> SendWithRetryAsync(Func<Task<HttpResponseMessage>> sendRequest, int delayMs = 2000)
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
                    // Log error if needed
                }
                await Task.Delay(delayMs);
            }
        }
    }
} 