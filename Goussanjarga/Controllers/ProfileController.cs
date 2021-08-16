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

        private readonly ICosmosDbService _cosmosDbService;
        private readonly Container _container;

        private readonly GraphServiceClient _graphServiceClient;
        private readonly MicrosoftIdentityConsentAndConditionalAccessHandler _consentHandler;

        public ProfileController(TelemetryClient telemtryClient,
                            IConfiguration configuration,
                            ICosmosDbService cosmosDbService,
                            GraphServiceClient graphServiceClient,
                            MicrosoftIdentityConsentAndConditionalAccessHandler consentHandler)
        {
            _telemetryClient = telemtryClient;

            _cosmosDbService = cosmosDbService;
            _container = _cosmosDbService.GetContainer(containerName);
            _graphServiceClient = graphServiceClient;
            _consentHandler = consentHandler;
            // Capture the Scopes for Graph that were used in the original request for an Access token (AT) for MS Graph as
            // they'd be needed again when requesting a fresh AT for Graph during claims challenge processing
            _graphScopes = configuration.GetValue<string>("DownstreamApi:Scopes")?.Split(' ');
        }

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
                    string claimChallenge = WwwAuthenticateParameters.GetClaimChallengeFromResponseHeaders(svcex.ResponseHeaders);
                    _consentHandler.ChallengeUser(_graphScopes, claimChallenge);
                }
                catch (Exception ex2)
                {
                    _telemetryClient.TrackException(ex2);
                    _consentHandler.HandleException(ex2);
                }
            }
            return View(currentUser);
        }
    }
}