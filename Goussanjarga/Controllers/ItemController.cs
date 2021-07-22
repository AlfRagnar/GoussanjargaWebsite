using Goussanjarga.Models;
using Goussanjarga.Services;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Goussanjarga.Controllers
{
    [Authorize]
    public class ItemController : Controller
    {
        private readonly string[] _graphScopes = new[] { "user.read" };
        private readonly string containerName = "ToDoList";
        private readonly ICosmosDbService _cosmosDbService;
        private readonly Container _container;
        private readonly TelemetryClient _telemetryClient;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly MicrosoftIdentityConsentAndConditionalAccessHandler _consentHandler;

        public ItemController(ICosmosDbService cosmosDbService, TelemetryClient telemetryClient, MicrosoftIdentityConsentAndConditionalAccessHandler consentHandler, GraphServiceClient graphServiceClient)
        {
            _cosmosDbService = cosmosDbService;
            _container = _cosmosDbService.GetContainer(containerName);
            _telemetryClient = telemetryClient;
            _consentHandler = consentHandler;
            _graphServiceClient = graphServiceClient;
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
        [AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
        public async Task<ActionResult> CreateAsync([Bind("Name,Description,Completed")] ToDoList item)
        {
            Microsoft.Graph.User currentUser = null;
            try
            {
                currentUser = await _graphServiceClient.Me.Request().GetAsync();
            }
            // Catch CAE exception from Graph SDK
            catch (ServiceException svcex) when (svcex.Message.Contains("Continuous access evaluation resulted in claims challenge"))
            {
                try
                {
                    _telemetryClient.TrackException(svcex);
                    string claimChallenge = WwwAuthenticateParameters.GetClaimChallengeFromResponseHeaders(svcex.ResponseHeaders);
                    _consentHandler.ChallengeUser(_graphScopes, claimChallenge);
                    return new EmptyResult();
                }
                catch (Exception ex2)
                {
                    _telemetryClient.TrackException(ex2);
                    _consentHandler.HandleException(ex2);
                }
            }

            item.UserId = currentUser.Id;
            item.Id = Guid.NewGuid().ToString();
            try
            {
                await _cosmosDbService.AddItemAsync(item, _container);
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                return View(item);
            }
        }

        // Get the Item based on Item ID
        public async Task<ActionResult> Edit(string id, [Bind("UserId, Id")] ToDoList list)
        {
            if (id != list.Id)
            {
                return NotFound();
            }

            ToDoList item = await _cosmosDbService.GetItemAsync(list.Id, list.UserId, _container);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        [HttpPost]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditConfirmed(string id, [Bind("UserId,Id,Name,Description,Completed")] ToDoList list)
        {
            if (id != list.Id)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                try
                {
                    await _cosmosDbService.UpdateItem(list, _container);
                }
                catch (CosmosException ex)
                {
                    if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index");
            }
            return View(list);
        }

        // Get the item object and return to view

        [ActionName("Delete")]
        public async Task<ActionResult> DeleteAsync(string id, [Bind("UserId,Id")] ToDoList list)
        {
            if (id != list.Id)
            {
                return NotFound();
            }
            ToDoList item = await _cosmosDbService.GetItemAsync(list.Id, list.UserId, _container);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmedAsync(string id, [Bind("Id, UserId")] ToDoList list)
        {
            if (id != list.Id)
            {
                return NotFound();
            }
            await _cosmosDbService.DeleteItemAsync(list.Id, list.UserId, _container);
            return RedirectToAction("Index");
        }
    }
}