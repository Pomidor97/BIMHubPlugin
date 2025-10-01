using System;
using System.Collections.Generic;

namespace BIMHubPlugin.Models
{
    public class FamilyItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string NameRfa { get; set; }
        public string Description { get; set; }
        
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; }
        
        public Guid SectionId { get; set; }
        public string SectionName { get; set; }
        
        public Guid RevitVersionId { get; set; }
        public string RevitVersionName { get; set; }
        
        public Guid ManufacturerId { get; set; }
        public string ManufacturerName { get; set; }
        
        public string MainFile { get; set; }           // Имя файла .rfa
        public string PreviewFile { get; set; }        // Имя файла превью
        public List<string> Attachments { get; set; } = new List<string>();
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Вычисляемые свойства для UI
        public string PreviewUrl { get; set; }         // Полный URL превью
        public string DownloadUrl { get; set; }        // Полный URL скачивания
    }
}