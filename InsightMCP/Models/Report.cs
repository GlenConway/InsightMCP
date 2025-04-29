namespace InsightMCP.Models;

public class Report
{

    public required string CaseNumber { get; set; }

    public required string ReportLOINCCode { get; set; }

    public required string ReportLOINCName { get; set; }

    public required string ProtocolName { get; set; }

    public required string ReportText { get; set; }
}
