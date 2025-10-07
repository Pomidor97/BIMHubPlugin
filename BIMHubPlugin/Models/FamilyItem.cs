using System;
using System.Collections.Generic;
using Newtonsoft.Json;

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
        
        // ИСПРАВЛЕНО: правильные названия полей из API
        [JsonProperty("mainFileName")]
        public string MainFile { get; set; }
        
        [JsonProperty("previewImageName")]
        public string PreviewFile { get; set; }
        
        public List<string> AttachmentFileNames { get; set; } = new List<string>();
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsNew { get; set; }

        // Вычисляемые свойства для UI (не из JSON)
        [JsonIgnore]
        public string PreviewUrl { get; set; }
        
        [JsonIgnore]
        public string DownloadUrl { get; set; }
    }
}