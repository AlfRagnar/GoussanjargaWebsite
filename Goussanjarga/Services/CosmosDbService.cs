using Goussanjarga.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Goussanjarga.Services
{
    public class CosmosDbService : ICosmosDbService
    {
        private readonly Database _dbClient;
        private readonly CosmosClient _client;

        public CosmosDbService(CosmosClient dbClient, string databaseName)
        {
            {
                _client = dbClient;
                _dbClient = dbClient.GetDatabase(databaseName);
            }
        }

        // Set the container for when the service is ran
        public Container GetContainer([Optional] string containerName)
        {
            Container container;
            if (string.IsNullOrEmpty(containerName))
            {
                try
                {
                    container = _dbClient.GetContainer("User");
                }
                catch (CosmosException NotFound)
                {
                    Trace.TraceError("Cosmos Exception: " + NotFound);
                    throw;
                }
            }
            else
            {
                container = _dbClient.GetContainer(containerName);
            }
            return container;
        }

        public async Task<Container> CreateContainer(string containerName, string partitionKeyPath)
        {
            ContainerResponse containerResponse = await _dbClient.CreateContainerIfNotExistsAsync(
                id: containerName,
                partitionKeyPath: partitionKeyPath,
                throughput: 400);
            return containerResponse;
        }

        public async Task AddItemAsync(Item item, Container container)
        {
            await container.CreateItemAsync(item, new PartitionKey(item.Id));
        }

        public async Task AddFamilyAsync(Family family, Container container)
        {
            await container.CreateItemAsync(family, new PartitionKey(family.LastName));
        }

        public async Task DeleteItemAsync(Item item, Container container)
        {
            await container.DeleteItemAsync<Item>(item.Id, new PartitionKey(item.Id));
        }

        public async Task DeleteFamilyAsync(Family family, Container container)
        {
            try
            {
                await container.DeleteItemAsync<Family>(family.Id, new PartitionKey(family.LastName));
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                Trace.TraceError("Family not found");
                throw;
            }
            catch (CosmosException ex)
            {
                Trace.TraceError("Cosmos Exception: " + ex);
                throw;
            }
        }

        public async Task<Item> GetItemAsync(string id, Container container)
        {
            try
            {
                ItemResponse<Item> response = await container.ReadItemAsync<Item>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<Family> GetFamilyAsync(string id, string familyName, Container container)
        {
            try
            {
                ItemResponse<Family> response = await container.ReadItemAsync<Family>(id, new PartitionKey(familyName));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<IEnumerable<Item>> GetItemsAsync(string queryString, Container container)
        {
            var query = container.GetItemQueryIterator<Item>(new QueryDefinition(queryString));
            List<Item> results = new();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();

                results.AddRange(response.ToList());
            }
            Trace.WriteLine("Current Container: {0}", container.Id);
            return results;
        }

        public async Task<IEnumerable<Family>> GetFamiliesAsync(string queryString, Container container)
        {
            var query = container.GetItemLinqQueryable<Family>();
            var iterator = query.ToFeedIterator();
            var results = await iterator.ReadNextAsync();
            return results;
        }

        public async Task UpdateItem(Item item, Container container)
        {
            await container.UpsertItemAsync(item, new PartitionKey(item.Id));
        }

        public async Task UpdateFamily(Family family, Container container)
        {
            await container.UpsertItemAsync(family, new PartitionKey(family.LastName));
        }

        public async Task<DatabaseResponse> CheckDatabase(string database)
        {
            DatabaseResponse databaseResponse = await _client.CreateDatabaseIfNotExistsAsync(database);
            return databaseResponse;
        }

        public async Task<ContainerResponse> CheckContainer(string container, string partitionKey)
        {
            ContainerResponse containerResponse = await _dbClient.CreateContainerIfNotExistsAsync(container, partitionKey);
            return containerResponse;
        }

        public async Task ListContainersInDatabase()
        {
            using FeedIterator<ContainerProperties> resultSetIterator = _dbClient.GetContainerQueryIterator<ContainerProperties>();
            while (resultSetIterator.HasMoreResults)
            {
                foreach (ContainerProperties container in await resultSetIterator.ReadNextAsync())
                {
                    Trace.TraceInformation("Container name: " + container.Id);
                }
            }
        }
    }
}