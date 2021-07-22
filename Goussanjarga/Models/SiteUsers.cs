using Microsoft.Graph;
using Newtonsoft.Json;

namespace Goussanjarga.Models
{
    public class SiteUsers
    {
        [JsonProperty(PropertyName = "id")]
        public string id { get; set; }

        public string DisplayName { get; set; }
        public string Mail { get; set; }
        public string UserPrincipalName { get; set; }
        public ProfilePhoto ProfilePhoto { get; set; }
    }
}