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
    public class FamilyController : Controller
    {
        private readonly string containerName = "Families";
        private readonly ICosmosDbService _cosmosDbService;
        private readonly Container _container;
        private readonly TelemetryClient _telemetryClient;

        public FamilyController(ICosmosDbService cosmosDbService, TelemetryClient telemetryClient)
        {
            _cosmosDbService = cosmosDbService;
            _container = _cosmosDbService.GetContainer(containerName);
            _telemetryClient = telemetryClient;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                IEnumerable<Families> families = await _cosmosDbService.GetFamiliesAsync("SELECT * FROM c", _container);
                return View(families);
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
        public async Task<ActionResult> CreateAsync([Bind("Id,LastName,Address,IsRegistered")] Families family)
        {
            if (ModelState.IsValid)
            {
                family.Id = Guid.NewGuid().ToString();
                await _cosmosDbService.AddFamilyAsync(family, _container);
                return RedirectToAction("Index");
            }
            return View(family);
        }

        // Get the family object and return to View
        [ActionName("Details")]
        public async Task<IActionResult> DetailsAsync(string id, string family)
        {
            Families familyObject = await _cosmosDbService.GetFamilyAsync(id, family, _container);
            if (familyObject == null)
            {
                return NotFound();
            }
            return View(familyObject);
        }

        // Get the Family object and return to View
        [ActionName("Edit")]
        public async Task<ActionResult> EditAsync(string id, string family)
        {
            Families familyObject = await _cosmosDbService.GetFamilyAsync(id, family, _container);

            if (familyObject == null)
            {
                return NotFound();
            }
            return View(familyObject);
        }

        [HttpPost]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditConfirmedAsync(Families family)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var reponse = await _cosmosDbService.UpdateFamily(family, _container);
                }
                catch (CosmosException ex) when (ex.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    _telemetryClient.TrackException(ex);
                    return BadRequest();
                }
                return RedirectToAction("Index");
            }
            return View(family);
        }

        // Get the family object and return to View
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteAsync(string id, string family)
        {
            Families familyObject = await _cosmosDbService.GetFamilyAsync(id, family, _container);
            if (familyObject == null)
            {
                return NotFound();
            }
            return View(familyObject);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(string id, string family)
        {
            if (ModelState.IsValid && id != null)
            {
                await _cosmosDbService.DeleteFamilyAsync(id, family, _container);
                return RedirectToAction("Index");
            }
            return View(family);
        }
    }
}