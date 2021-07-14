using Goussanjarga.Models;
using Goussanjarga.Services;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Goussanjarga.Controllers
{
    [Authorize]
    public class ItemController : Controller
    {
        private readonly ICosmosDbService _cosmosDbService;
        private readonly Container _container;
        private readonly TelemetryClient telemetryClient;
        private readonly string containerName = "ToDoList";

        public ItemController(ICosmosDbService cosmosDbService, TelemetryClient telemetryClient)
        {
            _cosmosDbService = cosmosDbService;
            _container = _cosmosDbService.GetContainer(containerName);
            this.telemetryClient = telemetryClient;
        }

        [ActionName("Index")]
        public async Task<IActionResult> Index()
        {
            try
            {
                telemetryClient.TrackTrace("Current Container: " + _container.Id);
                IEnumerable<Item> item = await _cosmosDbService.GetItemsAsync("SELECT * FROM c", _container);
                return View(item);
            }
            catch (CosmosException ex)
            {
                telemetryClient.TrackException(ex);
                throw;
            }
        }

        [ActionName("Create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateAsync([Bind("Id,Name,Description,Completed")] Item item)
        {
            if (ModelState.IsValid)
            {
                item.Id = Guid.NewGuid().ToString();
                await _cosmosDbService.AddItemAsync(item, _container);
                return RedirectToAction("Index");
            }

            return View(item);
        }

        [HttpPost]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditAsync([Bind("Id,Name,Description,Completed")] Item item)
        {
            if (ModelState.IsValid)
            {
                await _cosmosDbService.UpdateItem(item.Id, item, _container);
                return RedirectToAction("Index");
            }

            return View(item);
        }

        [ActionName("Edit")]
        public async Task<ActionResult> EditAsync(string id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            Item item = await _cosmosDbService.GetItemAsync(id, _container);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        [ActionName("Delete")]
        public async Task<ActionResult> DeleteAsync(string id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            Item item = await _cosmosDbService.GetItemAsync(id, _container);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmedAsync([Bind("Id")] Item item)
        {
            await _cosmosDbService.DeleteItemAsync(item, _container);
            return RedirectToAction("Index");
        }

        [ActionName("Details")]
        public async Task<ActionResult> DetailsAsync(string id)
        {
            return View(await _cosmosDbService.GetItemAsync(id, _container));
        }
    }
}