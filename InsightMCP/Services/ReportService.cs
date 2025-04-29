using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using System.Globalization;
using InsightMCP.Models;
using System.Text;

namespace InsightMCP.Services;

/// <summary>
/// Service for managing medical reports and their associated questions and answers.
/// Loads data from CSV files and caches it in memory.
/// </summary>
public class ReportService : IReportService
{
    private readonly string _resultsPath;
    private List<Report>? _reports;

    /// <summary>
    /// Initializes a new instance of the ReportService class and loads all data from CSV files.
    /// </summary>
    /// <param name="reportsPath">Path to the reports CSV file. Defaults to "reports.csv"</param>
    /// <param name="questionsAndAnswersPath">Path to the Q&A CSV file. Defaults to "q_and_a.csv"</param>
    /// <param name="resultsPath">Path to the results CSV file. Defaults to "results.csv"</param>
    public ReportService(string resultsPath = "Data/results.csv")
    {
        _resultsPath = resultsPath;
    }

    /// <summary>
    /// Ensures reports are loaded from the source file
    /// </summary>
    private async Task EnsureReportsLoadedAsync()
    {
        if (_reports == null)
        {
            using var reader = new StreamReader(_resultsPath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            var results = await csv.GetRecordsAsync<Result>().ToListAsync();

            _reports = results
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
        await EnsureReportsLoadedAsync();

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

        return new PagedResult<Report>
        {
            Items = items,
            NextCursor = nextCursor,
            HasMore = hasMore,
            TotalCount = _reports!.Count
        };
    }

    /// <summary>
    /// Returns a list of distinct protocol names from all reports.
    /// </summary>
    /// <returns>A collection of protocol names</returns>
    public async Task<IEnumerable<string>> GetDistinctProtocolNamesAsync()
    {
        await EnsureReportsLoadedAsync();

        return _reports!
            .Select(r => r.ProtocolName)
            .Distinct()
            .OrderBy(name => name);
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
        await EnsureReportsLoadedAsync();

        var startIndex = 0;
        if (!string.IsNullOrEmpty(cursor))
        {
            startIndex = Convert.ToInt32(Encoding.UTF8.GetString(Convert.FromBase64String(cursor)));
        }

        var filteredReports = _reports!
            .Where(r => r.ProtocolName.Equals(protocolName, StringComparison.OrdinalIgnoreCase));

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

        return new PagedResult<Report>
        {
            Items = items,
            NextCursor = nextCursor,
            HasMore = hasMore,
            TotalCount = filteredReports.Count()
        };
    }
}
