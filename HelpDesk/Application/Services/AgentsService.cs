using HelpDesk.Application.Agents;
using System.Net.Http.Json;

namespace HelpDesk.Application.Services;

public interface IAgentsService
{
    Task<OrchestrationResult?> KickoffAsync(string objective, string? context = null, CancellationToken cancellationToken = default);
}

public sealed class AgentsService : IAgentsService
{
    private readonly HttpClient _httpClient;

    public AgentsService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<OrchestrationResult?> KickoffAsync(
        string objective,
        string? context = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(objective))
        {
            throw new ArgumentException("Objective cannot be empty.", nameof(objective));
        }

        try
        {
            var request = new AgentRequest(objective, context);
            var response = await _httpClient.PostAsJsonAsync(
                "/api/agents/kickoff",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"API request failed with status {response.StatusCode}: {response.ReasonPhrase}");
            }

            var result = await response.Content.ReadFromJsonAsync<OrchestrationResult>(cancellationToken: cancellationToken);
            return result;
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed to kickoff agents: {ex.Message}", ex);
        }
    }
}
