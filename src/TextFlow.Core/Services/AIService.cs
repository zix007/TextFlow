using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TextFlow.Services.Interfaces;

namespace TextFlow.Services;

public class AIService : IAIService
{
    private readonly ISettingsService _settingsService;
    private readonly HttpClient _httpClient;

    public AIService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        _httpClient = new HttpClient();
    }

    public async Task<string> SummarizeTextAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var apiKey = _settingsService.Settings.AiApiKey;
        if (string.IsNullOrEmpty(apiKey))
        {
            // Provide intelligent fallback summary if no API key is provided
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int words = text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
            return $"Summary (Offline Analysis):\n- Total lines: {lines.Length}\n- Total words: {words}\n- Preview: {(text.Length > 150 ? text.Substring(0, 150) + "..." : text)}\n\n(Configure your AI API Key in Settings > AI to enable full LLM summarization.)";
        }

        return await QueryAiModelAsync($"Summarize the following text concisely:\n\n{text}");
    }

    public async Task<string> RewriteContentAsync(string text, string toneOrStyle)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var apiKey = _settingsService.Settings.AiApiKey;
        if (string.IsNullOrEmpty(apiKey))
        {
            return $"[Rewritten ({toneOrStyle})]:\n{text}\n\n(Configure AI API Key in Settings for generative rewriting.)";
        }

        return await QueryAiModelAsync($"Rewrite the following text with a {toneOrStyle} tone:\n\n{text}");
    }

    public async Task<string> ExplainSqlAsync(string sqlQuery)
    {
        if (string.IsNullOrWhiteSpace(sqlQuery)) return string.Empty;
        var apiKey = _settingsService.Settings.AiApiKey;
        if (string.IsNullOrEmpty(apiKey))
        {
            return $"SQL Query Analysis:\nStatements detected in query. Configure AI API Key in Settings for deep query optimization and explanation.";
        }

        return await QueryAiModelAsync($"Explain this SQL query step-by-step and provide suggestions for indexing or performance:\n\n{sqlQuery}");
    }

    public async Task<string> SmartSearchAndReplaceAsync(string text, string instruction)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var apiKey = _settingsService.Settings.AiApiKey;
        if (string.IsNullOrEmpty(apiKey))
        {
            return text;
        }

        return await QueryAiModelAsync($"Apply the following transformation instruction to the text:\nInstruction: {instruction}\n\nText:\n{text}");
    }

    public async Task<string> SuggestNextSentenceAsync(string contextText)
    {
        if (string.IsNullOrWhiteSpace(contextText)) return string.Empty;
        var apiKey = _settingsService.Settings.AiApiKey;
        if (string.IsNullOrEmpty(apiKey))
        {
            return string.Empty;
        }

        return await QueryAiModelAsync($"Suggest the next completion sentence for:\n\n{contextText}");
    }

    private async Task<string> QueryAiModelAsync(string prompt)
    {
        try
        {
            // Example OpenAI-compatible completion request
            var requestBody = new
            {
                model = _settingsService.Settings.AiModel,
                messages = new[]
                {
                    new { role = "system", content = "You are TextFlow AI, a helpful coding and writing assistant." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.5
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", $"Bearer {_settingsService.Settings.AiApiKey}");

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
            }
            return $"AI request failed with status: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"AI Error: {ex.Message}";
        }
    }
}
