using System.Collections.Generic;
using Newtonsoft.Json;

namespace BIMHubPlugin.Models
{
    /// <summary>Форма ответа нового Catalog API (BimHelpDesk.Application.Common.Models.PagedResult&lt;T&gt;).</summary>
    public class PagedResult<T>
    {
        [JsonProperty("items")]
        public List<T> Items { get; set; }

        [JsonProperty("totalCount")]
        public int TotalCount { get; set; }

        [JsonProperty("totalPages")]
        public int TotalPages { get; set; }

        [JsonProperty("page")]
        public int Page { get; set; }

        [JsonProperty("pageSize")]
        public int PageSize { get; set; }
    }
}
