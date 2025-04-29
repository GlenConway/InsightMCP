using InsightMCP.Models;

namespace InsightMCP.Services;

public interface IReportService
{
       
    /// <summary>
    /// Retrieves all pathology reports from the Results.csv file.
    /// </summary>
    /// <returns>A collection of PathologyReport objects</returns>
   Task<PagedResult<Report>> GetReportsAsync(int pageSize = 10, string? cursor = null);

    /// <summary>
    /// Returns a list of distinct protocol names from all reports.
    /// </summary>
    /// <returns>A collection of protocol names</returns>
    Task<IEnumerable<string>> GetDistinctProtocolNamesAsync();

    /// <summary>
    /// Retrieves reports filtered by protocol name.
    /// </summary>
    /// <param name="protocolName">The protocol name to filter by</param>
    /// <param name="pageSize">Number of items per page. Defaults to 10.</param>
    /// <param name="cursor">Base64 encoded cursor for pagination</param>
    /// <returns>A paged result containing filtered reports</returns>
    Task<PagedResult<Report>> GetReportsByProtocolAsync(string protocolName, int pageSize = 10, string? cursor = null);
}
