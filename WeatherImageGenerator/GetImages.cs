using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Azure.Storage.Blobs;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using Microsoft.Extensions.Logging;
using Azure;

namespace WeatherImageGenerator
{
    public static class GetImages
    {
        [Function("GetImages")]
        public static async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetImages/{jobId}")] HttpRequestData req,
            string jobId,
            FunctionContext context)
        {
            var logger = context.GetLogger("GetImages");
            logger.LogInformation($"GetImages triggered with JobId: {jobId}");

            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("weather-images");


            if (!await containerClient.ExistsAsync())
            {
                logger.LogWarning($"Blob container 'weather-images' does not exist.");
                var notFoundResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new { error = "No images found for the specified JobId." });
                return notFoundResponse;
            }

            var imageUrls = new List<string>();
            var blobs = containerClient.GetBlobsAsync(prefix: $"{jobId}/").GetAsyncEnumerator();

            try
            {
                while (await blobs.MoveNextAsync())
                {
                    var blob = blobs.Current;
                    BlobClient blobClient = containerClient.GetBlobClient(blob.Name);
                    imageUrls.Add(blobClient.Uri.ToString());
                }
            }
            finally
            {
                await blobs.DisposeAsync(); 
            }

            if (!imageUrls.Any())
            {
                logger.LogWarning($"No images found for JobId: {jobId}");
                var notFoundResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new { error = "No images found for the specified JobId." });
                return notFoundResponse;
            }



            var successResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await successResponse.WriteAsJsonAsync(new { jobId, images = imageUrls });
            return successResponse;
        }
    }
}
