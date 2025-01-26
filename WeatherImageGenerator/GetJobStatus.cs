using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System;

namespace WeatherImageGenerator
{
    public static class GetJobStatus
    {
        [Function("GetJobStatus")]
        public static async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "JobStatus/{jobId}")] HttpRequestData req,
            string jobId,
            FunctionContext context)
        {
            var logger = context.GetLogger("GetJobStatus");
            logger.LogInformation($"Fetching status for job: {jobId}");

            try
            {
                var jobStatus = await status.TableStorage.GetJobStatusAsync(jobId);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    JobId = jobStatus.RowKey,
                    Status = jobStatus.Status,
                    Timestamp = jobStatus.Timestamp
                });

                return response;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error fetching status for job {jobId}: {ex.Message}");
                var response = req.CreateResponse(HttpStatusCode.NotFound);
                await response.WriteStringAsync($"Job with ID {jobId} not found.");
                return response;
            }
        }
    }
}
