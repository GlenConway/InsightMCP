using InsightMCP.Services;
using System.IO;
using System.Text;
using System.Threading;

namespace InsightMCP.Tests;
public class ReportServiceTests : IDisposable
{
    private readonly string _baseDir = AppContext.BaseDirectory;
    private readonly string _testResultsPath = "TestData/results.csv";
    private readonly ReportService _service;
    private readonly string _realResultsPath;

    public ReportServiceTests()
    {
        // Set up real file paths
        _realResultsPath = Path.Combine("TestData", "results.csv");


        _service = new ReportService(_testResultsPath);
    }
    [Fact]
    public async Task GetReportsAsync_ShouldReturnAllReportsFromResultsFile()
    {
        // Arrange
        CreateTestResultsFile();
        var serviceWithResults = new ReportService(_testResultsPath);

        // Act
        var reports = (await serviceWithResults.GetReportsAsync()).ToList();

        // Assert
        Assert.NotNull(reports);
        Assert.Equal(2, reports.Count);
        Assert.Contains(reports, r => r.CaseNumber == "result1");
        Assert.Contains(reports, r => r.ReportLOINCCode == "12345-6");
        Assert.Contains(reports, r => r.ReportLOINCName == "Test LOINC Name");
        Assert.Contains(reports, r => r.ProtocolName == "Test Protocol Name");
        Assert.Contains(reports, r => r.Question == "Result Question1");
        Assert.Contains(reports, r => r.Answer == "Result Answer1");
    }

    private void CreateTestResultsFile()
    {
        var content = @"CaseNumber,ReportLoincCode,ReportLoincName,Protocol Name,Question,Answer
result1,12345-6,Test LOINC Name,Test Protocol Name,Result Question1,Result Answer1
result2,67890-1,Another LOINC Name,Another Protocol Name,Result Question2,Result Answer2";

        Directory.CreateDirectory("TestData");
        File.WriteAllText(_testResultsPath, content);
    }



    [Fact]
    public async Task LoadRealCsvFiles_ShouldSuccessfullyParseAndLoad()
    {
        // Arrange
        CreateRealSampleFiles();

        // Create service with paths relative to current directory
        var realService = new ReportService(_realResultsPath);

        // Act
        var reports = (await realService.GetReportsAsync()).ToList();

        // Assert
        Assert.NotNull(reports);
        Assert.NotEmpty(reports);
    }

    private void CreateRealSampleFiles()
    {
        try
        {
            // Ensure the output directory exists
            var directory = Path.GetDirectoryName(_realResultsPath);
            Directory.CreateDirectory(directory);


            // Create sample results.csv file with explicit Windows line endings
            var resultsContent = "CaseNumber,ReportLoincCode,ReportLoincName,Protocol Name,Question,Answer\r\n" +
                           "CASE001,60568-3,Lung Cancer Synoptic Report,Lung Cancer Protocol,Tumor Size,3.5 cm\r\n" +
                           "CASE002,60569-1,Colorectal Cancer Synoptic Report,Colorectal Cancer Protocol,Lymph Node Status,Positive (2/12)\r\n" +
                           "CASE003,60570-9,Breast Cancer Synoptic Report,Breast Cancer Protocol,Estrogen Receptor Status,Positive (>90%)";

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