using Azure;
using Azure.Storage.Blobs.Models;
using System.Threading.Tasks;

namespace Goussanjarga.Services
{
    public interface IBlobStorageService
    {
        Task<string> WriteTo(string fileName, string content);

        Task<BlobDownloadResult> FetchFile(string fileName);

        Pageable<BlobItem> ListFiles();
    }
}