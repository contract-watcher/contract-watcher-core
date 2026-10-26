using System.Buffers.Text;
using System.Security.Cryptography;
using ContractWatcher.Core.Data;
using ContractWatcher.Core.Data.Domain;
using ContractWatcher.Core.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ContractWatcher.Core.Services.Auth;

/// <summary>
/// Результат запроса письма подтверждения
/// </summary>
public enum EmailConfirmationRequestStatus
{
    Sent,
    AlreadyConfirmed,

    /// <summary>
    /// Прошлое письмо отправлено меньше минуты назад
    /// </summary>
    TooSoon,
}

/// <summary>
/// Подтверждение email: без него письма о неуспешных проверках не включаются,
/// иначе уведомления уходили бы на адрес, который указали при регистрации, не владея им
/// </summary>
public class EmailConfirmationService(DataContext db, TimeProvider timeProvider, IOptions<NotificationOptions> options)
{
    private const int TokenSizeBytes = 32;

    // Повторное письмо не чаще раза в минуту: через API нельзя засыпать письмами чужой ящик
    private static readonly TimeSpan ResendInterval = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Ставит в очередь письмо со ссылкой подтверждения. Новое письмо отменяет ссылку из прошлого
    /// </summary>
    public async Task<EmailConfirmationRequestStatus> RequestAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleAsync(x => x.Id == userId, cancellationToken);
        if (user.EmailConfirmedAt is not null)
            return EmailConfirmationRequestStatus.AlreadyConfirmed;

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (user.EmailConfirmationSentAt > now - ResendInterval)
            return EmailConfirmationRequestStatus.TooSoon;

        var lifetimeHours = options.Value.EmailConfirmationLifetimeHours;
        var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenSizeBytes));

        user.EmailConfirmationTokenHash = TokenService.HashToken(token);
        user.EmailConfirmationSentAt = now;
        user.EmailConfirmationExpiresAt = now.AddHours(lifetimeHours);

        var link = FrontendLinks.ConfirmEmail(options.Value.FrontendBaseUrl, token);
        db.EmailMessages.Add(
            new EmailMessage
            {
                To = user.Email,
                Subject = "Подтвердите email для уведомлений",
                Body = $"""
                    Здравствуйте, {user.Name}!

                    Чтобы получать письма о неуспешных проверках контрактов, подтвердите этот адрес по ссылке:
                    {link}

                    Ссылка действует {lifetimeHours} ч. Если вы не запрашивали подтверждение, просто проигнорируйте письмо.
                    """,
                NextAttemptAt = now,
            }
        );

        await db.SaveChangesAsync(cancellationToken);

        return EmailConfirmationRequestStatus.Sent;
    }

    /// <summary>
    /// Подтверждает email по токену из письма. False — токен неизвестен, уже использован или истёк
    /// </summary>
    public async Task<bool> ConfirmAsync(string token, CancellationToken cancellationToken)
    {
        var tokenHash = TokenService.HashToken(token);
        var user = await db.Users.SingleOrDefaultAsync(x => x.EmailConfirmationTokenHash == tokenHash, cancellationToken);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (user is null || user.EmailConfirmationExpiresAt <= now)
            return false;

        user.EmailConfirmedAt = now;
        user.EmailConfirmationTokenHash = null;
        user.EmailConfirmationExpiresAt = null;

        await db.SaveChangesAsync(cancellationToken);

        return true;
    }
}
