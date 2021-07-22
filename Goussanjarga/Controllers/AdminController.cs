using Goussanjarga.Models;
using Goussanjarga.Services;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    public class AdminController : Controller
    {
        private readonly string[] _graphScopes = new[] { "user.read" };
        private readonly string containerName = "ToDoList";
        private readonly ICosmosDbService _cosmosDbService;
        private readonly Container _container;
        private readonly TelemetryClient _telemetryClient;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly MicrosoftIdentityConsentAndConditionalAccessHandler _consentHandler;
        private readonly Microsoft.Graph.User _user;

        public AdminController(ICosmosDbService cosmosDbService, TelemetryClient telemetryClient, MicrosoftIdentityConsentAndConditionalAccessHandler consentHandler, GraphServiceClient graphServiceClient)
        {
            _cosmosDbService = cosmosDbService;
            _container = _cosmosDbService.GetContainer(containerName);
            _telemetryClient = telemetryClient;
            _consentHandler = consentHandler;
            _graphServiceClient = graphServiceClient;
            _user = GetUser().GetAwaiter().GetResult();
        }

        [AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
        private async Task<Microsoft.Graph.User> GetUser()
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
                    string claimChallenge = WwwAuthenticateParameters.GetClaimChallengeFromResponseHeaders(svcex.ResponseHeaders);
                    _consentHandler.ChallengeUser(_graphScopes, claimChallenge);
                    try
                    {
                        currentUser = await _graphServiceClient.Me.Request().GetAsync();
                    }
                    catch (Exception ex3)
                    {
                        _telemetryClient.TrackException(ex3);
                        _consentHandler.HandleException(ex3);
                    }
                }
                catch (Exception ex2)
                {
                    _telemetryClient.TrackException(ex2);
                    _consentHandler.HandleException(ex2);
                }
            }
            return currentUser;
        }

        // GET: AdminController
        [AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
        public async Task<ActionResult> Index()
        {
            if (_user.Id != "146ed50e-8f34-47c3-82bd-f29ce70a87e6")
            {
                return RedirectToAction("Index", "Home");
            }
            IEnumerable<ToDoList> allItems = await _cosmosDbService.GetItemsAsync("SELECT * FROM c", _container);
            return View(allItems);
        }

        // GET: AdminController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: AdminController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: AdminController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: AdminController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: AdminController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: AdminController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: AdminController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}