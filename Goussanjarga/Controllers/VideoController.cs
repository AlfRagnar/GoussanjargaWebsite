using Goussanjarga.Models;
using Goussanjarga.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Goussanjarga.Controllers
{
    [AllowAnonymous]
    public class VideoController : Controller
    {
        public VideoController(ICosmosDbService cosmosDb, ILogger<VideoController> logger)
        {
            this.cosmosDb = cosmosDb;
            _logger = logger;
        }

        private readonly ILogger<VideoController> _logger;
        private readonly ICosmosDbService cosmosDb;
        private Container Container => cosmosDb.GetContainer(Config.CosmosVideos);

        public async Task<IActionResult> Index()
        {
            IEnumerable<Videos> videos = await cosmosDb.GetVideos(Container);

            foreach (var video in videos)
            {
                video.FileName = video.FileName.Trim();
                if (video.FileName.Length % 4 == 0 && Regex.IsMatch(video.FileName, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None))
                {
                    var bytes = Convert.FromBase64String(video.FileName);
                    video.FileName = Encoding.UTF8.GetString(bytes);
                }
            }
            return View(videos);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Upload()
        {
            return RedirectToAction("Index", "Upload");
        }

        public IActionResult Edit()
        {
            return RedirectToAction("Index", "Upload");
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