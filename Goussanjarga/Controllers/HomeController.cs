using Goussanjarga.Models;
using Goussanjarga.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
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
        private Container Container => cosmosDb.GetContainer(Config.CosmosVideos);

        public HomeController(ICosmosDbService cosmosDb, IAzMediaService azMedia)
        {
            this.cosmosDb = cosmosDb;
            this.azMedia = azMedia;
        }

        public async Task<IActionResult> Index()
        {
            IEnumerable<Videos> streamingVideos = await cosmosDb.GetStreamingVideosAsync(Container);
            if(streamingVideos != null)
            {
                foreach(var video in streamingVideos)
                {
                    var listStreaming = await azMedia.GetStreamingURL(video.Locator);
                    if(listStreaming != null)
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}