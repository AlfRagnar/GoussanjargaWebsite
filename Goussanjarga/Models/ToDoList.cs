using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Goussanjarga.Models
{
    public class ToDoList
    {
        [Required]
        public string UserId { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "isComplete")]
        public bool Completed { get; set; }
    }
}