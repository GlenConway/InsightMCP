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
public class StagingConcordance
{
    private readonly IReportService _reportService;

    public StagingConcordance(IReportService reportService)
    {
        _reportService = reportService;
    }

    [McpServerTool, Description("Analyzes concordance between clinical and pathological staging.")]
    public async Task<StagingConcordanceResponse> AnalyzeStagingConcordance(
        DateTime startDate,
        DateTime endDate,
        string? stagingSystem = null,
        string? tumorType = null,
        string? benchmarkType = null)
    {
        ValidateParameters(startDate, endDate);

        var reports = await _reportService.GetReportsAsync();
        var filteredReports = FilterReports(reports.Items, startDate, endDate, stagingSystem, tumorType);

        var response = new StagingConcordanceResponse
        {
            TotalCases = filteredReports.Count,
            OverallConcordanceRate = CalculateConcordanceRate(filteredReports),
            TrendingData = CalculateTrendingData(filteredReports),
            DiscordancePatterns = CalculateDiscordancePatterns(filteredReports)
        };

        if (!string.IsNullOrEmpty(benchmarkType))
        {
            response.BenchmarkComparison = await GetBenchmarkComparison(
                response.OverallConcordanceRate,
                benchmarkType,
                stagingSystem,
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
        string? stagingSystem,
        string? tumorType)
    {
        return reports
            .Where(r => r.Date >= startDate && r.Date <= endDate)
            .Where(r => !string.IsNullOrEmpty(r.ClinicalStage) && !string.IsNullOrEmpty(r.PathologicalStage))
            .Where(r => string.IsNullOrEmpty(stagingSystem) || r.StagingSystem == stagingSystem)
            .Where(r => string.IsNullOrEmpty(tumorType) || r.TumorType == tumorType)
            .ToList();
    }

    private double CalculateConcordanceRate(List<Report> reports)
    {
        if (!reports.Any())
        {
            return 0;
        }

        var concordantCases = reports.Count(r => r.ClinicalStage == r.PathologicalStage);
        return (double)concordantCases / reports.Count;
    }

    private List<StagingTrendingData> CalculateTrendingData(List<Report> reports)
    {
        if (!reports.Any())
        {
            return new List<StagingTrendingData>();
        }

        var trendingData = reports
            .GroupBy(r => new { r.Date?.Year, r.Date?.Month })
            .Where(g => g.Key.Year.HasValue && g.Key.Month.HasValue)
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Month)
            .Select(g =>
            {
                var monthReports = g.ToList();
                var concordantCases = monthReports.Count(r => r.ClinicalStage == r.PathologicalStage);
                var concordanceRate = (double)concordantCases / monthReports.Count;

                return new StagingTrendingData
                {
                    Date = new DateTime(g.Key.Year.Value, g.Key.Month.Value, 1),
                    TotalCases = monthReports.Count,
                    ConcordantCases = concordantCases,
                    ConcordanceRate = concordanceRate
                };
            })
            .ToList();

        return trendingData;
    }

    private List<DiscordancePattern> CalculateDiscordancePatterns(List<Report> reports)
    {
        var discordantReports = reports.Where(r => r.ClinicalStage != r.PathologicalStage).ToList();
        var totalDiscordant = discordantReports.Count;

        if (totalDiscordant == 0)
        {
            return new List<DiscordancePattern>();
        }

        return discordantReports
            .GroupBy(r => new { r.ClinicalStage, r.PathologicalStage })
            .Select(g => new DiscordancePattern
            {
                ClinicalStage = g.Key.ClinicalStage!,
                PathologicalStage = g.Key.PathologicalStage!,
                Frequency = g.Count(),
                Percentage = (double)g.Count() / totalDiscordant,
                CommonFactors = IdentifyCommonFactors(g.ToList())
            })
            .OrderByDescending(p => p.Frequency)
            .ToList();
    }

    private string? IdentifyCommonFactors(List<Report> reports)
    {
        var factors = new List<string>();

        // Analyze tumor size patterns
        var avgTumorSize = reports.Average(r => r.TumorSize ?? 0);
        if (avgTumorSize > 0)
        {
            factors.Add($"Average tumor size: {avgTumorSize:F1} cm");
        }

        // Analyze tumor type patterns
        var commonTumorType = reports
            .GroupBy(r => r.TumorType)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();
        if (commonTumorType != null && !string.IsNullOrEmpty(commonTumorType.Key))
        {
            factors.Add($"Common tumor type: {commonTumorType.Key}");
        }

        return factors.Any() ? string.Join(", ", factors) : null;
    }

    private Task<BenchmarkComparison> GetBenchmarkComparison(
        double currentRate,
        string benchmarkType,
        string? stagingSystem,
        string? tumorType)
    {
        // In a real implementation, this would fetch benchmark data from a database or external service
        var benchmarkRate = 0.75; // Example benchmark rate

        return Task.FromResult(new BenchmarkComparison
        {
            BenchmarkType = benchmarkType,
            BenchmarkRate = benchmarkRate,
            Difference = currentRate - benchmarkRate,
            IsAboveBenchmark = currentRate > benchmarkRate
        });
    }
} 