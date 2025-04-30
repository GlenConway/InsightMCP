using InsightMCP.Services;
using InsightMCP.Models;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace InsightMCP.Tools;

[McpServerToolType]
public sealed class ProtocolSearchFilter
{
    private readonly IReportService _reportService;
    private readonly ILogger<ProtocolSearchFilter> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProtocolSearchFilter(IReportService reportService, ILogger<ProtocolSearchFilter> logger)
    {
        _reportService = reportService;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReferenceHandler = ReferenceHandler.Preserve
        };
    }

    [McpServerTool, Description(@"Search for protocols using multiple criteria.

Parameters:
- organSystem (string, optional): Filter by organ system name. Case-insensitive text search.
- version (string, optional): Filter by version number (e.g., '1.0', 'v2'). Will match with or without 'v' prefix.
- pageSize (number, optional): Number of results per page. Default is 10, maximum is 100.
- cursor (string, optional): Base64-encoded pagination cursor for fetching subsequent pages. Use null for first page.

Example call:
SearchProtocols(""Breast"", 2022, ""1.0"", 25, null)

Response format:
{
  ""items"": [...],           // Array of protocol objects matching criteria
  ""nextCursor"": ""base64=="",  // Cursor to use for the next page (null if no more results)
  ""hasMore"": true/false,    // Whether there are more results available
  ""totalCount"": 42          // Total number of protocols matching criteria
}

Pagination notes: To retrieve subsequent pages, pass the nextCursor value from the previous response.")]
    public async Task<string> SearchProtocols(string? organSystem = null, int? year = null, string? version = null, int pageSize = 10, string? cursor = null)
    {
        try
        {
            _logger.LogInformation("SearchProtocols called with parameters: organSystem={OrganSystem}, year={Year}, version={Version}, pageSize={PageSize}, cursor={Cursor}", 
                organSystem, year, version, pageSize, cursor);

            // Validate parameters
            ValidateSearchParameters(organSystem, ref year, ref version, ref pageSize, ref cursor);

            // Get distinct protocol names first to filter more efficiently
            var protocolNames = await _reportService.GetDistinctProtocolNamesAsync();
            var filteredProtocolNames = FilterProtocolNames(protocolNames, organSystem, year, version);

            // Now get reports only for the filtered protocol names
            var allFilteredReports = new List<Report>();
            foreach (var protocolName in filteredProtocolNames)
            {
                var reports = await _reportService.GetReportsByProtocolAsync(protocolName, int.MaxValue - 1);
                allFilteredReports.AddRange(reports.Items);
            }

            // Handle pagination
            var startIndex = GetStartIndex(cursor);
            var totalCount = allFilteredReports.Count;

            var pagedItems = allFilteredReports
                .Skip(startIndex)
                .Take(pageSize + 1)
                .ToList();

            var hasMore = pagedItems.Count > pageSize;
            if (hasMore)
            {
                pagedItems = pagedItems.Take(pageSize).ToList();
            }

            var nextCursor = hasMore
                ? Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes(
                        (startIndex + pageSize).ToString()
                    )
                )
                : null;

            var response = new
            {
                Items = pagedItems,
                NextCursor = nextCursor,
                HasMore = hasMore,
                TotalCount = totalCount
            };

            var json = JsonSerializer.Serialize(response, _jsonOptions);
            _logger.LogInformation(
                "Retrieved {Count} protocols matching criteria", 
                pagedItems.Count
            );
            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SearchProtocols");
            throw;
        }
    }

    private void ValidateSearchParameters(string? organSystem, ref int? year, ref string? version, ref int pageSize, ref string? cursor)
    {
        if (pageSize <= 0) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        if (cursor == "null") cursor = null;

        if (year.HasValue && (year < 1900 || year > 2100))
            throw new ArgumentException("Year must be between 1900 and 2100", nameof(year));
    }

    private IEnumerable<string> FilterProtocolNames(IEnumerable<string> protocolNames, string? organSystem, int? year, string? version)
    {
        var filtered = protocolNames;

        if (!string.IsNullOrWhiteSpace(organSystem))
        {
            filtered = filtered.Where(p => 
                p.Contains(organSystem, StringComparison.OrdinalIgnoreCase));
        }

        if (year.HasValue)
        {
            var yearStr = year.ToString();
            filtered = filtered.Where(p => 
                Regex.IsMatch(p, $@"\b{yearStr}\b"));
        }

        if (!string.IsNullOrWhiteSpace(version))
        {
            // Match version patterns like v1.0, 2.1, etc.
            var versionPattern = version.StartsWith("v", StringComparison.OrdinalIgnoreCase)
                ? version
                : $"v?{version}";
            filtered = filtered.Where(p => 
                Regex.IsMatch(p, $@"\b{Regex.Escape(versionPattern)}\b", RegexOptions.IgnoreCase));
        }

        return filtered;
    }

    private int GetStartIndex(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor))
            return 0;

        return Convert.ToInt32(
            System.Text.Encoding.UTF8.GetString(
                Convert.FromBase64String(cursor)
            )
        );
    }
}
