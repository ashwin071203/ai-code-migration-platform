using System.Net.Http.Json;
using MigrationPlatform.AI.Interfaces;
using MigrationPlatform.AI.Models;
using Microsoft.Extensions.Configuration;

namespace MigrationPlatform.AI.Implementations;

public sealed class OllamaAIProvider(HttpClient httpClient, IConfiguration configuration) : IAIProvider
{
    public string Name => "Ollama";

    public async Task<AIResponse> CompleteAsync(AIRequest request, CancellationToken cancellationToken)
    {
        var model = request.Model ?? configuration["AI:Model"] ?? "llama3.2";
        var endpoint = configuration["AI:Endpoint"] ?? "http://localhost:11434";
        using var response = await httpClient.PostAsJsonAsync($"{endpoint.TrimEnd('/')}/api/chat", new { model, stream = false, messages = new[] { new { role = "system", content = request.SystemPrompt }, new { role = "user", content = request.UserPrompt } } }, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Ollama returned {(int)response.StatusCode} ({response.ReasonPhrase}). {detail}");
        }
        var payload = await response.Content.ReadFromJsonAsync<OllamaResponse>(cancellationToken);
        return new(payload?.Message?.Content ?? string.Empty, Name, model, payload?.PromptEvalCount, payload?.EvalCount);
    }

    private sealed record OllamaResponse(OllamaMessage? Message, int? PromptEvalCount, int? EvalCount);
    private sealed record OllamaMessage(string Content);
}