using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsightMCP.Models;
using InsightMCP.Services;
using InsightMCP.Tools;
using Moq;
using Xunit;

namespace InsightMCP.Tests.Tools;

public class StagingConcordanceTests
{
    private readonly Mock<IReportService> _mockReportService;
    private readonly StagingConcordance _analyzer;

    public StagingConcordanceTests()
    {
        _mockReportService = new Mock<IReportService>();
        _analyzer = new StagingConcordance(_mockReportService.Object);
    }

    [Fact]
    public async Task AnalyzeStagingConcordance_WithValidParameters_ReturnsAnalysis()
    {
        // Arrange
        var startDate = DateTime.Now.AddMonths(-6);
        var endDate = DateTime.Now;
        var reports = new List<Report>
        {
            new Report
            {
                CaseNumber = "CASE-001",
                ReportLOINCCode = "12345-6",
                ReportLOINCName = "Breast Staging Report",
                ProtocolName = "Breast Protocol",
                ReportText = "Clinical Stage: IIA, Pathological Stage: IIA",
                Date = startDate.AddMonths(1),
                ProcedureType = "Lumpectomy",
                TumorType = "Invasive Ductal Carcinoma",
                TumorSize = 2.5,
                ClinicalStage = "IIA",
                PathologicalStage = "IIA",
                StagingSystem = "AJCC 8th Edition"
            },
            new Report
            {
                CaseNumber = "CASE-002",
                ReportLOINCCode = "12345-6",
                ReportLOINCName = "Breast Staging Report",
                ProtocolName = "Breast Protocol",
                ReportText = "Clinical Stage: IIB, Pathological Stage: IIIA",
                Date = startDate.AddMonths(2),
                ProcedureType = "Lumpectomy",
                TumorType = "Invasive Ductal Carcinoma",
                TumorSize = 3.0,
                ClinicalStage = "IIB",
                PathologicalStage = "IIIA",
                StagingSystem = "AJCC 8th Edition"
            },
            new Report
            {
                CaseNumber = "CASE-003",
                ReportLOINCCode = "12345-6",
                ReportLOINCName = "Breast Staging Report",
                ProtocolName = "Breast Protocol",
                ReportText = "Clinical Stage: I, Pathological Stage: I",
                Date = startDate.AddMonths(3),
                ProcedureType = "Mastectomy",
                TumorType = "Invasive Lobular Carcinoma",
                TumorSize = 4.0,
                ClinicalStage = "I",
                PathologicalStage = "I",
                StagingSystem = "AJCC 8th Edition"
            }
        };

        _mockReportService.Setup(x => x.GetReportsAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new PagedResult<Report>
            {
                Items = reports,
                TotalCount = reports.Count,
                HasMore = false
            });

        // Act
        var result = await _analyzer.AnalyzeStagingConcordance(
            startDate,
            endDate,
            "AJCC 8th Edition",
            "Invasive Ductal Carcinoma",
            "National");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCases);
        Assert.Equal(0.5, result.OverallConcordanceRate);
        Assert.Equal(2, result.TrendingData.Count);
        Assert.Single(result.DiscordancePatterns);
        Assert.NotNull(result.BenchmarkComparison);
    }

    [Fact]
    public async Task AnalyzeStagingConcordance_WithNoMatchingReports_ReturnsEmptyAnalysis()
    {
        // Arrange
        var startDate = DateTime.Now.AddMonths(-6);
        var endDate = DateTime.Now;
        var reports = new List<Report>();

        _mockReportService.Setup(x => x.GetReportsAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new PagedResult<Report>
            {
                Items = reports,
                TotalCount = 0,
                HasMore = false
            });

        // Act
        var result = await _analyzer.AnalyzeStagingConcordance(
            startDate,
            endDate,
            "AJCC 8th Edition",
            null,
            null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCases);
        Assert.Equal(0, result.OverallConcordanceRate);
        Assert.Empty(result.TrendingData);
        Assert.Empty(result.DiscordancePatterns);
        Assert.Null(result.BenchmarkComparison);
    }

    [Fact]
    public async Task AnalyzeStagingConcordance_WithInvalidDateRange_ThrowsArgumentException()
    {
        // Arrange
        var startDate = DateTime.Now;
        var endDate = DateTime.Now.AddMonths(-1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _analyzer.AnalyzeStagingConcordance(startDate, endDate, null, null, null));
    }

    [Fact]
    public async Task AnalyzeStagingConcordance_WithDiscordantCases_IdentifiesPatterns()
    {
        // Arrange
        var startDate = DateTime.Now.AddMonths(-6);
        var endDate = DateTime.Now;
        var reports = new List<Report>
        {
            new Report
            {
                CaseNumber = "CASE-001",
                ReportLOINCCode = "12345-6",
                ReportLOINCName = "Breast Staging Report",
                ProtocolName = "Breast Protocol",
                ReportText = "Clinical Stage: IIA, Pathological Stage: IIIA",
                Date = startDate.AddMonths(1),
                ProcedureType = "Lumpectomy",
                TumorType = "Invasive Ductal Carcinoma",
                TumorSize = 2.5,
                ClinicalStage = "IIA",
                PathologicalStage = "IIIA",
                StagingSystem = "AJCC 8th Edition"
            },
            new Report
            {
                CaseNumber = "CASE-002",
                ReportLOINCCode = "12345-6",
                ReportLOINCName = "Breast Staging Report",
                ProtocolName = "Breast Protocol",
                ReportText = "Clinical Stage: IIA, Pathological Stage: IIIA",
                Date = startDate.AddMonths(2),
                ProcedureType = "Lumpectomy",
                TumorType = "Invasive Ductal Carcinoma",
                TumorSize = 3.0,
                ClinicalStage = "IIA",
                PathologicalStage = "IIIA",
                StagingSystem = "AJCC 8th Edition"
            }
        };

        _mockReportService.Setup(x => x.GetReportsAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new PagedResult<Report>
            {
                Items = reports,
                TotalCount = reports.Count,
                HasMore = false
            });

        // Act
        var result = await _analyzer.AnalyzeStagingConcordance(
            startDate,
            endDate,
            "AJCC 8th Edition",
            "Invasive Ductal Carcinoma",
            null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCases);
        Assert.Equal(0, result.OverallConcordanceRate);
        Assert.Single(result.DiscordancePatterns);
        var pattern = result.DiscordancePatterns.First();
        Assert.Equal("IIA", pattern.ClinicalStage);
        Assert.Equal("IIIA", pattern.PathologicalStage);
        Assert.Equal(2, pattern.Frequency);
        Assert.Equal(1.0, pattern.Percentage);
        Assert.Contains("Average tumor size", pattern.CommonFactors);
        Assert.Contains("Common tumor type", pattern.CommonFactors);
    }
} 