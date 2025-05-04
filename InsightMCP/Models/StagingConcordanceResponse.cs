using System;
using System.Collections.Generic;

namespace InsightMCP.Models;

public class StagingConcordanceResponse
{
    public int TotalCases { get; set; }
    public double OverallConcordanceRate { get; set; }
    public List<StagingTrendingData> TrendingData { get; set; } = new();
    public List<DiscordancePattern> DiscordancePatterns { get; set; } = new();
    public BenchmarkComparison? BenchmarkComparison { get; set; }
}

public class StagingTrendingData
{
    public DateTime Date { get; set; }
    public int TotalCases { get; set; }
    public int ConcordantCases { get; set; }
    public double ConcordanceRate { get; set; }
}

public class DiscordancePattern
{
    public string ClinicalStage { get; set; } = string.Empty;
    public string PathologicalStage { get; set; } = string.Empty;
    public int Frequency { get; set; }
    public double Percentage { get; set; }
    public string? CommonFactors { get; set; }
} 