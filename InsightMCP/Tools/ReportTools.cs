using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using InsightMCP.Models;
using InsightMCP.Services;
using CsvHelper;
using System.Globalization;

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

    [McpServerTool, Description("Get an Insight Report by case number.")]
    public async Task<string> InsightReport([Description("The case number of the Report to get details for")] string caseNumber)
    {
        try
        {
            _logger.LogInformation("InsightReport called for case number: {caseNumber}", caseNumber);
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

    [McpServerTool, Description("Export reports data to a CSV file for analytics")]
    public async Task<string> ExportAnalyticsCSV()
    {
        try
        {
            _logger.LogInformation("ExportAnalyticsCSV called");
            var reports = await ReportService.GetReportsAsync();
            
            // Create analytics records combining report and Q&A data
            var analyticsRecords = reports.SelectMany(report => 
                report.QuestionsAndAnswers.Select(qa => new
                {
                    CaseNumber = report.CaseNumber,
                    Protocol = report.Protocol,
                    ProtocolSource = report.ProtocolSource,
                    Question = qa.Question,
                    Answer = qa.Answer
                }));

            string outputPath = "analytics_export.csv";
            
            using (var writer = new StreamWriter(outputPath))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                await csv.WriteRecordsAsync(analyticsRecords);
            }

            _logger.LogInformation("Analytics data exported to {path}", outputPath);
            return $"Analytics data exported to {outputPath}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ExportAnalyticsCSV");
            throw;
        }
    }
}