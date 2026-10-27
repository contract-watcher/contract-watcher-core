using Microsoft.Extensions.Options;

namespace ContractWatcher.Core.Services.Monitoring;

/// <summary>
/// Предохранитель доставки в Monitoring. После BreakerThreshold неудач подряд, каждая не позже BreakerWindowSeconds
/// после предыдущей, размыкается: в течение паузы запросов нет.
/// После паузы пропускается один пробный отчёт: успех замыкает предохранитель, неудача размыкает снова на вдвое
/// большую паузу, но не дольше MaxBreakerDelaySeconds. Один на приложение: состояние живёт между проходами worker'а
/// </summary>
public class MonitoringCircuitBreaker(IOptions<MonitoringOptions> options, ILogger<MonitoringCircuitBreaker> logger)
{
    private enum State
    {
        Closed,
        Open,
        HalfOpen,
    }

    private readonly Lock _lock = new();
    private State _state = State.Closed;
    private int _failures;
    private DateTime _lastFailureAt;
    private TimeSpan _delay;
    private DateTime _openUntil;

    /// <summary>
    /// Замкнут: Monitoring принимает отчёты. Только в этом состоянии неудача засчитывается самому отчёту
    /// </summary>
    public bool IsClosed
    {
        get
        {
            lock (_lock)
                return _state == State.Closed;
        }
    }

    /// <summary>
    /// Можно ли сейчас обращаться к Monitoring. Probe — после паузы разрешён только один пробный отчёт
    /// </summary>
    public bool TryEnter(DateTime now, out bool probe)
    {
        lock (_lock)
        {
            if (_state == State.Open && now >= _openUntil)
                _state = State.HalfOpen;

            probe = _state == State.HalfOpen;
            return _state != State.Open;
        }
    }

    /// <summary>
    /// Monitoring ответил по существу: принял или отклонил отчёт
    /// </summary>
    public void OnSuccess()
    {
        lock (_lock)
        {
            if (_state != State.Closed)
                logger.LogInformation("Monitoring is available again, report delivery resumed");

            _state = State.Closed;
            _failures = 0;
        }
    }

    /// <summary>
    /// Monitoring недоступен или не смог обработать отчёт
    /// </summary>
    public void OnFailure(DateTime now, string? error)
    {
        lock (_lock)
        {
            // «Подряд» — без долгих перерывов. Иначе редкие попытки отложенного отчёта раз в сутки
            // на тихой интеграции, где между ними нет других отчётов, разомкнули бы предохранитель
            if (now - _lastFailureAt > TimeSpan.FromSeconds(options.Value.BreakerWindowSeconds))
                _failures = 0;

            _failures++;
            _lastFailureAt = now;

            if (_state == State.HalfOpen)
            {
                // Пробный отчёт не прошёл: Monitoring всё ещё не работает, пауза растёт
                _delay = TimeSpan.FromSeconds(
                    Math.Min(_delay.TotalSeconds * 2, options.Value.MaxBreakerDelaySeconds)
                );
                Open(now);
                return;
            }

            if (_state == State.Closed && _failures >= options.Value.BreakerThreshold)
            {
                _delay = TimeSpan.FromSeconds(options.Value.BreakerDelaySeconds);
                Open(now);
                // Одна запись на весь сбой: пробные отчёты во время паузы в лог не пишутся
                logger.LogWarning(
                    "Monitoring failed {Failures} times in a row, report delivery is paused: {Error}",
                    _failures,
                    error
                );
            }
        }
    }

    private void Open(DateTime now)
    {
        _state = State.Open;
        _openUntil = now + _delay;
    }
}
