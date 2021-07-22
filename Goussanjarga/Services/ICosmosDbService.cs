using Goussanjarga.Models;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Goussanjarga.Services
{
    public interface ICosmosDbService
    {
        // CRUD OPERATIONS
        Task AddItemAsync(ToDoList item, Container container);

        Task AddFamilyAsync(Families family, Container container);

        Task AddVideo(Videos videos, Container container);

        Task AddUser(SiteUsers siteUsers, Container container);

        Task DeleteItemAsync(string id, string userId, Container container);

        Task DeleteFamilyAsync(string id, string family, Container container);

        Task UpdateItem(ToDoList item, Container container);

        Task<ItemResponse<Families>> UpdateFamily(Families family, Container container);

        // META CRUD OPERATIONS
        Task<DatabaseResponse> CheckDatabase(string database);

        Task<ContainerResponse> CheckContainer(string containerName, string partitionKeyPath);

        // FETCH OPERATIONS
        Container GetContainer(string containerName);

        Task<ToDoList> GetItemAsync(string id, string userId, Container container);
        Task<List<ToDoList>> GetMyItems(string userId, Container container);

        Task<Families> GetFamilyAsync(string id, string familyName, Container container);
        Task<SiteUsers> GetUser(string id, Container container);

        Task<IEnumerable<ToDoList>> GetItemsAsync(string queryString, Container container);

        Task<IEnumerable<Families>> GetFamiliesAsync(string queryString, Container container);

        Task<IEnumerable<Videos>> GetUploadsAsync(string queryString, Container container);

        Task ListContainersInDatabase();
    }
}