using System.Collections.Generic;
using System.Linq;

namespace XRAY_ULTIMATE.Models;

public class PropertyItem
{
    public string Category { get; set; } = string.Empty;
    public string Property { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public PropertyItem() { }

    public PropertyItem(string category, string property, string value, string unit = "", string status = "")
    {
        Category = category;
        Property = property;
        Value = value;
        Unit = unit;
        Status = status;
    }

    public string DisplayValue => string.IsNullOrEmpty(Unit) ? Value : $"{Value} {Unit}";

    public static List<PropertyGroup> GroupByCategory(IEnumerable<PropertyItem> items)
    {
        return items
            .GroupBy(p => p.Category)
            .Select(g => new PropertyGroup(g.Key, g.ToList()))
            .ToList();
    }
}

public class PropertyGroup
{
    public string Category { get; set; } = string.Empty;
    public List<PropertyItem> Items { get; set; } = new();

    public PropertyGroup() { }

    public PropertyGroup(string category, List<PropertyItem> items)
    {
        Category = category;
        Items = items;
    }
}
