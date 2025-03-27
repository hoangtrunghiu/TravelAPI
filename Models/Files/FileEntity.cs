using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelAPI.Models.Files{
    public class FileEntity
    {
        [Key]
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? ContentType { get; set; }
        public long Size { get; set; }
        public string? Path { get; set; }
        public string Url { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? FolderId { get; set; }
        public Folder? Folder { get; set; }
    }
    public class FileDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? ContentType { get; set; }
        public long Size { get; set; }
        public string Url { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? FolderId { get; set; }
    }
    public class UploadFileDto
    {
        [Required]
        public IFormFile File { get; set; }
        public int? FolderId { get; set; }
    }
    public class UploadFileDto2
    {
        [Required]
        public IFormFile Upload { get; set; }
    }

}