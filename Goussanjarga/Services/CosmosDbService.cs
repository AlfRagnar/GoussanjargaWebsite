using Goussanjarga.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Goussanjarga.Services
{
    public class CosmosDbService : ICosmosDbService
    {
        private readonly CosmosClient _client;
        private readonly Database _dbClient;

        public CosmosDbService(CosmosClient dbClient, string databaseName)
        {
            _client = dbClient;
            _dbClient = dbClient.GetDatabase(databaseName);
        }

        // Set the container for when the service is ran
        public Container GetContainer(string containerName)
        {
            try
            {
                Container container = _dbClient.GetContainer(containerName);
                return container;
            }
            catch (CosmosException)
            {
                throw;
            }
        }

        public async Task<ContainerResponse> CheckContainer(string containerName, string partitionKeyPath)
        {
            try
            {
                ContainerResponse containerResponse = await _dbClient.CreateContainerIfNotExistsAsync(
               id: containerName,
               partitionKeyPath: partitionKeyPath,
               throughput: 400);
                return containerResponse;
            }
            catch (CosmosException)
            {
                throw;
            }
        }

        public async Task AddItemAsync(ToDoList item, Container container)
        {
            try
            {
                await container.CreateItemAsync(item, new PartitionKey(item.UserId));
            }
            catch (CosmosException)
            {
                throw;
            }
        }

        public async Task AddFamilyAsync(Families family, Container container)
        {
            try
            {
                await container.CreateItemAsync(family, new PartitionKey(family.LastName));
            }
            catch (CosmosException)
            {
                throw;
            }
        }

        public async Task AddVideo(Videos videos, Container container)
        {
            try
            {
                await container.CreateItemAsync(videos, new PartitionKey(videos.User.id));
            }
            catch (CosmosException)
            {
                throw;
            }
        }

        public async Task DeleteItemAsync(string id, string userId, Container container)
        {
            try
            {
                await container.DeleteItemAsync<ToDoList>(id, new PartitionKey(userId));
            }
            catch (CosmosException)
            {
                throw;
            }
        }

        public async Task DeleteFamilyAsync(string id, string family, Container container)
        {
            try
            {
                await container.DeleteItemAsync<Families>(id, new PartitionKey(family));
            }
            catch (CosmosException)
            {
                throw;
            }
        }

        public async Task<ToDoList> GetItemAsync(string id,string userId, Container container)
        {
            try
            {
                ItemResponse<ToDoList> response = await container.ReadItemAsync<ToDoList>(id, new PartitionKey(userId));
                return response.Resource;
            }
            catch (CosmosException ex)
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
            catch (CosmosException)
            {
                return null;
            }
        }

        public async Task<IEnumerable<ToDoList>> GetItemsAsync(string queryString, Container container)
        {
            try
            {
                FeedIterator<ToDoList> query = container.GetItemQueryIterator<ToDoList>(new QueryDefinition(queryString));
                List<ToDoList> results = new();
                while (query.HasMoreResults)
                {
                    FeedResponse<ToDoList> response = await query.ReadNextAsync();

                    results.AddRange(response.ToList());
                }
                return results;
            }
            catch (CosmosException)
            {
                throw;
            }
        }

        public async Task<IEnumerable<Families>> GetFamiliesAsync(string queryString, Container container)
        {
            try
            {
                IOrderedQueryable<Families> query = container.GetItemLinqQueryable<Families>();
                FeedIterator<Families> iterator = query.ToFeedIterator();
                FeedResponse<Families> results = await iterator.ReadNextAsync();
                return results;
            }
            catch (CosmosException)
            {
                throw;
            }
        }

        public async Task<IEnumerable<Videos>> GetUploadsAsync(string queryString, Container container)
        {
            try
            {
                IOrderedQueryable<Videos> query = container.GetItemLinqQueryable<Videos>();
                FeedIterator<Videos> iterator = query.ToFeedIterator();
                FeedResponse<Videos> results = await iterator.ReadNextAsync();
                return results;
            }
            catch (CosmosException)
            {
                throw;
            }
        }

        public async Task UpdateItem(ToDoList item, Container container)
        {
            try
            {
                await container.UpsertItemAsync(item, new PartitionKey(item.UserId));
            }
            catch (CosmosException)
            {
                throw;
            }
        }

        public async Task<ItemResponse<Families>> UpdateFamily(Families family, Container container)
        {
            try
            {
                ItemResponse<Families> updateResponse = await container.UpsertItemAsync(family, new PartitionKey(family.LastName));
                return updateResponse;
            }
            catch (CosmosException)
            {
                throw;
            }
        }

        public async Task<DatabaseResponse> CheckDatabase(string database)
        {
            try
            {
                DatabaseResponse databaseResponse = await _client.CreateDatabaseIfNotExistsAsync(database);
                return databaseResponse;
            }
            catch (CosmosException)
            {
                throw;
            }
        }

        public async Task AddUser(SiteUsers siteUsers, Container container)
        {
            try
            {
                await container.CreateItemAsync(siteUsers, new PartitionKey(siteUsers.id));
            }
            catch (CosmosException)
            {
                throw;
            }
        }

        public async Task<SiteUsers> GetUser(string id, Container container)
        {
            try
            {
                ItemResponse<SiteUsers> response = await container.ReadItemAsync<SiteUsers>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException)
            {
                SiteUsers siteUsers = new();
                return siteUsers;
            }
        }

        public Task ListContainersInDatabase()
        {
            throw new NotImplementedException();
        }
    }
}