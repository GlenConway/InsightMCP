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

public class MarginAnalyzerTests
{
    private readonly Mock<IReportService> _mockReportService;
    private readonly MarginAnalyzer _analyzer;

    public MarginAnalyzerTests()
    {
        _mockReportService = new Mock<IReportService>();
        _analyzer = new MarginAnalyzer(_mockReportService.Object);
    }

    [Fact]
    public async Task AnalyzeMargins_WithValidParameters_ReturnsAnalysis()
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
                ReportLOINCName = "Breast Margin Report",
                ProtocolName = "Breast Protocol",
                ReportText = "Margin: Negative",
                Date = startDate.AddMonths(1),
                ProcedureType = "Lumpectomy",
                TumorType = "Invasive Ductal Carcinoma",
                TumorSize = 2.5,
                MarginStatus = "Negative"
            },
            new Report
            {
                CaseNumber = "CASE-002",
                ReportLOINCCode = "12345-6",
                ReportLOINCName = "Breast Margin Report",
                ProtocolName = "Breast Protocol",
                ReportText = "Margin: Positive",
                Date = startDate.AddMonths(2),
                ProcedureType = "Lumpectomy",
                TumorType = "Invasive Ductal Carcinoma",
                TumorSize = 3.0,
                MarginStatus = "Positive"
            },
            new Report
            {
                CaseNumber = "CASE-003",
                ReportLOINCCode = "12345-6",
                ReportLOINCName = "Breast Margin Report",
                ProtocolName = "Breast Protocol",
                ReportText = "Margin: Negative",
                Date = startDate.AddMonths(3),
                ProcedureType = "Mastectomy",
                TumorType = "Invasive Lobular Carcinoma",
                TumorSize = 4.0,
                MarginStatus = "Negative"
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
        var result = await _analyzer.AnalyzeMargins(
            startDate,
            endDate,
            "Lumpectomy",
            "Invasive Ductal Carcinoma",
            null,
            "National");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCases);
        Assert.Equal(0.5, result.MarginAdequacyRate);
        Assert.Equal(2, result.TrendingData.Count);
        Assert.Single(result.TumorCorrelations);
        Assert.NotNull(result.BenchmarkComparison);
    }

    [Fact]
    public async Task AnalyzeMargins_WithNoMatchingReports_ReturnsEmptyAnalysis()
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
        var result = await _analyzer.AnalyzeMargins(
            startDate,
            endDate,
            "Lumpectomy",
            null,
            null,
            null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCases);
        Assert.Equal(0, result.MarginAdequacyRate);
        Assert.Empty(result.TrendingData);
        Assert.Empty(result.TumorCorrelations);
        Assert.Null(result.BenchmarkComparison);
    }

    [Fact]
    public async Task AnalyzeMargins_WithInvalidDateRange_ThrowsArgumentException()
    {
        // Arrange
        var startDate = DateTime.Now;
        var endDate = DateTime.Now.AddMonths(-1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _analyzer.AnalyzeMargins(startDate, endDate, null, null, null, null));
    }

    [Fact]
    public async Task AnalyzeMargins_WithTumorSizeFilter_ReturnsFilteredAnalysis()
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
                ReportLOINCName = "Breast Margin Report",
                ProtocolName = "Breast Protocol",
                ReportText = "Margin: Negative",
                Date = startDate.AddMonths(1),
                ProcedureType = "Lumpectomy",
                TumorType = "Invasive Ductal Carcinoma",
                TumorSize = 2.5,
                MarginStatus = "Negative"
            },
            new Report
            {
                CaseNumber = "CASE-002",
                ReportLOINCCode = "12345-6",
                ReportLOINCName = "Breast Margin Report",
                ProtocolName = "Breast Protocol",
                ReportText = "Margin: Positive",
                Date = startDate.AddMonths(2),
                ProcedureType = "Lumpectomy",
                TumorType = "Invasive Ductal Carcinoma",
                TumorSize = 3.0,
                MarginStatus = "Positive"
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
        var result = await _analyzer.AnalyzeMargins(
            startDate,
            endDate,
            "Lumpectomy",
            "Invasive Ductal Carcinoma",
            2.5,
            null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCases);
        Assert.Equal(1.0, result.MarginAdequacyRate);
    }
}