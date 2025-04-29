using InsightMCP.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InsightMCP.Tools;

[McpServerToolType]
public sealed class ReportTools
{
    private readonly IReportService ReportService;
    private readonly ILogger<ReportTools> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ReportTools(IReportService ReportService, ILogger<ReportTools> logger)
    {
        this.ReportService = ReportService;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReferenceHandler = ReferenceHandler.Preserve
        };
    }

    [McpServerTool, Description("Get a list of all Insight Reports with cursor-based paging.")]
    public async Task<string> AllInsightReports(int pageSize = 10, string? cursor = null)
    {
        try
        {
            _logger.LogInformation("InsightReports called with pageSize: {pageSize}, cursor: {cursor}", pageSize, cursor);

            if (cursor == "null")
            {
                cursor = null;
                _logger.LogInformation("Adjusted cursor: {cursor}", cursor);
            }
            if (pageSize <= 0)
            {
                pageSize = 10;
                _logger.LogInformation("Adjusted pageSize: {pageSize}", pageSize);
            }
            if (pageSize > 100)
            {
                pageSize = 100;

                _logger.LogInformation("Adjusted pageSize: {pageSize}", pageSize);
            }

            _logger.LogInformation("Fetching reports with pageSize: {pageSize}, cursor: {cursor}", pageSize, cursor);
            // Fetch reports with pagination
            var pagedResult = await ReportService.GetReportsAsync(pageSize, cursor);

            var response = new
            {
                Items = pagedResult.Items,
                NextCursor = pagedResult.NextCursor,
                HasMore = pagedResult.HasMore,
                TotalCount = pagedResult.TotalCount
            };

            _logger.LogInformation("Retrieved {count} reports", pagedResult.Items?.Count() ?? 0);
            var json = JsonSerializer.Serialize(response, _jsonOptions);
            _logger.LogInformation("Serialized reports to JSON, length: {length}", json?.Length ?? 0);
            return json ?? "[]";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in InsightReports");
            throw;
        }
    }

    [McpServerTool, Description("Get a list of all distinct protocol names from all Insight Reports.")]
    public async Task<string> AllInsightProtocolNames()
    {
        try
        {
            _logger.LogInformation("InsightProtocolNames called");

            var protocolNames = await ReportService.GetDistinctProtocolNamesAsync();

            _logger.LogInformation("Retrieved {count} distinct protocol names", protocolNames?.Count() ?? 0);
            var json = JsonSerializer.Serialize(protocolNames, _jsonOptions);
            _logger.LogInformation("Serialized protocol names to JSON, length: {length}", json?.Length ?? 0);
            return json ?? "[]";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in InsightProtocolNames");
            throw;
        }
    }

    [McpServerTool, Description("Get a list of Insight Reports filtered by protocol name with cursor-based paging.")]
    public async Task<string> InsightReportsByProtocol(string protocolName, int pageSize = 10, string? cursor = null)
    {
        try
        {
            _logger.LogInformation("InsightReportsByProtocol called with protocol: {protocolName}, pageSize: {pageSize}, cursor: {cursor}", 
                protocolName, pageSize, cursor);

            if (string.IsNullOrWhiteSpace(protocolName))
            {
                throw new ArgumentException("Protocol name cannot be empty", nameof(protocolName));
            }

            if (cursor == "null")
            {
                cursor = null;
                _logger.LogInformation("Adjusted cursor: {cursor}", cursor);
            }
            if (pageSize <= 0)
            {
                pageSize = 10;
                _logger.LogInformation("Adjusted pageSize: {pageSize}", pageSize);
            }
            if (pageSize > 100)
            {
                pageSize = 100;
                _logger.LogInformation("Adjusted pageSize: {pageSize}", pageSize);
            }

            _logger.LogInformation("Fetching reports for protocol: {protocolName} with pageSize: {pageSize}, cursor: {cursor}", 
                protocolName, pageSize, cursor);
            
            var pagedResult = await ReportService.GetReportsByProtocolAsync(protocolName, pageSize, cursor);

            var response = new
            {
                Items = pagedResult.Items,
                NextCursor = pagedResult.NextCursor,
                HasMore = pagedResult.HasMore,
                TotalCount = pagedResult.TotalCount
            };

            _logger.LogInformation("Retrieved {count} reports for protocol: {protocolName}", 
                pagedResult.Items?.Count() ?? 0, protocolName);
            var json = JsonSerializer.Serialize(response, _jsonOptions);
            _logger.LogInformation("Serialized reports to JSON, length: {length}", json?.Length ?? 0);
            return json ?? "[]";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in InsightReportsByProtocol");
            throw;
        }
    }
}
