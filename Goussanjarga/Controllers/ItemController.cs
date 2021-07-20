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
        private readonly TelemetryClient _telemetryClient;
        private readonly string containerName = "ToDoList";

        public ItemController(ICosmosDbService cosmosDbService, TelemetryClient telemetryClient)
        {
            _cosmosDbService = cosmosDbService;
            _container = _cosmosDbService.GetContainer(containerName);
            _telemetryClient = telemetryClient;
        }

        [ActionName("Index")]
        public async Task<IActionResult> Index()
        {
            try
            {
                IEnumerable<ToDoList> item = await _cosmosDbService.GetItemsAsync("SELECT * FROM c", _container);
                return View(item);
            }
            catch (CosmosException ex)
            {
                _telemetryClient.TrackException(ex);
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
        public async Task<ActionResult> CreateAsync([Bind("Id,Name,Description,Completed")] ToDoList item)
        {
            if (ModelState.IsValid)
            {
                item.Id = Guid.NewGuid().ToString();
                await _cosmosDbService.AddItemAsync(item, _container);
                return RedirectToAction("Index");
            }

            return View(item);
        }

        

        // Get the Item based on Item ID
        [ActionName("Edit")]
        public async Task<ActionResult> EditAsync(string id)
        {

            ToDoList item = await _cosmosDbService.GetItemAsync(id, _container);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }
        [HttpPost]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditConfirmed([Bind("Id,Name,Description,Completed")] ToDoList item)
        {
            if (ModelState.IsValid)
            {
                await _cosmosDbService.UpdateItem(item, _container);
                return RedirectToAction("Index");
            }
            return RedirectToAction("Index");
        }

        // Get the item object and return to view

        [ActionName("Delete")]
        public async Task<ActionResult> DeleteAsync(string id)
        {
            ToDoList item = await _cosmosDbService.GetItemAsync(id, _container);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmedAsync([Bind("Id")] string id)
        {
            await _cosmosDbService.DeleteItemAsync(id, _container);
            return RedirectToAction("Index");
        }
    }
}