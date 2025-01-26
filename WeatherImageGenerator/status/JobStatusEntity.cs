using Azure.Data.Tables;
using System;

namespace WeatherImageGenerator.status
{
    public class JobStatusEntity : ITableEntity
    {
        public string PartitionKey { get; set; } 
        public string RowKey { get; set; } 
        public Azure.ETag ETag { get; set; } 
        public DateTimeOffset? Timestamp { get; set; } 
        public string Status { get; set; }
    }
}
