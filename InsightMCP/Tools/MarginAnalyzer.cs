using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsightMCP.Models;
using InsightMCP.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace InsightMCP.Tools;

[McpServerToolType]
public class MarginAnalyzer
{
    private readonly IReportService _reportService;

    public MarginAnalyzer(IReportService reportService)
    {
        _reportService = reportService;
    }

[McpServerTool, Description("Analyzes the margin adequacy of reports.")]
    public async Task<MarginAnalysisResponse> AnalyzeMargins(
        DateTime startDate,
        DateTime endDate,
        string? procedureType = null,
        string? tumorType = null,
        double? tumorSize = null,
        string? benchmarkType = null)
    {
        ValidateParameters(startDate, endDate);

        var reports = await _reportService.GetReportsAsync();
        var filteredReports = FilterReports(reports.Items, startDate, endDate, procedureType, tumorType, tumorSize);

        var response = new MarginAnalysisResponse
        {
            TotalCases = filteredReports.Count,
            MarginAdequacyRate = CalculateMarginAdequacyRate(filteredReports),
            TrendingData = CalculateTrendingData(filteredReports),
            TumorCorrelations = CalculateTumorCorrelations(filteredReports)
        };

        if (!string.IsNullOrEmpty(benchmarkType))
        {
            response.BenchmarkComparison = await GetBenchmarkComparison(
                response.MarginAdequacyRate,
                benchmarkType,
                procedureType,
                tumorType);
        }

        return response;
    }

    private void ValidateParameters(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
        {
            throw new ArgumentException("Start date must be before end date");
        }
    }

    private List<Report> FilterReports(
        IEnumerable<Report> reports,
        DateTime startDate,
        DateTime endDate,
        string? procedureType,
        string? tumorType,
        double? tumorSize)
    {
        return reports
            .Where(r => r.Date >= startDate && r.Date <= endDate)
            .Where(r => string.IsNullOrEmpty(procedureType) || r.ProcedureType == procedureType)
            .Where(r => string.IsNullOrEmpty(tumorType) || r.TumorType == tumorType)
            .Where(r => !tumorSize.HasValue || r.TumorSize == tumorSize)
            .ToList();
    }

    private double CalculateMarginAdequacyRate(List<Report> reports)
    {
        if (!reports.Any())
        {
            return 0;
        }

        var negativeMargins = reports.Count(r => r.MarginStatus?.ToLower() == "negative");
        return (double)negativeMargins / reports.Count;
    }

    private List<TrendingDataPoint> CalculateTrendingData(List<Report> reports)
    {
        if (!reports.Any())
        {
            return new List<TrendingDataPoint>();
        }

        var trendingData = reports
            .GroupBy(r => new { r.Date?.Year, r.Date?.Month })
            .Where(g => g.Key.Year.HasValue && g.Key.Month.HasValue)
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Month)
            .Select(g =>
            {
                var monthReports = g.ToList();
                var negativeMargins = monthReports.Count(r => r.MarginStatus?.ToLower() == "negative");
                var adequacyRate = (double)negativeMargins / monthReports.Count;

                return new TrendingDataPoint
                {
                    Date = new DateTime(g.Key.Year.Value, g.Key.Month.Value, 1),
                    TotalCases = monthReports.Count,
                    NegativeMargins = negativeMargins,
                    AdequacyRate = adequacyRate
                };
            })
            .ToList();

        return trendingData;
    }

    private List<TumorCorrelation> CalculateTumorCorrelations(List<Report> reports)
    {
        return reports
            .GroupBy(r => r.TumorType)
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .Select(g => new TumorCorrelation
            {
                TumorType = g.Key!,
                TotalCases = g.Count(),
                AverageTumorSize = g.Average(r => r.TumorSize ?? 0),
                MarginAdequacyRate = (double)g.Count(r => r.MarginStatus?.ToLower() == "negative") / g.Count()
            })
            .ToList();
    }

    private Task<BenchmarkComparison> GetBenchmarkComparison(
        double currentRate,
        string benchmarkType,
        string? procedureType,
        string? tumorType)
    {
        // In a real implementation, this would fetch benchmark data from a database or external service
        var benchmarkRate = 0.85; // Example benchmark rate

        return Task.FromResult(new BenchmarkComparison
        {
            BenchmarkType = benchmarkType,
            BenchmarkRate = benchmarkRate,
            Difference = currentRate - benchmarkRate,
            IsAboveBenchmark = currentRate > benchmarkRate
        });
    }
}