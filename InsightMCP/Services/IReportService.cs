using InsightMCP.Models;

namespace InsightMCP.Services;

public interface IReportService
{
       
    /// <summary>
    /// Retrieves all pathology reports from the Results.csv file.
    /// </summary>
    /// <returns>A collection of PathologyReport objects</returns>
    Task<IEnumerable<Report>> GetReportsAsync();
}
