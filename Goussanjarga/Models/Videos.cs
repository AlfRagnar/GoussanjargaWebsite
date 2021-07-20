using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Goussanjarga.Models
{
    public class Videos
    {
        // Set By the User
        [Required(ErrorMessage = "Please enter a video Title")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Please enter a video description")]
        public string Description { get; set; }

        // Set by the system upon file being uploaded
        public string Id { get; set; }

        public string FileName { get; set; }

        public Uri BlobUri { get; set; }

        public int VideoLength { get; set; }
        public DateTime UploadDate { get; set; }
        public FileInfo Size { get; set; }
        public FileInfo Extension { get; set; }
        public SiteUsers User { get; set; }
        public IFormFile File { get; set; }
    }
}