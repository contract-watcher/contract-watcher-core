using ContractWatcher.Core.Data.Domain;

namespace ContractWatcher.Core.Models.Projects;

/// <summary>
/// Проект в ответе API
/// </summary>
public record ProjectResponse(Guid Id, string Name, DateTime CreatedAt, DateTime UpdatedAt)
{
    public static ProjectResponse From(Project project) =>
        new(project.Id, project.Name, project.CreatedAt, project.UpdatedAt);
}
