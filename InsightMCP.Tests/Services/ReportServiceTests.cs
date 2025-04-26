using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using InsightMCP.Services;
using InsightMCP.Models;
public class ReportServiceTests : IDisposable
{
    private readonly string _testReportsPath = "TestData/test_reports.csv";
    private readonly string _testQandAPath = "TestData/test_q_and_a.csv";
    private readonly ReportService _service;
    private readonly string _realReportsPath = "reports.csv";
    private readonly string _realQandAPath = "q_and_a.csv";

    public ReportServiceTests()
    {
        // Create test CSV files
        CreateTestReportsFile();
        CreateTestQandAFile();
        
        _service = new ReportService(_testReportsPath, _testQandAPath);
    }

    [Fact]
    public async Task GetReportsAsync_ShouldReturnAllReports()
    {
        // Act
        var reports = (await _service.GetReportsAsync()).ToList();

        // Assert
        Assert.NotNull(reports);
        Assert.Equal(2, reports.Count);
        Assert.Contains(reports, r => r.CaseNumber == "test1");
        Assert.Contains(reports, r => r.CaseNumber == "test2");
    }

    [Fact]
    public async Task GetReportAsync_WithValidCaseNumber_ShouldReturnReport()
    {
        // Act
        var report = await _service.GetReportAsync("test1");

        // Assert
        Assert.NotNull(report);
        Assert.Equal("test1", report.CaseNumber);
        Assert.Equal("Test Protocol", report.Protocol);
        Assert.Equal("CAP Ecc", report.ProtocolSource);
    }

    [Fact]
    public async Task GetReportAsync_WithInvalidCaseNumber_ShouldReturnNull()
    {
        // Act
        var report = await _service.GetReportAsync("nonexistent");

        // Assert
        Assert.Null(report);
    }

    [Fact]
    public async Task GetReportAsync_ShouldIncludeQuestionsAndAnswers()
    {
        // Act
        var report = await _service.GetReportAsync("test1");

        // Assert
        Assert.NotNull(report);
        Assert.NotNull(report?.QuestionsAndAnswers);
        Assert.Equal(2, report!.QuestionsAndAnswers.Count);
        Assert.Contains(report.QuestionsAndAnswers, qa => qa.Question == "Question1");
        Assert.Contains(report.QuestionsAndAnswers, qa => qa.Answer == "Answer1");
    }

    [Fact]
    public async Task LoadRealCsvFiles_ShouldSuccessfullyParseAndLoad()
    {
        // Arrange
        var realService = new ReportService(_realReportsPath, _realQandAPath);

        // Act
        var reports = (await realService.GetReportsAsync()).ToList();

        // Assert
        Assert.NotNull(reports);
        Assert.NotEmpty(reports);
        
        // Verify structure of loaded reports
        foreach (var report in reports)
        {
            Assert.NotNull(report.CaseNumber);
            Assert.NotNull(report.Protocol);
            Assert.NotNull(report.ProtocolSource);
            Assert.NotNull(report.QuestionsAndAnswers);
            
            // Verify each report has its questions and answers properly linked
            if (report.QuestionsAndAnswers.Any())
            {
                foreach (var qa in report.QuestionsAndAnswers)
                {
                    Assert.Equal(report.CaseNumber, qa.CaseNumber);
                }
            }
        }
    }

    private void CreateTestReportsFile()
    {
        var content = @"Case Number,Protocol,Protocol Source
test1,Test Protocol,CAP Ecc
test2,Another Protocol,RCPATH";
        
        Directory.CreateDirectory("TestData");
        File.WriteAllText(_testReportsPath, content);
    }

    private void CreateTestQandAFile()
    {
        var content = @"Case Number,Question,Answer
test1,Question1,Answer1
test1,Question2,Answer2
test2,Question3,Answer3";
        
        Directory.CreateDirectory("TestData");
        File.WriteAllText(_testQandAPath, content);
    }

    private bool _disposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Clean up test files
                if (File.Exists(_testReportsPath))
                    File.Delete(_testReportsPath);
                
                if (File.Exists(_testQandAPath))
                    File.Delete(_testQandAPath);

                if (Directory.Exists("TestData"))
                    Directory.Delete("TestData", true);
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