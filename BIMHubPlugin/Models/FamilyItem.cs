using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;

namespace BIMHubPlugin.Models
{
    /// <summary>
    /// Семейство каталога. Объединяет поля списка (FamilyListItemDto) и карточки
    /// (FamilyDetailDto) нового Catalog API BimHelpDesk — часть полей заполняется
    /// только при загрузке через GetFamilyByIdAsync (Description, MainFileDisplayName,
    /// MainFileSizeBytes, Attachments).
    /// В отличие от старого BimHub, API не отдаёт сырые имена файлов на диске —
    /// только факт наличия (HasPreview) и защищённые эндпоинты скачивания по Id.
    /// </summary>
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

        public bool HasPreview { get; set; }

        /// <summary>Заполняется только в карточке (GetFamilyByIdAsync).</summary>
        public string MainFileDisplayName { get; set; }
        /// <summary>Заполняется только в карточке (GetFamilyByIdAsync).</summary>
        public long MainFileSizeBytes { get; set; }

        public int DownloadCount { get; set; }
        public List<FamilyAttachment> Attachments { get; set; } = new List<FamilyAttachment>();

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Вычисляемые свойства для UI (не из JSON) — строятся клиентом по Id,
        // т.к. API больше не отдаёт прямые имена файлов (раздел 7.1 плана объединения).
        [JsonIgnore]
        public string PreviewUrl { get; set; }

        [JsonIgnore]
        public string DownloadUrl { get; set; }

        /// <summary>
        /// Превью-миниатюра, скачанная и распакованная клиентом (см. CatalogApiClient.GetFamiliesAsync).
        /// Эндпоинт превью защищён Bearer-токеном, поэтому прямой WPF-биндинг Image.Source на URL
        /// не сработает — картинка должна быть уже загружена в память к моменту отрисовки списка.
        /// </summary>
        [JsonIgnore]
        public BitmapImage PreviewImage { get; set; }
    }

    public class FamilyAttachment
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; }
        public string ContentType { get; set; }
        public long SizeBytes { get; set; }
    }
}
