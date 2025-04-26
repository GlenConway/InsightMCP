using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using InsightMCP.Models;
using InsightMCP.Services;

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

    [McpServerTool, Description("Get a list of Reports.")]
    public async Task<string> GetReports()
    {
        try
        {
            _logger.LogInformation("GetReports called");
            var reports = await ReportService.GetReportsAsync();
            _logger.LogInformation("Retrieved {count} reports", reports?.Count() ?? 0);
            var json = JsonSerializer.Serialize(reports, _jsonOptions);
            _logger.LogInformation("Serialized reports to JSON, length: {length}", json?.Length ?? 0);
            return json ?? "[]";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetReports");
            throw;
        }
    }

    [McpServerTool, Description("Get a Report by case number.")]
    public async Task<string> GetReport([Description("The case number of the Report to get details for")] string caseNumber)
    {
        try
        {
            _logger.LogInformation("GetReport called for case number: {caseNumber}", caseNumber);
            var report = await ReportService.GetReportAsync(caseNumber);
            _logger.LogInformation("Retrieved report: {found}", report != null);
            var json = JsonSerializer.Serialize(report, _jsonOptions);
            _logger.LogInformation("Serialized report to JSON, length: {length}", json?.Length ?? 0);
            return json ?? "null";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetReport for case number: {caseNumber}", caseNumber);
            throw;
        }
    }
}