using System;

namespace BIMHubPlugin.Models;

public class FilterOptions
{
    public string Search { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? ManufacturerId { get; set; }
    public Guid? RevitVersionId { get; set; }
    public Guid? SectionId { get; set; }
    public string SortBy { get; set; } = "name";
    public string SortOrder { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}