using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Drawing;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Newtonsoft.Json;

namespace WeatherImageGenerator
{
    public static class ProcessImage
    {
        private static readonly HttpClient httpClient = new HttpClient();

        [Function("ProcessImage")]
        public static async Task Run(
            [QueueTrigger("image-jobs", Connection = "AzureWebJobsStorage")] string imageJobData,
            FunctionContext context)
        {
            var logger = context.GetLogger("ProcessImage");
            logger.LogInformation($"Processing image job: {imageJobData}");

            var stationData = JsonConvert.DeserializeObject<dynamic>(imageJobData);
            string jobId = stationData.JobId;
            string stationName = stationData.StationName;
            string weatherText = $"Station: {stationName}\nTemperature: {stationData.Temperature}°C\nWeather: {stationData.WeatherDescription} ";
                               

            try
            {
                string imageUrl = await FetchUnsplashImage();

                string imagePath = Path.GetTempFileName() + ".png";
                var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                File.WriteAllBytes(imagePath, imageBytes);

                string modifiedImagePath = WriteTextOnImage(jobId, stationName, weatherText, imagePath);

                string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("weather-images");
                await containerClient.CreateIfNotExistsAsync();

                BlobClient blobClient = containerClient.GetBlobClient($"{jobId}/{stationName}.png");
                using (FileStream uploadFileStream = File.OpenRead(modifiedImagePath))
                {
                    await blobClient.UploadAsync(uploadFileStream, overwrite: true);
                }
            }
            catch (Exception ex)
            {
                await status.TableStorage.SaveJobStatusAsync(jobId, "Failed");
                logger.LogError($"An error occurred while processing image for station {stationName}: {ex.Message}");
            }
        }

        private static string WriteTextOnImage(string jobId, string stationName, string weatherText, string imagePath)
        {
            string modifiedImagePath = Path.Combine(Path.GetTempPath(), $"{jobId}_{stationName}.png");
            using (var fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
            using (var bitmap = new Bitmap(fileStream))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.DrawString(weatherText, new Font("Arial", 24), Brushes.Black, new PointF(10, 10));
                bitmap.Save(modifiedImagePath);
            }

            return modifiedImagePath;
        }

        private static async Task<string> FetchUnsplashImage()
        {
            string unsplashApiKey = Environment.GetEnvironmentVariable("unsplashApiKey");
            string unsplashUrl = $"https://api.unsplash.com/photos/random?client_id={unsplashApiKey}&query=weather";
            var unsplashResponse = await httpClient.GetStringAsync(unsplashUrl);
            var unsplashData = JsonConvert.DeserializeObject<dynamic>(unsplashResponse);
            string imageUrl = unsplashData.urls.regular;
            return imageUrl;
        }
    }
}
