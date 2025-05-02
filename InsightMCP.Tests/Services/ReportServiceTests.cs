using InsightMCP.Services;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
namespace InsightMCP.Tests;
public class ReportServiceTests : IDisposable
{
    private readonly string _baseDir = AppContext.BaseDirectory;
    private readonly string _testResultsPath = "TestData/results.csv";
    private readonly ReportService _service;
    private readonly string _realResultsPath;
    private readonly ILogger<ReportService> _logger;

    public ReportServiceTests()
    {
        // Set up real file paths
        _realResultsPath = Path.Combine("TestData", "results.csv");
        
        // Create a null logger for testing
        _logger = NullLogger<ReportService>.Instance;

        _service = new ReportService(_logger, _testResultsPath);
    }
    [Fact]
    public async Task GetReportsAsync_ShouldReturnAllReportsFromResultsFile()
    {
        // Arrange
        CreateTestResultsFile();
        var serviceWithResults = new ReportService(_logger, _testResultsPath);

        // Act
        var reports = (await serviceWithResults.GetReportsAsync()).Items.ToList();

        // Assert
        Assert.NotNull(reports);
        Assert.Equal(2, reports.Count());
        Assert.Contains(reports, r => r.CaseNumber == "result1");
        Assert.Contains(reports, r => r.ReportLOINCCode == "12345-6");
        Assert.Contains(reports, r => r.ReportLOINCName == "Test LOINC Name");
        Assert.Contains(reports, r => r.ProtocolName == "Test Protocol Name");
        Assert.Contains(reports, r => r.ReportText == "Result Question1: Result Answer1");
    }

    [Fact]
    public async Task GetReportsAsync_ShouldReturnPagedResults()
    {
        // Arrange
        CreateTestResultsFile();
        var serviceWithResults = new ReportService(_logger, _testResultsPath);
        const int pageSize = 1;

        // Act
        var firstPage = await serviceWithResults.GetReportsAsync(pageSize);
        var secondPage = await serviceWithResults.GetReportsAsync(pageSize, firstPage.NextCursor);

        // Assert
        Assert.NotNull(firstPage);
        Assert.NotNull(secondPage);
        Assert.Single(firstPage.Items);
        Assert.Single(secondPage.Items);
        Assert.NotEqual(firstPage.Items.First().CaseNumber, secondPage.Items.First().CaseNumber);
        Assert.True(firstPage.HasMore);
        Assert.Equal(2, firstPage.TotalCount);
    }

    [Fact]
    public async Task GetReportsAsync_LastPage_HasNoNextCursor()
    {
        // Arrange
        CreateTestResultsFile();
        var serviceWithResults = new ReportService(_logger, _testResultsPath);
        const int pageSize = 2;

        // Act
        var page = await serviceWithResults.GetReportsAsync(pageSize);

        // Assert
        Assert.NotNull(page);
        Assert.Equal(2, page.Items.Count());
        Assert.False(page.HasMore);
        Assert.Null(page.NextCursor);
    }

    [Fact]
    public async Task GetReportsAsync_WithInvalidCursor_ThrowsException()
    {
        // Arrange
        CreateTestResultsFile();
        var serviceWithResults = new ReportService(_logger, _testResultsPath);
        const string invalidCursor = "invalid_cursor";

        // Act & Assert
        await Assert.ThrowsAsync<FormatException>(() => 
            serviceWithResults.GetReportsAsync(cursor: invalidCursor));
    }

    private void CreateTestResultsFile()
    {
        var content = @"CaseNumber,ReportLoincCode,ReportLoincName,Protocol Name,Question,Answer,Date
result1,12345-6,Test LOINC Name,Test Protocol Name,Result Question1,Result Answer1,2024-01-01
result2,67890-1,Another LOINC Name,Another Protocol Name,Result Question2,Result Answer2,2024-01-02";

        Directory.CreateDirectory("TestData");
        File.WriteAllText(_testResultsPath, content);
    }

    [Fact]
    public async Task LoadRealCsvFiles_ShouldSuccessfullyParseAndLoad()
    {
        // Arrange
        CreateRealSampleFiles();

        // Create service with paths relative to current directory
        var realService = new ReportService(_logger, _realResultsPath);

        // Act
        var reports = (await realService.GetReportsAsync()).Items.ToList();

        // Assert
        Assert.NotNull(reports);
        Assert.NotEmpty(reports);
    }

    [Fact]
    public async Task GetDistinctProtocolNamesAsync_ShouldReturnUniqueProtocolNames()
    {
        // Arrange
        CreateTestResultsFile();
        var serviceWithResults = new ReportService(_logger, _testResultsPath);

        // Act
        var protocolNames = await serviceWithResults.GetDistinctProtocolNamesAsync();

        // Assert
        Assert.NotNull(protocolNames);
        var namesList = protocolNames.ToList();
        Assert.Equal(2, namesList.Count());
        Assert.Contains("Test Protocol Name", namesList);
        Assert.Contains("Another Protocol Name", namesList);
        Assert.True(namesList.SequenceEqual(namesList.OrderBy(n => n)), "Protocol names should be ordered alphabetically");
    }

    [Fact]
    public async Task GetReportsByProtocolAsync_ShouldReturnFilteredReports()
    {
        // Arrange
        CreateTestResultsFile();
        var serviceWithResults = new ReportService(_logger, _testResultsPath);

        // Act
        var reports = (await serviceWithResults.GetReportsByProtocolAsync("Test Protocol Name")).Items.ToList();

        // Assert
        Assert.NotNull(reports);
        Assert.Single(reports);
        Assert.All(reports, r => Assert.Equal("Test Protocol Name", r.ProtocolName));
    }

    [Fact]
    public async Task GetReportsByProtocolAsync_ShouldBeCaseInsensitive()
    {
        // Arrange
        CreateTestResultsFile();
        var serviceWithResults = new ReportService(_logger, _testResultsPath);

        // Act
        var reports = (await serviceWithResults.GetReportsByProtocolAsync("TEST PROTOCOL NAME")).Items.ToList();

        // Assert
        Assert.NotNull(reports);
        Assert.Single(reports);
        Assert.All(reports, r => Assert.Equal("Test Protocol Name", r.ProtocolName, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetReportsByProtocolAsync_WithNonExistentProtocol_ShouldReturnEmptyList()
    {
        // Arrange
        CreateTestResultsFile();
        var serviceWithResults = new ReportService(_logger, _testResultsPath);

        // Act
        var pagedResult = await serviceWithResults.GetReportsByProtocolAsync("Non Existent Protocol");

        // Assert
        Assert.NotNull(pagedResult);
        Assert.Empty(pagedResult.Items);
        Assert.False(pagedResult.HasMore);
        Assert.Null(pagedResult.NextCursor);
        Assert.Equal(0, pagedResult.TotalCount);
    }

    [Fact]
    public async Task GetReportsByProtocolAsync_ShouldProperlyPaginate()
    {
        // Arrange
        CreateTestResultsFileWithMultipleProtocols();
        var serviceWithResults = new ReportService(_logger, _testResultsPath);
        const int pageSize = 1;
        const string protocolName = "Test Protocol Name";

        // Act
        var firstPage = await serviceWithResults.GetReportsByProtocolAsync(protocolName, pageSize);
        var secondPage = await serviceWithResults.GetReportsByProtocolAsync(protocolName, pageSize, firstPage.NextCursor);

        // Assert
        Assert.NotNull(firstPage);
        Assert.NotNull(secondPage);
        Assert.Single(firstPage.Items);
        Assert.Single(secondPage.Items);
        Assert.All(firstPage.Items.Concat(secondPage.Items), r => Assert.Equal(protocolName, r.ProtocolName));
        Assert.NotEqual(firstPage.Items.First().CaseNumber, secondPage.Items.First().CaseNumber);
        Assert.True(firstPage.HasMore);
        Assert.False(secondPage.HasMore);
    }

    private void CreateTestResultsFileWithMultipleProtocols()
    {
        var content = @"CaseNumber,ReportLoincCode,ReportLoincName,Protocol Name,Question,Answer,Date
result1,12345-6,Test LOINC Name,Test Protocol Name,Result Question1,Result Answer1,2024-01-01
result2,67890-1,Another LOINC Name,Another Protocol Name,Result Question2,Result Answer2,2024-01-02
result3,12345-6,Test LOINC Name,Test Protocol Name,Result Question3,Result Answer3,2024-01-03";

        Directory.CreateDirectory("TestData");
        File.WriteAllText(_testResultsPath, content);
    }

    private void CreateRealSampleFiles()
    {
        try
        {
            // Ensure the output directory exists
            var directory = Path.GetDirectoryName(_realResultsPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);


            // Create sample results.csv file with explicit Windows line endings
            var resultsContent = "CaseNumber,ReportLoincCode,ReportLoincName,Protocol Name,Question,Answer,Date\r\n" +
                           "CASE001,60568-3,Lung Cancer Synoptic Report,Lung Cancer Protocol,Tumor Size,3.5 cm,2024-01-01\r\n" +
                           "CASE002,60569-1,Colorectal Cancer Synoptic Report,Colorectal Cancer Protocol,Lymph Node Status,Positive (2/12),2024-01-02\r\n" +
                           "CASE003,60570-9,Breast Cancer Synoptic Report,Breast Cancer Protocol,Estrogen Receptor Status,Positive (>90%),2024-01-03";

            File.WriteAllText(_realResultsPath, resultsContent, new UTF8Encoding(false));

            // Verify files were created
            if (!File.Exists(_realResultsPath))
                throw new IOException($"Failed to create results file at: {_realResultsPath}");

            // Verify file contents
            var createdResultsContent = File.ReadAllText(_realResultsPath);

            if (string.IsNullOrEmpty(createdResultsContent))
                throw new IOException("Results file was created but is empty");
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to create test files in {_baseDir}: {ex.Message}", ex);
        }
    }

    private bool _disposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                try
                {
                    if (Directory.Exists("TestData"))
                    {
                        // Force a small delay to ensure file handles are released
                        Thread.Sleep(100);
                        Directory.Delete("TestData", true);
                    }
                }
                catch (IOException)
                {
                    // If files are still in use, try one more time after a longer delay
                    try
                    {
                        Thread.Sleep(500);
                        if (Directory.Exists("TestData"))
                        {
                            Directory.Delete("TestData", true);
                        }
                    }
                    catch
                    {
                        // If cleanup still fails, ignore the error
                    }
                }
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