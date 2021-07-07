using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Goussanjarga.Controllers
{
    public class UploadController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        private static async Task ListContainers(BlobServiceClient blobServiceClient, string prefix, int? segmentSize)
        {
            try
            {
                // Call the listing operation and enumerate the result segment.
                var resultSegment =
                    blobServiceClient.GetBlobContainersAsync(BlobContainerTraits.Metadata, prefix, default)
                    .AsPages(default, segmentSize);

                await foreach (Page<BlobContainerItem> containerPage in resultSegment)
                {
                    foreach (BlobContainerItem containerItem in containerPage.Values)
                    {
                        Console.WriteLine("Container name: {0}", containerItem.Name);
                    }

                    Console.WriteLine();
                }
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }

        private static void CreateRootContainer(BlobServiceClient blobServiceClient)
        {
            try
            {
                BlobContainerClient container = blobServiceClient.CreateBlobContainer("$root");
                if (container.Exists())
                {
                    Console.WriteLine("Created root container.");
                }
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine("HTTP error code {0}: {1}",
                            e.Status, e.ErrorCode);
                Console.WriteLine(e.Message);
            }
        }

        private static async Task<BlobContainerClient> CreateUserContainer(BlobServiceClient blobServiceClient)
        {
            string containerName = "test-container-" + Guid.NewGuid();
            try
            {
                BlobContainerClient container = await blobServiceClient.CreateBlobContainerAsync(containerName);
                if (await container.ExistsAsync())
                {
                    Console.WriteLine("Created container {0}", container.Name);
                    return container;
                }
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine("HTTP error code {0}: {1}", e.Status, e.ErrorCode);
                Console.WriteLine(e.Message);
            }
            return null;
        }

        private static async Task DeleteSampleContainerAsync(BlobServiceClient blobServiceClient, string containerName)
        {
            BlobContainerClient container = blobServiceClient.GetBlobContainerClient(containerName);

            try
            {
                // Delete the specified container and handle the exception.
                await container.DeleteAsync();
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine("HTTP error code {0}: {1}",
                                    e.Status, e.ErrorCode);
                Console.WriteLine(e.Message);
                Console.ReadLine();
            }
        }

        private static async Task DeleteContainersWithPrefixAsync(BlobServiceClient blobServiceClient, string prefix)
        {
            Console.WriteLine("Delete all containers beginning with the specified prefix");

            try
            {
                foreach (BlobContainerItem container in blobServiceClient.GetBlobContainers())
                {
                    if (container.Name.StartsWith(prefix))
                    {
                        Console.WriteLine("\tContainer:" + container.Name);
                        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(container.Name);
                        await containerClient.DeleteAsync();
                    }
                }

                Console.WriteLine();
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }
    }
}