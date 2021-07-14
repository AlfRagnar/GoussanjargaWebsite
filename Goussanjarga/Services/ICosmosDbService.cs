using Goussanjarga.Models;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Goussanjarga.Services
{
    public interface ICosmosDbService
    {
        Task<Container> CreateContainer(string containerName, string partitionKeyPath);

        // CRUD OPERATIONS
        Task AddItemAsync(Item item, Container container);

        Task AddFamilyAsync(Family family, Container container);

        Task DeleteItemAsync(Item item, Container container);

        Task DeleteFamilyAsync(Family family, Container container);

        Task UpdateItem(string id, Item item, Container container);

        Task UpdateFamily(Family family, Container container);

        Task<DatabaseResponse> CheckDatabase(string database);

        Task<ContainerResponse> CheckContainer(string container, string partitionKey);

        // FETCH OPERATIONS
        Container GetContainer([Optional] string containerName);

        Task<Item> GetItemAsync(string id, Container container);

        Task<Family> GetFamilyAsync(string familyId, string familyName, Container container);

        Task<IEnumerable<Item>> GetItemsAsync(string queryString, Container container);

        Task<IEnumerable<Family>> GetFamiliesAsync(string queryString, Container container);

        Task ListContainersInDatabase();
    }
}