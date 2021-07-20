using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace Goussanjarga.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly IConfiguration _config;
        private readonly TelemetryClient _telemetryClient;
        private readonly BlobServiceClient _serviceClient;
        private readonly BlobContainerClient _blobContainer;

        public BlobStorageService(IConfiguration configuration, TelemetryClient telemetryClient, BlobServiceClient serviceClient)
        {
            _config = configuration;
            _telemetryClient = telemetryClient;
            _serviceClient = serviceClient;
            _blobContainer = serviceClient.GetBlobContainerClient(_config["AzureBlobStorage:ContainerName"].ToString());
        }

        public async Task<BlobDownloadResult> FetchFile(string fileName)
        {
            try
            {
                BlobClient blobClient = _blobContainer.GetBlobClient(fileName);
                BlobDownloadResult blobDownloadResult = await blobClient.DownloadContentAsync();
                return blobDownloadResult;
            }
            catch (RequestFailedException ex)
            {
                _telemetryClient.TrackException(ex);
                throw new RequestFailedException(ex.ToString());
            }
        }

        public Pageable<BlobItem> ListFiles()
        {
            try
            {
                Pageable<BlobItem> items = _blobContainer.GetBlobs();
                return items;
            }
            catch (Exception ex)
            {
                _telemetryClient.TrackException(ex);
                throw;
            }
        }

        public Task<string> WriteTo(string fileName, string content)
        {
            throw new NotImplementedException();
        }

        public AsyncPageable<BlobContainerItem> ListContainers()
        {
            try
            {
                AsyncPageable<BlobContainerItem> containers = _serviceClient.GetBlobContainersAsync();
                return containers;
            }
            catch (RequestFailedException ex)
            {
                throw new RequestFailedException(ex.ToString());
            }
        }
    }
}