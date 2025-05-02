using InsightMCP.Models;
using InsightMCP.Services;
using InsightMCP.Tools;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

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
            "Respiratory Protocol 2023 v1.0",
            "Cardiovascular Protocol 2022 v2.0",
            "Respiratory Protocol 2023 v2.0"
        };
        var reports = protocols.Select(p => new Report { 
            ProtocolName = p, 
            ReportText = "test-report", 
            ReportLOINCName = "test-loinc", 
            ReportLOINCCode = "test-code",
            CaseNumber = "TEST-001",
            Date = p.Contains("2023") ? new DateTime(2023, 1, 1) : new DateTime(2022, 1, 1)
        }).ToList();
        
        SetupMockReportService(protocols, reports);
        
        // Act
        var result = await _filter.SearchProtocols(organSystem: null, year: 2023, version: null, pageSize: 10, cursor: null);
        var response = JsonSerializer.Deserialize<dynamic>(result);

        // Assert
        Assert.Equal(2, response!.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task SearchProtocols_WithYearFilter_ReturnsMatchingReports()
    {
        // Arrange
        var protocolNames = new[] { "Breast Protocol 2023 v1.0", "Lung Protocol 2023 v2.0" };
        var reports = new[]
        {
            new Report { 
                ProtocolName = "Breast Protocol 2023 v1.0", 
                Date = new DateTime(2023, 1, 1),
                CaseNumber = "TEST-001",
                ReportLOINCCode = "test-code",
                ReportLOINCName = "test-loinc",
                ReportText = "Sample report text"
            },
            new Report { 
                ProtocolName = "Breast Protocol 2023 v1.0", 
                Date = new DateTime(2024, 1, 1),
                CaseNumber = "TEST-002",
                ReportLOINCCode = "test-code",
                ReportLOINCName = "test-loinc",
                ReportText = "Sample report text"
            },
            new Report { 
                ProtocolName = "Lung Protocol 2023 v2.0", 
                Date = new DateTime(2023, 6, 1),
                CaseNumber = "TEST-003",
                ReportLOINCCode = "test-code",
                ReportLOINCName = "test-loinc",
                ReportText = "Sample report text"
            }
        };

        _mockReportService
            .Setup(s => s.GetDistinctProtocolNamesAsync())
            .ReturnsAsync(protocolNames);

        _mockReportService
            .Setup(s => s.GetReportsByProtocolAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync((string protocol, int pageSize, string cursor) => new PagedResult<Report>
            {
                Items = reports.Where(r => r.ProtocolName == protocol).ToList(),
                TotalCount = reports.Count(r => r.ProtocolName == protocol),
                HasMore = false
            });

        // Act
        var result = await _filter.SearchProtocols(year: 2023);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReferenceHandler = ReferenceHandler.Preserve
        };
        var response = JsonSerializer.Deserialize<SearchResponse>(result, options);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(2, response.Items.Count());
        Assert.All(response.Items, item => Assert.Equal(2023, item.Date?.Year));
        Assert.Equal(2, response.TotalCount);
        Assert.False(response.HasMore);
        Assert.Null(response.NextCursor);
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
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _filter.SearchProtocols(year: 1899));
        
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _filter.SearchProtocols(year: 2101));
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

// Helper class to deserialize response
public class SearchResponse
{
    [JsonPropertyName("items")]
    public IEnumerable<Report> Items { get; set; } = Array.Empty<Report>();

    [JsonPropertyName("nextCursor")]
    public string? NextCursor { get; set; }

    [JsonPropertyName("hasMore")]
    public bool HasMore { get; set; }

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}

