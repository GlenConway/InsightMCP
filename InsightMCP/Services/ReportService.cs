using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using System.Globalization;
using InsightMCP.Models;
using System.Text;
using Microsoft.Extensions.Logging;
using Serilog;

namespace InsightMCP.Services;

/// <summary>
/// Service for managing medical reports and their associated questions and answers.
/// Loads data from CSV files and caches it in memory.
/// </summary>
public class ReportService : IReportService
{
    private readonly string _resultsPath;
    private readonly ILogger<ReportService> _logger;
    private List<Report>? _reports;
    private List<Result>? _results;

    /// <summary>
    /// Initializes a new instance of the ReportService class and loads all data from CSV files.
    /// </summary>
    /// <param name="reportsPath">Path to the reports CSV file. Defaults to "reports.csv"</param>
    /// <param name="questionsAndAnswersPath">Path to the Q&A CSV file. Defaults to "q_and_a.csv"</param>
    /// <param name="resultsPath">Path to the results CSV file. Defaults to "results.csv"</param>
    public ReportService(ILogger<ReportService> logger, string resultsPath = "Data/results.csv")
    {
        _logger = logger;
        _resultsPath = resultsPath;
        Log.Information("LOG!");
        _logger.LogInformation("ReportService initialized with results path: {ResultsPath}", _resultsPath);
    }

    /// <summary>
    /// Ensures reports are loaded from the source file
    /// </summary>
    private async Task EnsureDataLoadedAsync()
    {
        if (_results == null)
        {
            _logger.LogInformation("Loading report data from {ResultsPath}", _resultsPath);
            try
            {
                using var reader = new StreamReader(_resultsPath);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                _results = await csv.GetRecordsAsync<Result>().ToListAsync();
                _logger.LogInformation("Successfully loaded {ResultCount} results from CSV", _results.Count);

                _reports = _results
                    .GroupBy(r => r.CaseNumber)
                    .Select(g => new Report
                    {
                        CaseNumber = g.Key,
                        ReportLOINCCode = g.First().ReportLOINCCode,
                        ReportLOINCName = g.First().ReportLOINCName,
                        ProtocolName = g.First().ProtocolName,
                        ReportText = string.Join("\n", g.Select(r => $"{r.Question}: {r.Answer}"))
                    })
                    .ToList();
                _logger.LogInformation("Generated {ReportCount} reports from results data", _reports.Count);
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex, "Results file not found at path: {ResultsPath}", _resultsPath);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading report data from {ResultsPath}", _resultsPath);
                throw;
            }
        }
    }

    /// <summary>
    /// Retrieves all pathology reports.
    /// </summary>
    /// <param name="pageSize">Number of items per page. Defaults to 10.</param>
    /// <param name="cursor">Base64 encoded cursor for pagination</param>
    /// <returns>A paged result containing reports</returns>
    public async Task<PagedResult<Report>> GetReportsAsync(int pageSize = 10, string? cursor = null)
    {
        _logger.LogInformation("Retrieving reports with pageSize: {PageSize}, cursor: {Cursor}", pageSize, cursor ?? "null");
        await EnsureDataLoadedAsync();

        var startIndex = 0;
        if (!string.IsNullOrEmpty(cursor))
        {
            startIndex = Convert.ToInt32(Encoding.UTF8.GetString(Convert.FromBase64String(cursor)));
        }

        var items = _reports!
            .Skip(startIndex)
            .Take(pageSize + 1)
            .ToList();

        var hasMore = items.Count > pageSize;
        if (hasMore)
        {
            items = items.Take(pageSize).ToList();
        }

        var nextCursor = hasMore ?
            Convert.ToBase64String(Encoding.UTF8.GetBytes((startIndex + pageSize).ToString())) :
            null;

        var result = new PagedResult<Report>
        {
            Items = items,
            NextCursor = nextCursor,
            HasMore = hasMore,
            TotalCount = _reports!.Count
        };
        
        _logger.LogInformation("Returning {ItemCount} reports (total: {TotalCount}, hasMore: {HasMore})", 
            items.Count, _reports!.Count, hasMore);
        return result;
    }

    /// <summary>
    /// Returns a list of distinct protocol names from all reports.
    /// </summary>
    /// <returns>A collection of protocol names</returns>
    public async Task<IEnumerable<string>> GetDistinctProtocolNamesAsync()
    {
        _logger.LogInformation("Retrieving distinct protocol names");
        await EnsureDataLoadedAsync();

        var protocolNames = _reports!
            .Select(r => r.ProtocolName)
            .Distinct()
            .OrderBy(name => name)
            .ToList();
            
        _logger.LogInformation("Found {ProtocolCount} distinct protocol names", protocolNames.Count);
        return protocolNames;
    }

    /// <summary>
    /// Retrieves reports filtered by protocol name.
    /// </summary>
    /// <param name="protocolName">The protocol name to filter by</param>
    /// <param name="pageSize">Number of items per page. Defaults to 10.</param>
    /// <param name="cursor">Base64 encoded cursor for pagination</param>
    /// <returns>A paged result containing filtered reports</returns>
    public async Task<PagedResult<Report>> GetReportsByProtocolAsync(string protocolName, int pageSize = 10, string? cursor = null)
    {
        _logger.LogInformation("Retrieving reports by protocol: {ProtocolName}, pageSize: {PageSize}, cursor: {Cursor}", 
            protocolName, pageSize, cursor ?? "null");
        await EnsureDataLoadedAsync();

        var startIndex = 0;
        if (!string.IsNullOrEmpty(cursor))
        {
            startIndex = Convert.ToInt32(Encoding.UTF8.GetString(Convert.FromBase64String(cursor)));
        }

        var filteredReports = _reports!
            .Where(r => r.ProtocolName.Equals(protocolName, StringComparison.OrdinalIgnoreCase))
            .ToList();
            
        _logger.LogInformation("Found {FilteredCount} reports matching protocol: {ProtocolName}", 
            filteredReports.Count, protocolName);
            
        if (filteredReports.Count == 0)
        {
            _logger.LogWarning("No reports found for protocol: {ProtocolName}", protocolName);
        }

        var items = filteredReports
            .Skip(startIndex)
            .Take(pageSize + 1)
            .ToList();

        var hasMore = items.Count > pageSize;
        if (hasMore)
        {
            items = items.Take(pageSize).ToList();
        }

        var nextCursor = hasMore ?
            Convert.ToBase64String(Encoding.UTF8.GetBytes((startIndex + pageSize).ToString())) :
            null;

        var result = new PagedResult<Report>
        {
            Items = items,
            NextCursor = nextCursor,
            HasMore = hasMore,
            TotalCount = filteredReports.Count
        };
        
        _logger.LogInformation("Returning {ItemCount} reports for protocol {ProtocolName} (total: {TotalCount}, hasMore: {HasMore})", 
            items.Count, protocolName, filteredReports.Count, hasMore);
        return result;
    }

    /// <summary>
    /// Retrieves all raw result records with questions and answers.
    /// </summary>
    /// <returns>A collection of Result objects</returns>
    public async Task<IEnumerable<Result>> GetResultsAsync()
    {
        _logger.LogInformation("Retrieving all raw results");
        await EnsureDataLoadedAsync();
        _logger.LogInformation("Returning {ResultCount} raw results", _results!.Count);
        return _results!;
    }
}
