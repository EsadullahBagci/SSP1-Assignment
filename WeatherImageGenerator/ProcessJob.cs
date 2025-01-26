using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace WeatherImageGenerator
{
    public static class ProcessJob
    {
        private static readonly HttpClient httpClient = new HttpClient();

        [Function("ProcessJob")]
        [QueueOutput("image-jobs", Connection = "AzureWebJobsStorage")]
        public static async Task<string[]> Run(
            [QueueTrigger("weather-jobs", Connection = "AzureWebJobsStorage")] string jobData,
            FunctionContext context)
        {
            var logger = context.GetLogger("ProcessJob");
            logger.LogInformation($"Processing job: {jobData}");

            var job = JsonConvert.DeserializeObject<dynamic>(jobData);
            string jobId = job.JobId;

            await status.TableStorage.SaveJobStatusAsync(jobId, "Processing");

            try
            {
                string weatherApiUrl = "https://data.buienradar.nl/2.0/feed/json";
                var weatherResponse = await httpClient.GetStringAsync(weatherApiUrl);
                var weatherData = JsonConvert.DeserializeObject<dynamic>(weatherResponse);

                var imageJobs = new List<string>();

                foreach (var station in weatherData.actual.stationmeasurements)
                {
                    string stationData = JsonConvert.SerializeObject(new
                    {
                        JobId = jobId,
                        StationName = station.stationname,
                        Temperature = station.temperature,
                        WeatherDescription = station.weatherdescription
                    });

                    imageJobs.Add(stationData);
                }

                logger.LogInformation($"Queued {imageJobs.Count} tasks for image generation.");
                await status.TableStorage.SaveJobStatusAsync(jobId, "Completed");
                return imageJobs.ToArray();

            }
            catch (Exception ex)
            {
                logger.LogError($"An error occurred while processing job {jobId}: {ex.Message}");
                await status.TableStorage.SaveJobStatusAsync(jobId, "Failed");
                return null;
            }
        }
    }
}
