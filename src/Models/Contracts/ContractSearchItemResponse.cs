namespace ContractWatcher.Core.Models.Contracts;

/// <summary>
/// Контракт в списке выбора: ID передаётся в фильтр истории как contractId
/// </summary>
public record ContractSearchItemResponse(Guid Id, string Slug, string Name);
