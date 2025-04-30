using InsightMCP.Models;
using InsightMCP.Services;
using InsightMCP.Tools;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InsightMCP.Tests.Tools;

public class FieldStatisticsGeneratorTests : IDisposable
{
    private readonly Mock<IReportService> _mockReportService;
    private readonly Mock<ILogger<FieldStatisticsGenerator>> _mockLogger;
    private readonly FieldStatisticsGenerator _generator;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    public FieldStatisticsGeneratorTests()
    {
        _mockReportService = new Mock<IReportService>();
        _mockLogger = new Mock<ILogger<FieldStatisticsGenerator>>();
        _generator = new FieldStatisticsGenerator(_mockReportService.Object, _mockLogger.Object);
        
        // Use same serialization settings as the implementation
        // Use the same options as in the implementation for consistent serialization
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    [Fact]
    public async Task AnalyzeFieldStatistics_ShouldReturnCorrectDistribution()
    {
        // Arrange
        var testData = CreateTestResults();
        _mockReportService.Setup(s => s.GetResultsAsync()).ReturnsAsync(testData);
        
        // Act
        var jsonResult = await _generator.AnalyzeFieldStatistics("Histologic Type");
        var result = JsonSerializer.Deserialize<FieldStatistics>(jsonResult, _jsonOptions);
        // Assert
        Assert.NotNull(result);
        Assert.Equal("Histologic Type", result.FieldName);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.Values.Count);
        
        // Check that the most frequent value is first
        Assert.Equal("Adenocarcinoma", result.Values[0].Value);
        Assert.Equal(2, result.Values[0].Count);
        Assert.Equal(66.67, Math.Round(result.Values[0].Percentage, 2));
        
        // Check the second value
        Assert.Equal("Squamous Cell Carcinoma", result.Values[1].Value);
        Assert.Equal(1, result.Values[1].Count);
        Assert.Equal(33.33, Math.Round(result.Values[1].Percentage, 2));
    }

    [Fact]
    public async Task AnalyzeFieldStatistics_ShouldBeCaseInsensitive()
    {
        // Arrange
        var testData = CreateTestResults();
        _mockReportService.Setup(s => s.GetResultsAsync()).ReturnsAsync(testData);
        
        // Act - use different casing than what's in the data
        var jsonResult = await _generator.AnalyzeFieldStatistics("histologic type");
        var result = JsonSerializer.Deserialize<FieldStatistics>(jsonResult, _jsonOptions);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("histologic type", result.FieldName);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.Values.Count);
    }

    [Fact]
    public async Task AnalyzeFieldStatistics_WithEmptyFieldName_ShouldThrowArgumentException()
    {
        // Arrange
        _mockReportService.Setup(s => s.GetResultsAsync()).ReturnsAsync(new List<Result>());
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _generator.AnalyzeFieldStatistics(""));
        await Assert.ThrowsAsync<ArgumentException>(() => _generator.AnalyzeFieldStatistics(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => _generator.AnalyzeFieldStatistics("   "));
    }

    [Fact]
    public async Task AnalyzeFieldStatistics_WithNoMatches_ShouldReturnEmptyStats()
    {
        // Arrange
        var testData = CreateTestResults();
        _mockReportService.Setup(s => s.GetResultsAsync()).ReturnsAsync(testData);
        
        // Act
        var jsonResult = await _generator.AnalyzeFieldStatistics("Non-Existent Field");
        var result = JsonSerializer.Deserialize<FieldStatistics>(jsonResult, _jsonOptions);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Non-Existent Field", result.FieldName);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Values);
    }

    [Fact]
    public async Task AnalyzeFieldStatistics_WithServiceException_ShouldPropagateException()
    {
        // Arrange
        _mockReportService.Setup(s => s.GetResultsAsync()).ThrowsAsync(new Exception("Test exception"));
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _generator.AnalyzeFieldStatistics("Histologic Type"));
        
        Assert.Equal("Test exception", exception.Message);
    }

    [Fact]
    public async Task AnalyzeFieldStatistics_ShouldLogErrors()
    {
        // Arrange
        _mockReportService.Setup(s => s.GetResultsAsync()).ThrowsAsync(new Exception("Test exception"));
        
        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _generator.AnalyzeFieldStatistics("Histologic Type"));
        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeFieldStatistics_ShouldReturnWellFormattedJson()
    {
        // Arrange
        var testData = CreateTestResults();
        _mockReportService.Setup(s => s.GetResultsAsync()).ReturnsAsync(testData);
        
        // Act
        var jsonResult = await _generator.AnalyzeFieldStatistics("Histologic Type");
        
        // Assert
        // Verify JSON can be parsed and has expected structure
        var jsonElement = JsonDocument.Parse(jsonResult).RootElement;
        
        Assert.True(jsonElement.TryGetProperty("fieldName", out var fieldNameElement));
        Assert.Equal("Histologic Type", fieldNameElement.GetString());
        Assert.True(jsonElement.TryGetProperty("totalCount", out _));
        Assert.True(jsonElement.TryGetProperty("values", out var valuesElement));
        Assert.True(valuesElement.ValueKind == JsonValueKind.Array);
        
        // Deserialize to verify the structure more robustly
        var result = JsonSerializer.Deserialize<FieldStatistics>(jsonResult, _jsonOptions);
        Assert.NotNull(result);
        Assert.Equal("Histologic Type", result.FieldName);
        Assert.NotEmpty(result.Values);
        
        // Check the properties of the first value
        var firstValue = result.Values.First();
        Assert.NotNull(firstValue.Value);
        Assert.True(firstValue.Count > 0);
        Assert.True(firstValue.Percentage > 0);
    }

    private static List<Result> CreateTestResults()
    {
        return new List<Result>
        {
            new Result
            {
                CaseNumber = "CASE001",
                ReportLOINCCode = "60568-3",
                ReportLOINCName = "Lung Cancer Synoptic Report",
                ProtocolName = "Lung Cancer Protocol",
                Question = "Histologic Type",
                Answer = "Adenocarcinoma"
            },
            new Result
            {
                CaseNumber = "CASE002",
                ReportLOINCCode = "60568-3",
                ReportLOINCName = "Lung Cancer Synoptic Report",
                ProtocolName = "Lung Cancer Protocol",
                Question = "Histologic Type",
                Answer = "Squamous Cell Carcinoma"
            },
            new Result
            {
                CaseNumber = "CASE003",
                ReportLOINCCode = "60568-3",
                ReportLOINCName = "Lung Cancer Synoptic Report",
                ProtocolName = "Lung Cancer Protocol",
                Question = "Histologic Type",
                Answer = "Adenocarcinoma"
            },
            new Result
            {
                CaseNumber = "CASE001",
                ReportLOINCCode = "60568-3",
                ReportLOINCName = "Lung Cancer Synoptic Report",
                ProtocolName = "Lung Cancer Protocol",
                Question = "Tumor Size",
                Answer = "3.5 cm"
            },
            new Result
            {
                CaseNumber = "CASE002",
                ReportLOINCCode = "60568-3",
                ReportLOINCName = "Lung Cancer Synoptic Report",
                ProtocolName = "Lung Cancer Protocol",
                Question = "Tumor Size",
                Answer = "2.1 cm"
            }
        };
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // No resources to clean up
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
