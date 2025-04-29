using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using System.Globalization;
using InsightMCP.Models;

namespace InsightMCP.Services;

/// <summary>
/// Service for managing medical reports and their associated questions and answers.
/// Loads data from CSV files and caches it in memory.
/// </summary>
public class ReportService : IReportService
{
    private readonly string _resultsPath;
    private List<Report> _reports;

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
    /// Retrieves all pathology reports.
    /// </summary>
    /// <returns>A collection of Report objects</returns>
    public async Task<IEnumerable<Report>> GetReportsAsync()
    {
        if (_reports != null)
        {
            return _reports;
        }

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

        return _reports;
    }
}