using InsightMCP.Models;
using InsightMCP.Services;
using InsightMCP.Tools;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace InsightMCP.Tests.Tools;

public class ProtocolSearchFilterTests
{
    private readonly Mock<IReportService> _mockReportService;
    private readonly Mock<ILogger<ProtocolSearchFilter>> _mockLogger;
    private readonly ProtocolSearchFilter _filter;

    public ProtocolSearchFilterTests()
    {
        _mockReportService = new Mock<IReportService>();
        _mockLogger = new Mock<ILogger<ProtocolSearchFilter>>();
        _filter = new ProtocolSearchFilter(_mockReportService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task SearchProtocols_WithNoFilters_ReturnsAllProtocols()
    {
        // Arrange
        var protocols = new[] { "Protocol1", "Protocol2", "Protocol3" };
        var reports = protocols.Select(p => new Report { 
            ProtocolName = p, 
            ReportText = "Sample report text", 
            ReportLOINCName = "test-loinc", 
            ReportLOINCCode = "test-code",
            CaseNumber = "TEST-001" 
        }).ToList();
        
        SetupMockReportService(protocols, reports);
        
        // Act
        var result = await _filter.SearchProtocols(organSystem: null, year: null, version: null, pageSize: 10, cursor: null);
        var response = JsonSerializer.Deserialize<dynamic>(result);

        // Assert
        Assert.Equal(3, response!.GetProperty("totalCount").GetInt32());
        Assert.False(response.GetProperty("hasMore").GetBoolean());
    }

    [Fact]
    public async Task SearchProtocols_WithOrganSystem_FiltersCorrectly()
    {
        // Arrange
        var protocols = new[] 
        { 
            "Respiratory Protocol v1.0",
            "Cardiovascular Protocol v2.0",
            "Respiratory Protocol v2.0"
        };
        var reports = protocols.Select(p => new Report { 
            ProtocolName = p, 
            ReportText = "Sample report text", 
            ReportLOINCName = "test-loinc", 
            ReportLOINCCode = "test-code",
            CaseNumber = "TEST-001" 
        }).ToList();
        
        SetupMockReportService(protocols, reports);
        
        // Act
        var result = await _filter.SearchProtocols(organSystem: "Respiratory", year: null, version: null, pageSize: 10, cursor: null);
        var response = JsonSerializer.Deserialize<dynamic>(result);

        // Assert
        Assert.Equal(2, response!.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task SearchProtocols_WithYearFilter_FiltersCorrectly()
    {
        // Arrange
        var protocols = new[] 
        { 
            "Protocol 2022 v1.0",
            "Protocol 2023 v1.0",
            "Protocol 2023 v2.0"
        };
        var reports = protocols.Select(p => new Report { 
            ProtocolName = p, 
            ReportText = "Sample report text", 
            ReportLOINCName = "test-loinc", 
            ReportLOINCCode = "test-code",
            CaseNumber = "TEST-001" 
        }).ToList();
        
        SetupMockReportService(protocols, reports);
        
        // Act
        var result = await _filter.SearchProtocols(organSystem: null, year: 2023, version: null, pageSize: 10, cursor: null);
        var response = JsonSerializer.Deserialize<dynamic>(result);

        // Assert
        Assert.Equal(2, response!.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task SearchProtocols_WithVersionFilter_FiltersCorrectly()
    {
        // Arrange
        var protocols = new[] 
        { 
            "Protocol v1.0",
            "Protocol v2.0",
            "Protocol v2.1"
        };
        var reports = protocols.Select(p => new Report { 
            ProtocolName = p, 
            ReportText = "Sample report text", 
            ReportLOINCName = "test-loinc", 
            ReportLOINCCode = "test-code",
            CaseNumber = "TEST-001" 
        }).ToList();
        
        SetupMockReportService(protocols, reports);
        
        // Act
        var result = await _filter.SearchProtocols(organSystem: null, year: null, version: "v2.0", pageSize: 10, cursor: null);
        var response = JsonSerializer.Deserialize<dynamic>(result);

        // Assert
        Assert.Equal(1, response!.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task SearchProtocols_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var protocols = Enumerable.Range(1, 15)
            .Select(i => $"Protocol{i}")
            .ToArray();
        var reports = protocols.Select(p => new Report { 
            ProtocolName = p, 
            ReportText = "Sample report text", 
            ReportLOINCName = "test-loinc", 
            ReportLOINCCode = "test-code",
            CaseNumber = "TEST-001" 
        }).ToList();
        
        SetupMockReportService(protocols, reports);
        
        // Act
        var result = await _filter.SearchProtocols(organSystem: null, year: null, version: null, pageSize: 10, cursor: null);
        var response = JsonSerializer.Deserialize<dynamic>(result);

        // Assert
        Assert.Equal(15, response!.GetProperty("totalCount").GetInt32());
        Assert.True(response.GetProperty("hasMore").GetBoolean());
        Assert.NotNull(response.GetProperty("nextCursor").GetString());
    }

    [Fact]
    public async Task SearchProtocols_WithInvalidYear_ThrowsArgumentException()
    {
        // Arrange
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _filter.SearchProtocols(organSystem: null, year: 1800, version: null, pageSize: 10, cursor: null));
    }

    private void SetupMockReportService(string[] protocols, List<Report> reports)
    {
        _mockReportService
            .Setup(s => s.GetDistinctProtocolNamesAsync())
            .ReturnsAsync(protocols);

        foreach (var protocol in protocols)
        {
            var protocolReports = reports
                .Where(r => r.ProtocolName == protocol)
                .ToList();

            _mockReportService
                .Setup(s => s.GetReportsByProtocolAsync(
                    protocol, 
                    It.IsAny<int>(), 
                    It.IsAny<string>()))
                .ReturnsAsync(new PagedResult<Report>
                {
                    Items = protocolReports,
                    TotalCount = protocolReports.Count,
                    HasMore = false
                });
        }
    }
}

