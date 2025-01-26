using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Microsoft.Extensions.Logging;

namespace WeatherImageGenerator
{
    public static class GetImages
    {
        private static readonly HttpClient httpClient = new HttpClient();

        [Function("GetImages")]
        public static async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "GetImages/{jobId}")] HttpRequestData req,
            string query,
            FunctionContext context)
        {
            var logger = context.GetLogger("GetImages");
            logger.LogInformation($"GetImages triggered with query: {query}");

            string unsplashApiKey = Environment.GetEnvironmentVariable("unsplashApiKey");
            string unsplashUrl = $"https://api.unsplash.com/search/photos?query={query}&client_id={unsplashApiKey}";

            try
            {
                var response = await httpClient.GetAsync(unsplashUrl);

                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    logger.LogError("Unsplash API rate limit reached.");
                    var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.Forbidden);
                    await errorResponse.WriteAsJsonAsync(new { error = "Unsplash API rate limit reached. Please try again later." });
                    return errorResponse;
                }

                response.EnsureSuccessStatusCode();

                string jsonResponse = await response.Content.ReadAsStringAsync();
                var images = JsonConvert.DeserializeObject<object>(jsonResponse);

                var successResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await successResponse.WriteAsJsonAsync(images);
                return successResponse;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error fetching images from Unsplash: {ex.Message}");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = "An error occurred while fetching images. Please try again later." });
                return errorResponse;
            }
        }
    }
}
