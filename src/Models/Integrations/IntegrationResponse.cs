using ContractWatcher.Core.Data.Domain;

namespace ContractWatcher.Core.Models.Integrations;

/// <summary>
/// Интеграция в ответе API
/// </summary>
public record IntegrationResponse(
    Guid Id,
    Guid ProjectId,
    string Slug,
    string Name,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt
)
{
    public static IntegrationResponse From(Integration integration) =>
        new(
            integration.Id,
            integration.ProjectId,
            integration.Slug,
            integration.Name,
            integration.Description,
            integration.CreatedAt,
            integration.UpdatedAt
        );
}
