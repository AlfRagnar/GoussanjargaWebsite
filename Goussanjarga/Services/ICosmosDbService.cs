using Goussanjarga.Models;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Goussanjarga.Services
{
    public interface ICosmosDbService
    {
        // CRUD OPERATIONS
        Task AddItemAsync(ToDoList item, Container container);

        Task AddFamilyAsync(Families family, Container container);

        Task DeleteItemAsync(string id, Container container);

        Task DeleteFamilyAsync(string id, string family, Container container);

        Task UpdateItem(ToDoList item, Container container);

        Task<ItemResponse<Families>> UpdateFamily(Families family, Container container);

        // META CRUD OPERATIONS
        Task<DatabaseResponse> CheckDatabase(string database);

        Task<ContainerResponse> CheckContainer(string containerName, string partitionKeyPath);

        // FETCH OPERATIONS
        Container GetContainer([Optional] string containerName);

        Task<ToDoList> GetItemAsync(string id, Container container);

        Task<Families> GetFamilyAsync(string id, string familyName, Container container);

        Task<IEnumerable<ToDoList>> GetItemsAsync(string queryString, Container container);

        Task<IEnumerable<Families>> GetFamiliesAsync(string queryString, Container container);

        Task<IEnumerable<Videos>> GetUploadsAsync(string queryString, Container container);

        Task ListContainersInDatabase();
    }
}