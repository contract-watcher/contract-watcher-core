namespace ContractWatcher.Core.Models.Reports;

/// <summary>
/// Страница списка
/// </summary>
/// <param name="Total">Сколько всего элементов с учётом фильтров</param>
/// <param name="Offset">Применённое смещение</param>
/// <param name="Limit">Применённый размер страницы</param>
public record PageResponse<T>(List<T> Items, long Total, int Offset, int Limit);
