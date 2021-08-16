using Goussanjarga.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Management.Media.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Goussanjarga.Services
{
    public interface IAzMediaService
    {
        Task<Videos> CreateAsset(IFormFile fileToUpload, Videos videos);

        Task<StreamingLocator> CreateStreamingLocatorAsync(string assetName, string locatorName = null);

        Task<StreamingEndpoint> EnsureStreamingEndpoint(string Endpoint = "default");

        Task<Transform> GetOrCreateTransformAsync(string transformName = "GoussanAdaptiveStreamingPreset");

        Task<IList<string>> GetStreamingURL(string locatorName);
    }
}