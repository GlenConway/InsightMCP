namespace InsightMCP.Models;
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public string? NextCursor { get; set; }
    public bool HasMore { get; set; }
    public int TotalCount { get; set; }
}