using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Goussanjarga.Models
{
    public class Uploads
    {
        // Set By the User
        [Required]
        public string Title { get; set; }
        [Required]
        public string Description { get; set; }
        // Set by the system upon file being uploaded
        public string FileName { get; set; }
        public DateTime UploadDate { get; set; }
        public FileInfo Size { get; set; }
        public FileInfo Extension { get; set; }
        public User User { get; set; }
    }
}