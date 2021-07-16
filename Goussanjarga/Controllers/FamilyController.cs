﻿using Goussanjarga.Models;
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
        private readonly ICosmosDbService _cosmosDbService;
        private readonly Container _container;
        private readonly TelemetryClient telemetryClient;
        private readonly string containerName = "Families";

        public FamilyController(ICosmosDbService cosmosDbService, TelemetryClient telemetryClient)
        {
            _cosmosDbService = cosmosDbService;
            _container = _cosmosDbService.GetContainer(containerName);
            this.telemetryClient = telemetryClient;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                telemetryClient.TrackTrace("Current Container: " + _container.Id);
                IEnumerable<Family> families = await _cosmosDbService.GetFamiliesAsync("SELECT * FROM c", _container);
                return View(families);
            }
            catch (CosmosException ex)
            {
                telemetryClient.TrackException(ex);
                throw;
            }
        }

        [ActionName("Edit")]
        public ActionResult Edit(Family family)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            if (family == null)
            {
                return NotFound();
            }
            return View(family);
        }

        [HttpPost]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditConfirmedAsync([Bind("Id,LastName,Address,IsRegistered")] Family family)
        {
            if (ModelState.IsValid)
            {
                await _cosmosDbService.UpdateFamily(family, _container);
                return RedirectToAction("Index");
            }
            return View(family);
        }

        [ActionName("Create")]
        public IActionResult Create()
        {
            return View();
        }

        [ActionName("Details")]
        public IActionResult Details(Family family)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            if (family == null)
            {
                return NotFound();
            }
            return View(family);
        }

        [ActionName("Delete")]
        public IActionResult Delete(Family family)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            if (family == null)
            {
                return NotFound();
            }
            return View(family);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed([Bind("Id,familyName")] Family family)
        {
            await _cosmosDbService.DeleteFamilyAsync(family, _container);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateAsync([Bind("Id,LastName,Address,IsRegistered")] Family item)
        {
            if (ModelState.IsValid)
            {
                item.Id = Guid.NewGuid().ToString();
                await _cosmosDbService.AddFamilyAsync(item, _container);
                return RedirectToAction("Index");
            }
            return View(item);
        }
    }
}