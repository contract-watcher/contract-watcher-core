using System.Net;
using System.Text;
using Moq;
using Moq.Protected;

namespace ContractWatcher.Core.Tests.Fixtures;

public static class HttpFixture
{
    public const string MonitoringUrl = "http://monitoring:8080/";

    public static HttpClient CreateHttpClient(HttpResponseMessage response) =>
        CreateHttpClient(_ => Task.FromResult(response));

    /// <summary>
    /// Клиент Monitoring, ответ которого строит respond: можно сохранить запрос и прочитать его тело
    /// </summary>
    public static HttpClient CreateHttpClient(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond)
    {
        var handler = new Mock<HttpMessageHandler>();

        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Returns((HttpRequestMessage request, CancellationToken _) => respond(request));

        return new HttpClient(handler.Object) { BaseAddress = new Uri(MonitoringUrl) };
    }

    /// <summary>
    /// Клиент Monitoring, запросы которого завершаются исключением: сеть недоступна или вышел таймаут
    /// </summary>
    public static HttpClient CreateFailingHttpClient(Exception exception)
    {
        var handler = new Mock<HttpMessageHandler>();

        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(exception);

        return new HttpClient(handler.Object) { BaseAddress = new Uri(MonitoringUrl) };
    }

    public static HttpResponseMessage CreateJsonResponse(
        HttpStatusCode statusCode,
        string json,
        string mediaType = "application/json"
    ) => new(statusCode) { Content = new StringContent(json, Encoding.UTF8, mediaType) };

    public static HttpResponseMessage CreateProblemResponse(HttpStatusCode statusCode, string json) =>
        CreateJsonResponse(statusCode, json, "application/problem+json");
}
