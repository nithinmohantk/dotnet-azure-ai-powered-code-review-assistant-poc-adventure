using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CodeReviewAssistant.Infrastructure.ExternalServices
{
    public interface IAzureCosmosDbService
    {
        Task<T> GetItemAsync<T>(string id, string partitionKey, CancellationToken cancellationToken = default) where T : class;
        Task<IEnumerable<T>> GetItemsAsync<T>(string query, CancellationToken cancellationToken = default) where T : class;
        Task<T> CreateItemAsync<T>(T item, CancellationToken cancellationToken = default) where T : class;
        Task<T> UpdateItemAsync<T>(string id, T item, string partitionKey, CancellationToken cancellationToken = default) where T : class;
        Task DeleteItemAsync<T>(string id, string partitionKey, CancellationToken cancellationToken = default) where T : class;
        Task<bool> ItemExistsAsync<T>(string id, string partitionKey, CancellationToken cancellationToken = default) where T : class;
    }

    public class AzureCosmosDbService : IAzureCosmosDbService
    {
        private readonly CosmosClient _cosmosClient;
        private readonly string _databaseName;
        private readonly string _containerName;
        private readonly ILogger<AzureCosmosDbService> _logger;

        public AzureCosmosDbService(IConfiguration configuration, ILogger<AzureCosmosDbService> logger)
        {
            var endpoint = configuration["Azure:CosmosDb:Endpoint"];
            var key = configuration["Azure:CosmosDb:Key"];
            _databaseName = configuration["Azure:CosmosDb:DatabaseName"] ?? "CodeReviewDB";
            _containerName = configuration["Azure:CosmosDb:ContainerName"] ?? "CodeReviews";

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Azure Cosmos DB endpoint and key must be configured");
            }

            _cosmosClient = new CosmosClient(endpoint, key, new CosmosClientOptions
            {
                ApplicationName = "CodeReviewAssistant",
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            });

            _logger = logger;
        }

        private Container GetContainer()
        {
            var database = _cosmosClient.GetDatabase(_databaseName);
            return database.GetContainer(_containerName);
        }

        public async Task<T> GetItemAsync<T>(string id, string partitionKey, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                _logger.LogDebug("Retrieving item {Id} with partition key {PartitionKey}", id, partitionKey);
                
                var container = GetContainer();
                var response = await container.ReadItemAsync<T>(id, new PartitionKey(partitionKey), null, cancellationToken);
                
                _logger.LogDebug("Successfully retrieved item {Id}", id);
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Item {Id} not found", id);
                return default;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve item {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<T>> GetItemsAsync<T>(string query, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                _logger.LogDebug("Executing query: {Query}", query);
                
                var container = GetContainer();
                var queryDefinition = new QueryDefinition(query);
                var queryResultSetIterator = container.GetItemQueryIterator<T>(queryDefinition);

                var results = new List<T>();
                while (queryResultSetIterator.HasMoreResults)
                {
                    var response = await queryResultSetIterator.ReadNextAsync(cancellationToken);
                    results.AddRange(response);
                }

                _logger.LogDebug("Query returned {Count} items", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute query: {Query}", query);
                throw;
            }
        }

        public async Task<T> CreateItemAsync<T>(T item, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                _logger.LogDebug("Creating new item");
                
                var container = GetContainer();
                var response = await container.CreateItemAsync(item, cancellationToken: cancellationToken);
                
                _logger.LogDebug("Successfully created item");
                return response.Resource;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create item");
                throw;
            }
        }

        public async Task<T> UpdateItemAsync<T>(string id, T item, string partitionKey, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                _logger.LogDebug("Updating item {Id}", id);
                
                var container = GetContainer();
                var response = await container.UpsertItemAsync(item, new PartitionKey(partitionKey), cancellationToken: cancellationToken);
                
                _logger.LogDebug("Successfully updated item {Id}", id);
                return response.Resource;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update item {Id}", id);
                throw;
            }
        }

        public async Task DeleteItemAsync<T>(string id, string partitionKey, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                _logger.LogDebug("Deleting item {Id}", id);
                
                var container = GetContainer();
                await container.DeleteItemAsync<T>(id, new PartitionKey(partitionKey), null, cancellationToken);
                
                _logger.LogDebug("Successfully deleted item {Id}", id);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Item {Id} not found for deletion", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete item {Id}", id);
                throw;
            }
        }

        public async Task<bool> ItemExistsAsync<T>(string id, string partitionKey, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                var container = GetContainer();
                var response = await container.ReadItemStreamAsync(id, new PartitionKey(partitionKey), null, cancellationToken);
                return response.StatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if item {Id} exists", id);
                throw;
            }
        }
    }

    public class CosmosDbContainerConfiguration
    {
        public string ContainerName { get; set; }
        public string PartitionKeyPath { get; set; }
        public int? Throughput { get; set; }
        public string? DefaultTimeToLive { get; set; }
        public List<IndexingPolicy> IndexingPolicies { get; set; } = new();
    }

    public class IndexingPolicy
    {
        public string Path { get; set; }
        public string DataType { get; set; }
        public string Kind { get; set; }
    }
}
