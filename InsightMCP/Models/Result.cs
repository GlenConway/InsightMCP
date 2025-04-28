using CsvHelper.Configuration.Attributes;

namespace InsightMCP.Models;

public class Result
{
    [Name("CaseNumber")]
    public required string CaseNumber { get; set; }
    
    [Name("ReportLoincCode")]
    public required string ReportLoincCode { get; set; }
    
    [Name("ReportLoincName")]
    public required string ReportLoincName { get; set; }
    
    [Name("Protocol Name")]
    public required string ProtocolName { get; set; }
    
    [Name("Question")]
    public required string Question { get; set; }
    
    [Name("QuestioncKey")]
    public string? QuestioncKey { get; set; }
    
    [Name("Answer")]
    public required string Answer { get; set; }
    
    [Name("AnswercKey")]
    public string? AnswercKey { get; set; }
}