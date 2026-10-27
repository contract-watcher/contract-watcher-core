using System.Net.Sockets;
using ContractWatcher.Core.Data.Domain;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace ContractWatcher.Core.Services.Notifications;

/// <summary>
/// Результат отправки письма
/// </summary>
public enum EmailSendStatus
{
    /// <summary>
    /// Почтовый сервер принял письмо
    /// </summary>
    Sent,

    /// <summary>
    /// Сервер отклонил адрес или содержимое (5xx): повторная отправка не поможет
    /// </summary>
    Rejected,

    /// <summary>
    /// Сервер временно не принял именно это письмо (4xx): отправить позже
    /// </summary>
    Failed,

    /// <summary>
    /// Сервер недоступен или не пустил по логину: письмо не виновато, остальные тоже не уйдут
    /// </summary>
    Unavailable,
}

/// <summary>
/// Отправка писем через SMTP. Одно соединение на scope: проход worker'а отправляет пачку через одно подключение
/// </summary>
public sealed class EmailSender(IOptions<SmtpOptions> options) : IAsyncDisposable
{
    private readonly SmtpClient client = new();

    public bool IsConfigured => options.Value.IsConfigured;

    /// <summary>
    /// Отправляет письмо. Error — описание ошибки для всех статусов, кроме Sent
    /// </summary>
    public async Task<(EmailSendStatus Status, string? Error)> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken
    )
    {
        var smtp = options.Value;

        if (!MailboxAddress.TryParse(message.To, out var recipient))
            return (EmailSendStatus.Rejected, $"Invalid recipient address: {message.To}");

        try
        {
            if (!client.IsConnected)
            {
                client.Timeout = (int)TimeSpan.FromSeconds(smtp.TimeoutSeconds).TotalMilliseconds;
                await client.ConnectAsync(smtp.Host, smtp.Port, smtp.Security, cancellationToken);

                if (!string.IsNullOrEmpty(smtp.Username))
                    await client.AuthenticateAsync(smtp.Username, smtp.Password, cancellationToken);
            }

            await client.SendAsync(ToMime(message, recipient, smtp), cancellationToken);
            return (EmailSendStatus.Sent, null);
        }
        catch (SmtpCommandException e)
            when (e.ErrorCode is SmtpErrorCode.RecipientNotAccepted or SmtpErrorCode.MessageNotAccepted)
        {
            // Отказ именно этому письму: 5xx — постоянный, 4xx — временный, например переполнен ящик
            var error = $"SMTP server refused the message ({(int)e.StatusCode}): {e.Message}";
            return (int)e.StatusCode >= 500 ? (EmailSendStatus.Rejected, error) : (EmailSendStatus.Failed, error);
        }
        catch (Exception e) when (IsServerError(e) && !cancellationToken.IsCancellationRequested)
        {
            // Отказ отправителю, логину или сбой соединения — проблема сервера или настроек, а не письма.
            // Соединение после такого может быть испорчено, следующее письмо подключится заново
            await DisconnectAsync();
            return (EmailSendStatus.Unavailable, $"SMTP server is unavailable: {e.GetType().Name}: {e.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        client.Dispose();
    }

    private async Task DisconnectAsync()
    {
        if (!client.IsConnected)
            return;

        try
        {
            await client.DisconnectAsync(true);
        }
        catch (Exception e) when (IsServerError(e))
        {
            // Сервер уже закрыл соединение — закрывать нечего
        }
    }

    private static MimeMessage ToMime(EmailMessage message, MailboxAddress recipient, SmtpOptions smtp)
    {
        var from = new MailboxAddress(smtp.FromName, smtp.FromAddress);
        var mime = new MimeMessage
        {
            Subject = message.Subject,
            // Повтор того же письма уходит с тем же Message-ID: часть почтовых служб по нему убирает дубли
            MessageId = $"{message.Id}@{from.Domain}",
            Body = new TextPart(TextFormat.Plain) { Text = message.Body },
        };
        mime.From.Add(from);
        mime.To.Add(recipient);

        return mime;
    }

    private static bool IsServerError(Exception e) =>
        e
            is SmtpCommandException
                or SmtpProtocolException
                or AuthenticationException
                or SslHandshakeException
                or ServiceNotConnectedException
                or IOException
                or SocketException
                or TimeoutException;
}
