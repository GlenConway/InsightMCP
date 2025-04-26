using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using System.Globalization;
using InsightMCP.Models;

namespace InsightMCP.Services;

/// <summary>
/// Service for managing medical reports and their associated questions and answers.
/// Loads data from CSV files and caches it in memory.
/// </summary>
public class ReportService : IReportService
{
    private readonly string _reportsPath;
    private readonly string _questionsAndAnswersPath;
    private readonly List<Report> _reports;

    /// <summary>
    /// Initializes a new instance of the ReportService class and loads all data from CSV files.
    /// </summary>
    /// <param name="reportsPath">Path to the reports CSV file. Defaults to "reports.csv"</param>
    /// <param name="questionsAndAnswersPath">Path to the Q&A CSV file. Defaults to "q_and_a.csv"</param>
    public ReportService(string reportsPath = "reports.csv", string questionsAndAnswersPath = "q_and_a.csv")
    {
        _reportsPath = reportsPath;
        _questionsAndAnswersPath = questionsAndAnswersPath;
        _reports = LoadReportsWithQuestionsAndAnswers().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Retrieves all medical reports with their associated questions and answers.
    /// </summary>
    /// <returns>A collection of Report objects</returns>
    public Task<IEnumerable<Report>> GetReportsAsync()
    {
        return Task.FromResult(_reports.AsEnumerable());
    }

    /// <summary>
    /// Retrieves a specific medical report by its case number.
    /// </summary>
    /// <param name="caseNumber">The case number to search for</param>
    /// <returns>The matching Report object or null if not found</returns>
    public Task<Report?> GetReportAsync(string caseNumber)
    {
        return Task.FromResult(_reports.FirstOrDefault(r => r.CaseNumber == caseNumber));
    }

    /// <summary>
    /// Loads reports and associates them with their corresponding questions and answers.
    /// </summary>
    /// <returns>A list of complete Report objects</returns>
    private async Task<List<Report>> LoadReportsWithQuestionsAndAnswers()
    {
        var reports = await LoadReportsAsync();
        var questionsAndAnswers = await LoadQuestionsAndAnswersAsync();

        foreach (var report in reports)
        {
            report.QuestionsAndAnswers = questionsAndAnswers
                .Where(qa => qa.CaseNumber == report.CaseNumber)
                .ToList();
        }

        return reports;
    }

    /// <summary>
    /// Loads the base report data from the CSV file.
    /// </summary>
    /// <returns>A list of Report objects without questions and answers</returns>
    private async Task<List<Report>> LoadReportsAsync()
    {
        using var reader = new StreamReader(_reportsPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        return await csv.GetRecordsAsync<Report>().ToListAsync();
    }

    /// <summary>
    /// Loads all questions and answers from the CSV file.
    /// </summary>
    /// <returns>A list of QuestionAndAnswer objects</returns>
    private async Task<List<QuestionAndAnswer>> LoadQuestionsAndAnswersAsync()
    {
        using var reader = new StreamReader(_questionsAndAnswersPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        return await csv.GetRecordsAsync<QuestionAndAnswer>().ToListAsync();
    }
}