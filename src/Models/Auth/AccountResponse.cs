namespace ContractWatcher.Core.Models.Auth;

/// <summary>
/// Текущий пользователь
/// </summary>
/// <param name="Email">Email — логин, на него же уходят письма о неуспешных проверках</param>
/// <param name="EmailConfirmed">Подтверждён ли email. Без подтверждения письма не включаются ни в одной интеграции</param>
public record AccountResponse(string Email, bool EmailConfirmed);
