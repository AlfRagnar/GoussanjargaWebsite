using Goussanjarga.Models;
using Goussanjarga.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Goussanjarga.Controllers
{
    [AllowAnonymous]
    public class UploadController : Controller
    {
        private readonly ICosmosDbService cosmosDb;
        private readonly IAzMediaService azMedia;
        private readonly ILogger<UploadController> _logger;
        private Container Container => cosmosDb.GetContainer(Config.CosmosVideos);

        public UploadController(ICosmosDbService cosmosDb, IAzMediaService azMedia, ILogger<UploadController> logger)
        {
            this.cosmosDb = cosmosDb;
            this.azMedia = azMedia;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(UploadVideosVM model)
        {
            if (model.File != null && model.File.Length > 0 && CheckFileType(model.File))
            {
                try
                {
                    string filenameForStorage = Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "");
                    var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(model.File.FileName);

                    Random random = new();
                    Videos uploadVideo = new()
                    {
                        Id = filenameForStorage,
                        Extension = model.File.ContentType,
                        FileName = Convert.ToBase64String(plainTextBytes),
                        Size = model.File.Length,
                        UploadDate = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                        Status = "Not Processed",
                        Title = model.Title,
                        Description = model.Description
                    };
                    // Upload video with Azure Storage Service, the function returns a string containing the URI
                    Videos response = await azMedia.CreateAsset(model.File, uploadVideo);

                    // Azure Cosmos DB Operations
                    var res = await Container.CreateItemAsync(response, new PartitionKey(response.Id));
                    Console.WriteLine($"Finished uploading {Math.Round(model.File.Length / 1024d / 1024d)} MB from {model.File.Name}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Internal Error: {ex.Message}");
                }
            }

            return RedirectToAction("Index", "Home");
        }

        private static bool CheckFileType(IFormFile file)
        {
            if (file.ContentType.Contains("video"))
            {
                return true;
            }
            string[] formats = new string[] { ".mp4", ".avi", ".ogg", ".mov", ".wmv", ".webm" };
            return formats.Any(item => file.Name.EndsWith(item, StringComparison.OrdinalIgnoreCase));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}