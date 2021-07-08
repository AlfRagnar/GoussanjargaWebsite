using Goussanjarga.Models;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Goussanjarga.Services
{
    public class CosmosDbService : ICosmosDbService
    {
        private readonly Container _container;

        public CosmosDbService(
            CosmosClient dbClient,
            string databaseName,
            string containerName = "Item")
        {
            try
            {
                Database database = dbClient.GetDatabase(databaseName);
                database.CreateContainerIfNotExistsAsync(containerName, "/id");
                _container = dbClient.GetContainer(databaseName, containerName);
            }
            catch (CosmosException ex)
            {
                Console.WriteLine("Error: " + ex);
                return;
            }
        }

        public async Task AddItemAsync(Item item)
        {
            await _container.CreateItemAsync(item, new PartitionKey(item.Id));
        }

        public async Task DeleteItemAsync(string id)
        {
            await _container.DeleteItemAsync<Item>(id, new PartitionKey(id));
        }

        public async Task<Item> GetItemAsync(string id)
        {
            try
            {
                ItemResponse<Item> response = await _container.ReadItemAsync<Item>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<IEnumerable<Item>> GetItemsAsync(string queryString)
        {
            var query = _container.GetItemQueryIterator<Item>(new QueryDefinition(queryString));
            List<Item> results = new();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();

                results.AddRange(response.ToList());
            }

            return results;
        }

        public async Task UpdateItemAsync(string id, Item item)
        {
            await _container.UpsertItemAsync(item, new PartitionKey(id));
        }
    }
}