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

        public async Task<ContainerResponse> CheckContainer(string containerName, string partitionKeyPath)
        {
            ContainerResponse containerResponse = await _dbClient.CreateContainerIfNotExistsAsync(
                id: containerName,
                partitionKeyPath: partitionKeyPath,
                throughput: 400);
            return containerResponse;
        }

        public async Task AddItemAsync(ToDoList item, Container container)
        {
            await container.CreateItemAsync(item, new PartitionKey(item.Id));
        }

        public async Task AddFamilyAsync(Families family, Container container)
        {
            await container.CreateItemAsync(family, new PartitionKey(family.LastName));
        }

        public async Task DeleteItemAsync(string id, Container container)
        {
            await container.DeleteItemAsync<ToDoList>(id, new PartitionKey(id));
        }

        public async Task DeleteFamilyAsync(string id,string family, Container container)
        {
            try
            {
                await container.DeleteItemAsync<Families>(id, new PartitionKey(family));
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

        public async Task<ToDoList> GetItemAsync(string id, Container container)
        {
            try
            {
                ItemResponse<ToDoList> response = await container.ReadItemAsync<ToDoList>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<Families> GetFamilyAsync(string id, string familyName, Container container)
        {
            try
            {
                ItemResponse<Families> response = await container.ReadItemAsync<Families>(id, new PartitionKey(familyName));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<IEnumerable<ToDoList>> GetItemsAsync(string queryString, Container container)
        {
            FeedIterator<ToDoList> query = container.GetItemQueryIterator<ToDoList>(new QueryDefinition(queryString));
            List<ToDoList> results = new();
            while (query.HasMoreResults)
            {
                FeedResponse<ToDoList> response = await query.ReadNextAsync();

                results.AddRange(response.ToList());
            }
            Trace.WriteLine("GET operation @ Container: {0}", container.Id);
            return results;
        }

        public async Task<IEnumerable<Families>> GetFamiliesAsync(string queryString, Container container)
        {
            IOrderedQueryable<Families> query = container.GetItemLinqQueryable<Families>();
            FeedIterator<Families> iterator = query.ToFeedIterator();
            FeedResponse<Families> results = await iterator.ReadNextAsync();
            return results;
        }

        public async Task<IEnumerable<Videos>> GetUploadsAsync(string queryString, Container container)
        {
            IOrderedQueryable<Videos> query = container.GetItemLinqQueryable<Videos>();
            FeedIterator<Videos> iterator = query.ToFeedIterator();
            FeedResponse<Videos> results = await iterator.ReadNextAsync();
            return results;
        }

        public async Task UpdateItem(ToDoList item, Container container)
        {
            await container.UpsertItemAsync(item, new PartitionKey(item.Id));
        }

        public async Task<ItemResponse<Families>> UpdateFamily(Families family, Container container)
        {
            ItemResponse<Families> updateResponse = await container.UpsertItemAsync(family, new PartitionKey(family.LastName));
            return updateResponse;
        }

        public async Task<DatabaseResponse> CheckDatabase(string database)
        {
            DatabaseResponse databaseResponse = await _client.CreateDatabaseIfNotExistsAsync(database);
            return databaseResponse;
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