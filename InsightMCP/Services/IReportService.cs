using System.Collections.Generic;
using System.Threading.Tasks;
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
    
    /// <summary>
    /// Retrieves all pathology reports from the Results.csv file.
    /// </summary>
    /// <returns>A collection of PathologyReport objects</returns>
    Task<IEnumerable<Result>> GetPathologyReportsAsync();
}
