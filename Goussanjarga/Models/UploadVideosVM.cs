using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Goussanjarga.Models
{
    public class UploadVideosVM
    {
        [Required(ErrorMessage = "Please enter a title for the video you're uploading")]
        public string Title { get; set; }

        [Display(Name = "Please enter a video description ( Optional )")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Please select a file")]
        public IFormFile File { get; set; }
    }
}