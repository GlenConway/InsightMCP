using System.Collections.Generic;
using System.Linq;

namespace InsightMCP.Models;

public static class QuestionAndAnswerExtensions
{
    public static IEnumerable<IGrouping<string, QuestionAndAnswer>> GroupByCase(
        this IEnumerable<QuestionAndAnswer> questionsAndAnswers)
    {
        return questionsAndAnswers.GroupBy(qa => qa.CaseNumber);
    }
}