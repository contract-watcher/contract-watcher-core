using ContractWatcher.Core.Data;
using ContractWatcher.Core.Data.Domain;
using ContractWatcher.Core.Extensions;
using ContractWatcher.Core.Models.Integrations;
using Microsoft.EntityFrameworkCore;

namespace ContractWatcher.Core.Services.Integrations;

/// <summary>
/// Результат создания или изменения интеграции
/// </summary>
public enum IntegrationSaveStatus
{
    Saved,
    NotFound,
    SlugTaken,
}

/// <summary>
/// Интеграции проекта. Доступны только владельцу проекта
/// </summary>
public class IntegrationService(DataContext db)
{
    /// <summary>
    /// Интеграции проекта. Null — проекта нет или он чужой
    /// </summary>
    public async Task<List<IntegrationResponse>?> GetListAsync(
        Guid userId,
        Guid projectId,
        CancellationToken cancellationToken
    )
    {
        if (!await ProjectExistsAsync(userId, projectId, cancellationToken))
            return null;

        var integrations = await db
            .Integrations.AsNoTracking()
            .Where(x => x.ProjectId == projectId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return integrations.ConvertAll(IntegrationResponse.From);
    }

    /// <summary>
    /// Интеграция проекта. Null — интеграции нет или проект чужой
    /// </summary>
    public async Task<IntegrationResponse?> GetAsync(
        Guid userId,
        Guid projectId,
        Guid integrationId,
        CancellationToken cancellationToken
    )
    {
        var integration = await OwnedBy(userId, projectId)
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == integrationId, cancellationToken);

        return integration is null ? null : IntegrationResponse.From(integration);
    }

    public async Task<(IntegrationSaveStatus Status, IntegrationResponse? Integration)> CreateAsync(
        Guid userId,
        Guid projectId,
        IntegrationRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!await ProjectExistsAsync(userId, projectId, cancellationToken))
            return (IntegrationSaveStatus.NotFound, null);

        if (await IsSlugTakenAsync(projectId, request.Slug, cancellationToken))
            return (IntegrationSaveStatus.SlugTaken, null);

        var integration = new Integration
        {
            ProjectId = projectId,
            Slug = request.Slug,
            Name = request.Name.Trim(),
            Description = NormalizeDescription(request.Description),
        };
        db.Integrations.Add(integration);

        return await SaveAsync(integration, cancellationToken);
    }

    public async Task<(IntegrationSaveStatus Status, IntegrationResponse? Integration)> UpdateAsync(
        Guid userId,
        Guid projectId,
        Guid integrationId,
        IntegrationRequest request,
        CancellationToken cancellationToken
    )
    {
        var integration = await OwnedBy(userId, projectId)
            .SingleOrDefaultAsync(x => x.Id == integrationId, cancellationToken);
        if (integration is null)
            return (IntegrationSaveStatus.NotFound, null);

        if (integration.Slug != request.Slug && await IsSlugTakenAsync(projectId, request.Slug, cancellationToken))
            return (IntegrationSaveStatus.SlugTaken, null);

        integration.Slug = request.Slug;
        integration.Name = request.Name.Trim();
        integration.Description = NormalizeDescription(request.Description);

        return await SaveAsync(integration, cancellationToken);
    }

    /// <summary>
    /// Удаляет интеграцию вместе с её ключами и контрактами. False — интеграции нет или проект чужой
    /// </summary>
    public async Task<bool> DeleteAsync(
        Guid userId,
        Guid projectId,
        Guid integrationId,
        CancellationToken cancellationToken
    )
    {
        var integration = await OwnedBy(userId, projectId)
            .SingleOrDefaultAsync(x => x.Id == integrationId, cancellationToken);
        if (integration is null)
            return false;

        db.Integrations.Remove(integration);
        await db.SaveChangesAsync(cancellationToken);

        return true;
    }

    private Task<bool> ProjectExistsAsync(Guid userId, Guid projectId, CancellationToken cancellationToken) =>
        db.Projects.AnyAsync(x => x.Id == projectId && x.OwnerId == userId, cancellationToken);

    private IQueryable<Integration> OwnedBy(Guid userId, Guid projectId) =>
        db.Integrations.Where(x => x.ProjectId == projectId && x.Project.OwnerId == userId);

    private Task<bool> IsSlugTakenAsync(Guid projectId, string slug, CancellationToken cancellationToken) =>
        db.Integrations.AnyAsync(x => x.ProjectId == projectId && x.Slug == slug, cancellationToken);

    private async Task<(IntegrationSaveStatus Status, IntegrationResponse? Integration)> SaveAsync(
        Integration integration,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException e) when (e.IsUniqueViolation())
        {
            // Тот же slug успели занять параллельным запросом
            return (IntegrationSaveStatus.SlugTaken, null);
        }

        return (IntegrationSaveStatus.Saved, IntegrationResponse.From(integration));
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
