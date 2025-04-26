using CsvHelper.Configuration.Attributes;

namespace InsightMCP.Models;

public class Report
{
    [Name("Case Number")]
    public required string CaseNumber { get; set; }
    
    [Name("Protocol")]
    public required string Protocol { get; set; }
    
    [Name("Protocol Source")]
    public required string ProtocolSource { get; set; }

    [Ignore]
    public List<QuestionAndAnswer> QuestionsAndAnswers { get; set; } = new();

    [Ignore]
    public string ReportText
    {
        get
        {
            return string.Join(Environment.NewLine, QuestionsAndAnswers.Select(qa => qa.Text));
        }
    }
}