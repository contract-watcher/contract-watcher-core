using ContractWatcher.Core.Data;
using ContractWatcher.Core.Data.Domain;
using ContractWatcher.Core.Models.Projects;
using Microsoft.EntityFrameworkCore;

namespace ContractWatcher.Core.Services.Projects;

/// <summary>
/// Проекты пользователя. Пользователь видит только проекты, владельцем которых является
/// </summary>
public class ProjectService(DataContext db)
{
    public async Task<List<ProjectResponse>> GetListAsync(Guid userId, CancellationToken cancellationToken)
    {
        var projects = await db
            .Projects.AsNoTracking()
            .Where(x => x.OwnerId == userId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return projects.ConvertAll(ProjectResponse.From);
    }

    /// <summary>
    /// Проект пользователя. Null — проекта нет или он чужой
    /// </summary>
    public async Task<ProjectResponse?> GetAsync(Guid userId, Guid projectId, CancellationToken cancellationToken)
    {
        var project = await db
            .Projects.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == projectId && x.OwnerId == userId, cancellationToken);

        return project is null ? null : ProjectResponse.From(project);
    }

    public async Task<ProjectResponse> CreateAsync(Guid userId, string name, CancellationToken cancellationToken)
    {
        var project = new Project { OwnerId = userId, Name = name.Trim() };
        db.Projects.Add(project);
        await db.SaveChangesAsync(cancellationToken);

        return ProjectResponse.From(project);
    }
}
