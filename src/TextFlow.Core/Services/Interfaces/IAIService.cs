using System.Threading.Tasks;

namespace TextFlow.Services.Interfaces;

public interface IAIService
{
    Task<string> SummarizeTextAsync(string text);
    Task<string> RewriteContentAsync(string text, string toneOrStyle);
    Task<string> ExplainSqlAsync(string sqlQuery);
    Task<string> SmartSearchAndReplaceAsync(string text, string instruction);
    Task<string> SuggestNextSentenceAsync(string contextText);
}
