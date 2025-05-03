using System;
using System.Collections.Generic;

namespace InsightMCP.Models;

public class MarginAnalysisResponse
{
    public int TotalCases { get; set; }
    public double MarginAdequacyRate { get; set; }
    public List<TrendingDataPoint> TrendingData { get; set; } = new();
    public List<TumorCorrelation> TumorCorrelations { get; set; } = new();
    public BenchmarkComparison? BenchmarkComparison { get; set; }
}

public class TrendingDataPoint
{
    public DateTime Date { get; set; }
    public int TotalCases { get; set; }
    public int NegativeMargins { get; set; }
    public double AdequacyRate { get; set; }
}

public class TumorCorrelation
{
    public string TumorType { get; set; } = string.Empty;
    public int TotalCases { get; set; }
    public double AverageTumorSize { get; set; }
    public double MarginAdequacyRate { get; set; }
}

public class BenchmarkComparison
{
    public string BenchmarkType { get; set; } = string.Empty;
    public double BenchmarkRate { get; set; }
    public double Difference { get; set; }
    public bool IsAboveBenchmark { get; set; }
} 