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

    [McpServerTool, Description("Get a list of Insight Reports.")]
    public async Task<string> InsightReports()
    {
        try
        {
            _logger.LogInformation("InsightReports called");
            var reports = await ReportService.GetReportsAsync();
            _logger.LogInformation("Retrieved {count} reports", reports?.Count() ?? 0);
            var json = JsonSerializer.Serialize(reports, _jsonOptions);
            _logger.LogInformation("Serialized reports to JSON, length: {length}", json?.Length ?? 0);
            return json ?? "[]";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in InsightReports");
            throw;
        }
    }
}