using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;

namespace WeatherImageGenerator
{
    public static class StartJob
    {
        [Function("StartJob")]
        [QueueOutput("weather-jobs", Connection = "AzureWebJobsStorage")]
        public static async Task<string> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("StartJob");
            logger.LogInformation("StartJob triggered");

            string jobId = Guid.NewGuid().ToString();

            await status.TableStorage.SaveJobStatusAsync(jobId, "Pending");
            var jobData = new { JobId = jobId, Status = "Pending" };
            string serializedJobData = JsonConvert.SerializeObject(jobData);

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteAsJsonAsync(new { JobId = jobId, StatusCheckUrl = $"/api/JobStatus/{jobId}" });

            return serializedJobData;
        }
    }
}