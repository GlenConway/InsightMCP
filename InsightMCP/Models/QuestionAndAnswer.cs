using CsvHelper.Configuration.Attributes;

namespace InsightMCP.Models;

public class QuestionAndAnswer
{
    [Name("Case Number")]
    public required string CaseNumber { get; set; }

    [Name("Question")]
    public required string Question { get; set; }

    [Name("Answer")]
    public required string Answer { get; set; }

    [Ignore]
    public string Text
    {
        get
        {
            return $"{Question}: {Answer}";
        }
    }

}
