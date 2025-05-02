using CsvHelper.Configuration.Attributes;

namespace InsightMCP.Models;

public class Result
{
    [Name("CaseNumber")]
    public required string CaseNumber { get; set; }

    [Name("ReportLoincCode")]
    public required string ReportLOINCCode { get; set; }

    [Name("ReportLoincName")]
    public required string ReportLOINCName { get; set; }

    [Name("Protocol Name")]
    public required string ProtocolName { get; set; }

    [Name("Question")]
    public required string Question { get; set; }

    [Name("Answer")]
    public required string Answer { get; set; }

    [Name("Date")]
    public DateTime? Date { get; set; }
}
