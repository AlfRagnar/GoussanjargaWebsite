using Goussanjarga.Models;
using Goussanjarga.Services;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using System;
using System.Threading.Tasks;

namespace Goussanjarga.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly string[] _graphScopes = new[] { "user.read" };
        private readonly string containerName = "User";
        private readonly TelemetryClient _telemetryClient;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ICosmosDbService _cosmosDbService;
        private readonly Container _container;
        private readonly MicrosoftIdentityConsentAndConditionalAccessHandler _consentHandler;

        public ProfileController(TelemetryClient telemtryClient,
                            IConfiguration configuration,
                            ICosmosDbService cosmosDbService,
                            GraphServiceClient graphServiceClient,
                            MicrosoftIdentityConsentAndConditionalAccessHandler consentHandler)
        {
            _telemetryClient = telemtryClient;
            _graphServiceClient = graphServiceClient;
            _cosmosDbService = cosmosDbService;
            _container = _cosmosDbService.GetContainer(containerName);
            _consentHandler = consentHandler;

            // Capture the Scopes for Graph that were used in the original request for an Access token (AT) for MS Graph as
            // they'd be needed again when requesting a fresh AT for Graph during claims challenge processing
            _graphScopes = configuration.GetValue<string>("DownstreamApi:Scopes")?.Split(' ');
        }

        //[AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
        //public IActionResult Index()
        //{
        //    return View();
        //}

        [AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
        public async Task<IActionResult> Index()
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

            try
            {
                // Get user photo
                //using var photoStream = await _graphServiceClient.Me.Photo.Content.Request().GetAsync();
                //photoByte = ((MemoryStream)photoStream).ToArray();
                currentUser.Photo = await _graphServiceClient.Me.Photo.Request().GetAsync();
            }
            catch (Exception ex)
            {
                _telemetryClient.TrackException(ex);
            }

            return View(currentUser);
        }

        [HttpPost]
        [AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
        public async Task<ActionResult> AddCosmosAsync(Microsoft.Graph.User siteUsers)
        {
            //Microsoft.Graph.User siteUsers = null;
            try
            {
                if (siteUsers.Id == null)
                {
                    // Get the current user from MS Graph
                    siteUsers = await _graphServiceClient.Me.Request().GetAsync();
                }
            }
            // Catch CAE exception from Graph SDK
            catch (ServiceException svcex) when (svcex.Message.Contains("Continuous access evaluation resulted in claims challenge"))
            {
                try
                {
                    // Try to solve Claims Challenge
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
            try
            {
                // Get user photo
                //using var photoStream = await _graphServiceClient.Me.Photo.Content.Request().GetAsync();
                //photoByte = ((MemoryStream)photoStream).ToArray();
                siteUsers.Photo = await _graphServiceClient.Me.Photo.Request().GetAsync();
            }
            catch (Exception ex)
            {
                _telemetryClient.TrackException(ex);
            }
            if (siteUsers.Id == null || siteUsers == null)
            {
                SiteUsers cosmosUser = new()
                {
                    id = siteUsers.Id.ToString(),
                    DisplayName = siteUsers.DisplayName.ToString(),
                    Mail = siteUsers.Mail.ToString(),
                    UserPrincipalName = siteUsers.UserPrincipalName.ToString(),
                    ProfilePhoto = siteUsers.Photo
                };

                try
                {
                    // Try to add the user to Cosmos DB
                    await _cosmosDbService.AddUser(cosmosUser, _container);
                }
                catch (Exception ex)
                {
                    _telemetryClient.TrackException(ex);
                }
            }
            return RedirectToAction("Index", "Home");
        }
    }
}