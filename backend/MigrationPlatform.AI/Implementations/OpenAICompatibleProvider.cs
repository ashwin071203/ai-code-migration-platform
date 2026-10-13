using System.Net.Http.Headers;
using System.Net.Http.Json;
using MigrationPlatform.AI.Interfaces;
using MigrationPlatform.AI.Models;
using Microsoft.Extensions.Configuration;

namespace MigrationPlatform.AI.Implementations;

public sealed class OpenAICompatibleProvider(HttpClient httpClient, IConfiguration configuration) : IAIProvider
{
    public string Name => "OpenAI-compatible";

    public async Task<AIResponse> CompleteAsync(AIRequest request, CancellationToken cancellationToken)
    {
        var endpoint = configuration["AI:Endpoint"] ?? throw new InvalidOperationException("AI:Endpoint is not configured.");
        var model = request.Model ?? configuration["AI:Model"] ?? throw new InvalidOperationException("AI:Model is not configured.");
        var key = configuration["AI:ApiKey"];
        using var message = new HttpRequestMessage(HttpMethod.Post, $"{endpoint.TrimEnd('/')}/chat/completions") { Content = JsonContent.Create(new { model, messages = new[] { new { role = "system", content = request.SystemPrompt }, new { role = "user", content = request.UserPrompt } } }) };
        if (!string.IsNullOrWhiteSpace(key)) message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        using var response = await httpClient.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"The configured AI provider returned {(int)response.StatusCode} ({response.ReasonPhrase}). {detail}");
        }
        var payload = await response.Content.ReadFromJsonAsync<OpenAIResponse>(cancellationToken);
        var usage = payload?.Usage;
        return new(payload?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty, Name, model, usage?.PromptTokens, usage?.CompletionTokens);
    }

    private sealed record OpenAIResponse(IReadOnlyList<Choice>? Choices, Usage? Usage);
    private sealed record Choice(Message? Message);
    private sealed record Message(string Content);
    private sealed record Usage(int PromptTokens, int CompletionTokens);
}