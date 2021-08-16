using Goussanjarga.Models;
using Goussanjarga.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Goussanjarga.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ICosmosDbService cosmosDb;
        private readonly IAzMediaService azMedia;
        private readonly ILogger<UploadController> _logger;

        private Container Container => cosmosDb.GetContainer(Config.CosmosVideos);

        public HomeController(ICosmosDbService cosmosDb, IAzMediaService azMedia, ILogger<UploadController> logger)
        {
            this.cosmosDb = cosmosDb;
            this.azMedia = azMedia;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            IEnumerable<Videos> streamingVideos = await cosmosDb.GetStreamingVideosAsync(Container);
            if (streamingVideos != null)
            {
                foreach (var video in streamingVideos)
                {
                    var listStreaming = await azMedia.GetStreamingURL(video.Locator);
                    if (listStreaming != null)
                    {
                        video.StreamingUrl = listStreaming;
                    }
                }
            }
            return View(streamingVideos);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<RedirectToActionResult> Delete(string Id)
        {
            try
            {
                await Container.DeleteItemAsync<Videos>(Id, new PartitionKey(Id));
            }
            catch (CosmosException ex)
            {
                _logger.LogError($"Cosmos Exception CODE: {ex.StatusCode} Message: {ex.Message}");
            }
            return RedirectToAction(nameof(Index));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}