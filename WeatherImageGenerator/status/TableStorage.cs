using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Data.Tables;

namespace WeatherImageGenerator.status
{
    public class TableStorage
    {
        private static readonly string TableName = "JobStatus";

        public static TableClient GetTableClient()
        {
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var serviceClient = new TableServiceClient(connectionString);
            var tableClient = serviceClient.GetTableClient(TableName);
            tableClient.CreateIfNotExists();
            return tableClient;
        }

        public static async Task SaveJobStatusAsync(string jobId, string status)
        {
            var tableClient = GetTableClient();
            var entity = new JobStatusEntity
            {
                PartitionKey = "JobStatus", 
                RowKey = jobId,
                Status = status
            };
            await tableClient.UpsertEntityAsync(entity);
        }

        public static async Task<JobStatusEntity> GetJobStatusAsync(string jobId)
        {
            var tableClient = GetTableClient();
            return await tableClient.GetEntityAsync<JobStatusEntity>("JobStatus", jobId);
        }
    }
}
