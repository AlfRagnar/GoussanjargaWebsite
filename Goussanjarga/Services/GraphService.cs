using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Goussanjarga.Services
{
    public class GraphService : IGraphService
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly MicrosoftIdentityConsentAndConditionalAccessHandler _consentHandler;
        private readonly string[] _graphScopes = new[] { "user.read" };

        public GraphService(TelemetryClient telemtryClient,
                            IConfiguration configuration,
                            GraphServiceClient graphServiceClient,
                            MicrosoftIdentityConsentAndConditionalAccessHandler consentHandler)
        {
            _telemetryClient = telemtryClient;
            _graphServiceClient = graphServiceClient;
            _consentHandler = consentHandler;

            // Capture the Scopes for Graph that were used in the original request for an Access token (AT) for MS Graph as
            // they'd be needed again when requesting a fresh AT for Graph during claims challenge processing
            _graphScopes = configuration.GetValue<string>("DownstreamApi:Scopes")?.Split(' ');
        }

        public async Task<User> CurrentUserAsync()
        {
            User currentUser = null;
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
                    return new User();
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
                using Stream photoStream = await _graphServiceClient.Me.Photo.Content.Request().GetAsync();
                byte[] photoByte = ((MemoryStream)photoStream).ToArray();

                currentUser.Photo.AdditionalData = (IDictionary<string, object>)photoStream;
            }
            catch (Exception pex)
            {
                _telemetryClient.TrackException(pex);
            }

            return currentUser;
        }
    }
}