using System;
using System.IO;

namespace Goussanjarga.Models
{
    public class Uploads
    {
        public string FileName { get; set; }
        public DateTime UploadDate { get; set; }
        public FileInfo Size { get; set; }
        public FileInfo Extension { get; set; }
    }
}