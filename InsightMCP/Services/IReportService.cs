using InsightMCP.Models;

namespace InsightMCP.Services;

public interface IReportService
{
    /// <summary>
    /// Retrieves all medical reports with their associated questions and answers.
    /// </summary>
    /// <returns>A collection of Report objects</returns>
    Task<IEnumerable<Report>> GetReportsAsync();
    /// <summary>
    /// Retrieves a specific medical report by its case number.
    /// </summary>
    /// <param name="caseNumber">The case number to search for</param>
    /// <returns>The matching Report object or null if not found</returns>
    Task<Report?> GetReportAsync(string caseNumber);
}