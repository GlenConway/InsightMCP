using InsightMCP.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using CsvHelper;
using System.Globalization;
using InsightMCP.Models;

namespace InsightMCP.Tools;

/// <summary>
/// Represents a single value distribution in field statistics
/// </summary>
public record ValueDistribution
{
    /// <summary>
    /// The field value
    /// </summary>
    [JsonPropertyName("value")]
    public required string Value { get; init; }
    
    /// <summary>
    /// Count of occurrences
    /// </summary>
    [JsonPropertyName("count")]
    public int Count { get; init; }
    
    /// <summary>
    /// Percentage of total occurrences
    /// </summary>
    [JsonPropertyName("percentage")]
    public double Percentage { get; init; }
}

/// <summary>
/// Represents statistics about a field's values
/// </summary>
public record FieldStatistics
{
    /// <summary>
    /// Name of the field being analyzed
    /// </summary>
    [JsonPropertyName("fieldName")]
    public required string FieldName { get; init; }
    
    /// <summary>
    /// Total number of occurrences of the field
    /// </summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; init; }
    
    /// <summary>
    /// Distribution of values
    /// </summary>
    [JsonPropertyName("values")]
    public required List<ValueDistribution> Values { get; init; }
}

[McpServerToolType]
public sealed class FieldStatisticsGenerator
{
    private readonly IReportService _reportService;
    private readonly ILogger<FieldStatisticsGenerator> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public FieldStatisticsGenerator(IReportService reportService, ILogger<FieldStatisticsGenerator> logger)
    {
        _reportService = reportService;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    [McpServerTool, Description("Analyzes the distribution of values for a specific field across all reports.")]
    public async Task<string> AnalyzeFieldStatistics(string fieldName)
    {
        try
        {
            _logger.LogInformation("AnalyzeFieldStatistics called for field: {fieldName}", fieldName);

            if (string.IsNullOrWhiteSpace(fieldName))
            {
                throw new ArgumentException("Field name cannot be empty", nameof(fieldName));
            }

            // Load raw results data
            var results = await _reportService.GetResultsAsync();
            
            // Filter results by the specified question/field
            var filteredResults = results
                .Where(r => r.Question.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            _logger.LogInformation("Found {count} occurrences of field: {fieldName}", filteredResults.Count, fieldName);

            if (filteredResults.Count == 0)
            {
                _logger.LogWarning("No data found for field: {fieldName}", fieldName);
                return JsonSerializer.Serialize(new FieldStatistics
                {
                    FieldName = fieldName,
                    TotalCount = 0,
                    Values = new List<ValueDistribution>()
                }, _jsonOptions);
            }

            // Calculate distribution
            var distribution = filteredResults
                .GroupBy(r => r.Answer)
                .Select(g => new ValueDistribution
                {
                    Value = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / filteredResults.Count * 100
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            // Create the statistics object with the original fieldName (preserving case)
            var response = new FieldStatistics
            {
                FieldName = fieldName, // Preserve original case
                TotalCount = filteredResults.Count,
                Values = distribution
            };

            _logger.LogInformation("Generated statistics for field: {fieldName} with {uniqueValues} unique values", 
                fieldName, distribution.Count);
            
            var json = JsonSerializer.Serialize(response, _jsonOptions);
            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AnalyzeFieldStatistics for field: {fieldName}", fieldName);
            throw;
        }
    }
}

