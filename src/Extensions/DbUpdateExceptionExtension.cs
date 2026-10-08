using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ContractWatcher.Core.Extensions;

public static class DbUpdateExceptionExtension
{
    /// <summary>
    /// Нарушен уникальный индекс — например, параллельный запрос успел занять тот же slug
    /// </summary>
    public static bool IsUniqueViolation(this DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
