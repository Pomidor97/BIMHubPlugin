using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BIMHubPlugin.Models
{
    public class PagedResult<T>
    {
        [JsonProperty("families")]
        public List<T> Items { get; set; }
        
        [JsonProperty("totalCount")]
        public int TotalCount { get; set; }
        
        [JsonProperty("totalPages")]
        public int TotalPages { get; set; }
        
        [JsonProperty("currentPage")]
        public int Page { get; set; }
        
        [JsonProperty("pageSize")]
        public int PageSize { get; set; }
        
        [JsonProperty("hasNextPage")]
        public bool HasNextPage { get; set; }
        
        [JsonProperty("hasPreviousPage")]
        public bool HasPreviousPage { get; set; }
    }
}