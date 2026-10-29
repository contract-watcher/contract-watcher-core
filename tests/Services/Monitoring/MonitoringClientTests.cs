using System.Net;
using System.Text.Json;
using ContractWatcher.Common.Enums;
using ContractWatcher.Core.Models.Monitoring;
using ContractWatcher.Core.Models.Reports;
using ContractWatcher.Core.Services.Monitoring;
using Microsoft.AspNetCore.WebUtilities;

// Fixtures
using static ContractWatcher.Core.Tests.Fixtures.HttpFixture;
using static ContractWatcher.Core.Tests.Fixtures.MonitoringFixture;
using static ContractWatcher.Core.Tests.Fixtures.ReportFixture;

namespace ContractWatcher.Core.Tests.Services.Monitoring;

public sealed class MonitoringClientTests
{
    private static readonly Guid Integration = Guid.Parse(IntegrationId);

    // Время, которое вернул Monitoring: 2026-10-19T09:00:00.123456Z
    private static readonly DateTime ReceivedAt = new DateTime(2026, 10, 19, 9, 0, 0, DateTimeKind.Utc).AddTicks(1_234_560);

    #region SendReportAsync

    [Theory]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.OK)]
    public async Task SendReportAsync_WhenMonitoringStoresReport_ReturnsDelivered(HttpStatusCode statusCode)
    {
        // Arrange
        using var httpClient = CreateHttpClient(new HttpResponseMessage(statusCode));
        var client = new MonitoringClient(httpClient);

        // Act
        var (status, error) = await client.SendReportAsync(CreateMonitoringReportRequest(), CancellationToken.None);

        // Assert
        Assert.Equal(MonitoringDeliveryStatus.Delivered, status);
        Assert.Null(error);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, MonitoringDeliveryStatus.Rejected)]
    [InlineData(HttpStatusCode.InternalServerError, MonitoringDeliveryStatus.Failed)]
    [InlineData(HttpStatusCode.NotFound, MonitoringDeliveryStatus.Unavailable)]
    [InlineData(HttpStatusCode.UnsupportedMediaType, MonitoringDeliveryStatus.Unavailable)]
    [InlineData(HttpStatusCode.BadGateway, MonitoringDeliveryStatus.Unavailable)]
    [InlineData(HttpStatusCode.ServiceUnavailable, MonitoringDeliveryStatus.Unavailable)]
    public async Task SendReportAsync_WhenMonitoringReturnsError_ReturnsExpectedStatus(
        HttpStatusCode statusCode,
        MonitoringDeliveryStatus expectedStatus
    )
    {
        // Arrange
        using var httpClient = CreateHttpClient(CreateProblemResponse(statusCode, InvalidReportJson));
        var client = new MonitoringClient(httpClient);

        // Act
        var (status, error) = await client.SendReportAsync(CreateMonitoringReportRequest(), CancellationToken.None);

        // Assert
        Assert.Equal(expectedStatus, status);
        Assert.NotNull(error);
        Assert.Contains($"HTTP {(int)statusCode}", error);
        Assert.Contains("VALIDATION_REPORT_INVALID", error);
    }

    [Fact]
    public async Task SendReportAsync_WhenMonitoringIsUnreachable_ReturnsUnavailable()
    {
        // Arrange
        using var httpClient = CreateFailingHttpClient(new HttpRequestException("Connection refused"));
        var client = new MonitoringClient(httpClient);

        // Act
        var (status, error) = await client.SendReportAsync(CreateMonitoringReportRequest(), CancellationToken.None);

        // Assert
        Assert.Equal(MonitoringDeliveryStatus.Unavailable, status);
        Assert.Contains("Connection refused", error);
    }

    [Fact]
    public async Task SendReportAsync_WhenRequestTimesOut_ReturnsUnavailable()
    {
        // Arrange: таймаут HttpClient приходит как TaskCanceledException без отмены вызывающим
        using var httpClient = CreateFailingHttpClient(new TaskCanceledException());
        var client = new MonitoringClient(httpClient);

        // Act
        var (status, error) = await client.SendReportAsync(CreateMonitoringReportRequest(), CancellationToken.None);

        // Assert
        Assert.Equal(MonitoringDeliveryStatus.Unavailable, status);
        Assert.Equal("Monitoring request timed out", error);
    }

    [Fact]
    public async Task SendReportAsync_WhenCallerCancels_Throws()
    {
        // Arrange
        using var httpClient = CreateHttpClient(new HttpResponseMessage(HttpStatusCode.Created));
        var client = new MonitoringClient(httpClient);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        var action = () => client.SendReportAsync(CreateMonitoringReportRequest(), cancellation.Token);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(action);
    }

    [Fact]
    public async Task SendReportAsync_SendsReportInMonitoringFormat()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        string? body = null;

        using var httpClient = CreateHttpClient(async request =>
        {
            capturedRequest = request;
            body = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.Created);
        });
        var client = new MonitoringClient(httpClient);
        var request = CreateMonitoringReportRequest();

        // Act
        await client.SendReportAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("/internal/v1/reports", capturedRequest.RequestUri!.AbsolutePath);

        using var json = JsonDocument.Parse(body!);
        var root = json.RootElement;
        Assert.Equal(request.IntegrationId, root.GetProperty("integrationId").GetGuid());
        Assert.Equal(request.ContractId, root.GetProperty("contractId").GetGuid());

        var report = root.GetProperty("report");
        Assert.Equal(request.Report.ReportId, report.GetProperty("reportId").GetGuid());
        Assert.Equal(1, report.GetProperty("contractVersion").GetInt32());
        Assert.Equal("Failed", report.GetProperty("status").GetString());
        Assert.Equal("2026-10-26T19:00:00.123456Z", report.GetProperty("timestamp").GetString());

        var violation = Assert.Single(report.GetProperty("violations").EnumerateArray());
        Assert.Equal("amount", violation.GetProperty("fieldName").GetString());
        Assert.Equal("TypeMismatch", violation.GetProperty("type").GetString());
        Assert.Equal("Number", violation.GetProperty("expectedType").GetString());
        Assert.Equal("String", violation.GetProperty("actualType").GetString());
    }

    #endregion

    #region GetReportHistoryAsync

    [Fact]
    public async Task GetReportHistoryAsync_WhenMonitoringReturnsPage_ReturnsFoundWithoutContractNames()
    {
        // Arrange
        using var httpClient = CreateHttpClient(CreateJsonResponse(HttpStatusCode.OK, HistoryJson));
        var client = new MonitoringClient(httpClient);

        // Act
        var read = await client.GetReportHistoryAsync(Integration, new ReportHistoryQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(MonitoringReadStatus.Found, read.Status);
        Assert.Null(read.Error);
        Assert.Equal(1, read.Value!.Total);

        var item = Assert.Single(read.Value.Items);
        Assert.Equal(Guid.Parse(ReportId), item.ReportId);
        Assert.Equal(Guid.Parse(ContractId), item.ContractId);
        Assert.Equal(3, item.ContractVersion);
        Assert.Equal(ValidationStatus.Failed, item.Status);
        Assert.Equal(ReceivedAt, item.ReceivedAt);
        Assert.Equal(DateTimeKind.Utc, item.ReceivedAt.Kind);
        Assert.Equal(2, item.ViolationCount);

        // Названия контрактов Monitoring не знает, их подставляет Core
        Assert.Null(item.ContractSlug);
        Assert.Null(item.ContractName);
    }

    [Fact]
    public async Task GetReportHistoryAsync_WhenFiltersAreNotSet_SendsOnlyPaging()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        using var httpClient = CreateHttpClient(request =>
        {
            capturedRequest = request;
            return Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, HistoryJson));
        });
        var client = new MonitoringClient(httpClient);

        // Act
        await client.GetReportHistoryAsync(Integration, new ReportHistoryQuery(), CancellationToken.None);

        // Assert
        Assert.Equal($"/internal/v1/integrations/{IntegrationId}/reports", capturedRequest!.RequestUri!.AbsolutePath);

        var query = QueryHelpers.ParseQuery(capturedRequest.RequestUri.Query);
        Assert.Equal(["limit", "offset"], query.Keys.Order());
        Assert.Equal("0", query["offset"]);
        Assert.Equal("20", query["limit"]);
    }

    [Fact]
    public async Task GetReportHistoryAsync_WhenFiltersAreSet_SendsThemInMonitoringFormat()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        using var httpClient = CreateHttpClient(request =>
        {
            capturedRequest = request;
            return Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, HistoryJson));
        });
        var client = new MonitoringClient(httpClient);

        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var historyQuery = new ReportHistoryQuery
        {
            ContractId = [first, second, first],
            From = new DateTimeOffset(2026, 10, 21, 3, 0, 0, TimeSpan.FromHours(3)),
            To = new DateTimeOffset(2026, 10, 22, 0, 0, 0, TimeSpan.Zero).AddTicks(1_234_567),
            Status = ValidationStatus.Failed,
            Offset = 40,
            Limit = 10,
        };

        // Act
        await client.GetReportHistoryAsync(Integration, historyQuery, CancellationToken.None);

        // Assert
        var query = QueryHelpers.ParseQuery(capturedRequest!.RequestUri!.Query);

        // Повторяющийся параметр без повторов значений и без списка через запятую
        Assert.Collection(
            query["contractId"],
            id => Assert.Equal(first.ToString(), id),
            id => Assert.Equal(second.ToString(), id)
        );

        // Время в UTC с Z и шестью знаками дроби, лишние знаки отбрасываются
        Assert.Equal("2026-10-21T00:00:00.000000Z", query["from"]);
        Assert.Equal("2026-10-22T00:00:00.123456Z", query["to"]);

        Assert.Equal("Failed", query["status"]);
        Assert.Equal("40", query["offset"]);
        Assert.Equal("10", query["limit"]);
    }

    [Fact]
    public async Task GetReportHistoryAsync_WhenMonitoringRejectsParameters_ReturnsInvalidRequest()
    {
        // Arrange
        using var httpClient = CreateHttpClient(CreateProblemResponse(HttpStatusCode.BadRequest, InvalidPeriodJson));
        var client = new MonitoringClient(httpClient);

        // Act
        var read = await client.GetReportHistoryAsync(Integration, new ReportHistoryQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(MonitoringReadStatus.InvalidRequest, read.Status);
        Assert.Null(read.Value);
        Assert.Contains("HTTP 400", read.Error);

        var parameter = Assert.Single(read.InvalidParameters!);
        Assert.Equal("from", parameter.Key);
        Assert.Contains("UTC", Assert.Single(parameter.Value));
    }

    #endregion

    #region Ответы Monitoring на чтение

    [Fact]
    public async Task GetReportAsync_WhenReportExists_ReturnsDetails()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        using var httpClient = CreateHttpClient(request =>
        {
            capturedRequest = request;
            return Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, DetailsJson));
        });
        var client = new MonitoringClient(httpClient);

        // Act
        var read = await client.GetReportAsync(Integration, Guid.Parse(ReportId), CancellationToken.None);

        // Assert
        Assert.Equal(
            $"/internal/v1/integrations/{IntegrationId}/reports/{ReportId}",
            capturedRequest!.RequestUri!.AbsolutePath
        );
        Assert.Equal(MonitoringReadStatus.Found, read.Status);
        Assert.Equal(2, read.Value!.Violations.Count);

        var violation = read.Value.Violations[0];
        Assert.Equal(Guid.Parse(GroupId), violation.GroupId);
        Assert.Equal(ViolationType.TypeMismatch, violation.Type);
        Assert.Equal(ContractFieldType.Number, violation.ExpectedType);
        Assert.Equal(ContractFieldType.String, violation.ActualType);
    }

    [Fact]
    public async Task GetViolationGroupsAsync_SendsPagingAndReturnsGroups()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        using var httpClient = CreateHttpClient(request =>
        {
            capturedRequest = request;
            return Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, GroupsJson));
        });
        var client = new MonitoringClient(httpClient);

        // Act
        var read = await client.GetViolationGroupsAsync(
            Integration,
            new ViolationGroupsQuery { Offset = 5, Limit = 5 },
            CancellationToken.None
        );

        // Assert
        Assert.Equal($"/internal/v1/integrations/{IntegrationId}/violation-groups", capturedRequest!.RequestUri!.AbsolutePath);

        var query = QueryHelpers.ParseQuery(capturedRequest.RequestUri.Query);
        Assert.Equal("5", query["offset"]);
        Assert.Equal("5", query["limit"]);

        Assert.Equal(MonitoringReadStatus.Found, read.Status);
        var group = Assert.Single(read.Value!.Items);
        Assert.Equal(Guid.Parse(GroupId), group.GroupId);
        Assert.Equal(2, group.OccurrenceCount);
        Assert.Equal(ReceivedAt, group.FirstSeenAt);
    }

    [Fact]
    public async Task GetViolationGroupAsync_SendsRequestForGroup()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        using var httpClient = CreateHttpClient(request =>
        {
            capturedRequest = request;
            return Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, GroupJson));
        });
        var client = new MonitoringClient(httpClient);

        // Act
        var read = await client.GetViolationGroupAsync(Integration, Guid.Parse(GroupId), CancellationToken.None);

        // Assert
        Assert.Equal(
            $"/internal/v1/integrations/{IntegrationId}/violation-groups/{GroupId}",
            capturedRequest!.RequestUri!.AbsolutePath
        );
        Assert.Equal(MonitoringReadStatus.Found, read.Status);
        Assert.Equal("sample_field", read.Value!.FieldName);
    }

    [Fact]
    public async Task GetAsync_WhenMonitoringReturnsNotFound_ReturnsNotFound()
    {
        // Arrange
        using var httpClient = CreateHttpClient(new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = new MonitoringClient(httpClient);

        // Act
        var read = await client.GetReportAsync(Integration, Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.Equal(MonitoringReadStatus.NotFound, read.Status);
        Assert.Null(read.Value);
        Assert.Null(read.Error);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task GetAsync_WhenMonitoringFails_ReturnsUnavailableWithResponse(HttpStatusCode statusCode)
    {
        // Arrange
        using var httpClient = CreateHttpClient(CreateProblemResponse(statusCode, """{"code":"STORAGE_UNAVAILABLE"}"""));
        var client = new MonitoringClient(httpClient);

        // Act
        var read = await client.GetReportAsync(Integration, Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.Equal(MonitoringReadStatus.Unavailable, read.Status);
        Assert.Contains($"HTTP {(int)statusCode}", read.Error);
        Assert.Contains("STORAGE_UNAVAILABLE", read.Error);
        Assert.Null(read.InvalidParameters);
    }

    [Theory]
    [InlineData(InvalidReportJson)]
    [InlineData(InvalidRequestWithoutErrorsJson)]
    [InlineData("not json")]
    [InlineData("")]
    public async Task GetAsync_WhenBadRequestIsNotAboutParameters_ReturnsUnavailable(string body)
    {
        // Arrange
        using var httpClient = CreateHttpClient(CreateProblemResponse(HttpStatusCode.BadRequest, body));
        var client = new MonitoringClient(httpClient);

        // Act
        var read = await client.GetSummaryAsync(Integration, new IntegrationSummaryQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(MonitoringReadStatus.Unavailable, read.Status);
        Assert.Contains("HTTP 400", read.Error);
        Assert.Null(read.InvalidParameters);
    }

    [Fact]
    public async Task GetAsync_WhenResponseIsNotJson_ReturnsUnavailable()
    {
        // Arrange
        using var httpClient = CreateHttpClient(CreateJsonResponse(HttpStatusCode.OK, "{not json"));
        var client = new MonitoringClient(httpClient);

        // Act
        var read = await client.GetSummaryAsync(Integration, new IntegrationSummaryQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(MonitoringReadStatus.Unavailable, read.Status);
        Assert.StartsWith("Monitoring returned unexpected JSON", read.Error);
    }

    [Fact]
    public async Task GetAsync_WhenResponseIsJsonNull_ReturnsUnavailable()
    {
        // Arrange
        using var httpClient = CreateHttpClient(CreateJsonResponse(HttpStatusCode.OK, "null"));
        var client = new MonitoringClient(httpClient);

        // Act
        var read = await client.GetSummaryAsync(Integration, new IntegrationSummaryQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(MonitoringReadStatus.Unavailable, read.Status);
        Assert.Equal("Monitoring returned an empty body", read.Error);
    }

    [Fact]
    public async Task GetAsync_WhenMonitoringIsUnreachable_ReturnsUnavailable()
    {
        // Arrange
        using var httpClient = CreateFailingHttpClient(new HttpRequestException("Connection refused"));
        var client = new MonitoringClient(httpClient);

        // Act
        var read = await client.GetSummaryAsync(Integration, new IntegrationSummaryQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(MonitoringReadStatus.Unavailable, read.Status);
        Assert.Contains("Connection refused", read.Error);
    }

    [Fact]
    public async Task GetAsync_WhenRequestTimesOut_ReturnsUnavailable()
    {
        // Arrange
        using var httpClient = CreateFailingHttpClient(new TaskCanceledException());
        var client = new MonitoringClient(httpClient);

        // Act
        var read = await client.GetSummaryAsync(Integration, new IntegrationSummaryQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(MonitoringReadStatus.Unavailable, read.Status);
        Assert.Equal("Monitoring request timed out", read.Error);
    }

    [Fact]
    public async Task GetAsync_WhenCallerCancels_Throws()
    {
        // Arrange
        using var httpClient = CreateHttpClient(CreateJsonResponse(HttpStatusCode.OK, SummaryJson));
        var client = new MonitoringClient(httpClient);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        var action = () => client.GetSummaryAsync(Integration, new IntegrationSummaryQuery(), cancellation.Token);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(action);
    }

    #endregion

    #region GetSummaryAsync

    [Fact]
    public async Task GetSummaryAsync_WhenPeriodIsNotSet_SendsNoQuery()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        using var httpClient = CreateHttpClient(request =>
        {
            capturedRequest = request;
            return Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, SummaryJson));
        });
        var client = new MonitoringClient(httpClient);

        // Act
        var read = await client.GetSummaryAsync(Integration, new IntegrationSummaryQuery(), CancellationToken.None);

        // Assert
        Assert.Equal($"/internal/v1/integrations/{IntegrationId}/summary", capturedRequest!.RequestUri!.AbsolutePath);
        Assert.Empty(capturedRequest.RequestUri.Query);

        Assert.Equal(MonitoringReadStatus.Found, read.Status);
        Assert.Equal(4, read.Value!.TotalReports);
        Assert.Equal(2, read.Value.PassedReports);
        Assert.Equal(2, read.Value.FailedReports);
        Assert.Equal(new DateTime(2026, 10, 19, 9, 5, 0, DateTimeKind.Utc).AddTicks(1_234_560), read.Value.LastReceivedAt);
    }

    [Fact]
    public async Task GetSummaryAsync_WhenPeriodIsSet_SendsBoundsInUtc()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        using var httpClient = CreateHttpClient(request =>
        {
            capturedRequest = request;
            return Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, SummaryJson));
        });
        var client = new MonitoringClient(httpClient);

        var summaryQuery = new IntegrationSummaryQuery
        {
            From = new DateTimeOffset(2026, 10, 26, 0, 0, 0, TimeSpan.FromHours(3)),
            To = new DateTimeOffset(2026, 10, 28, 0, 0, 0, TimeSpan.FromHours(3)),
        };

        // Act
        await client.GetSummaryAsync(Integration, summaryQuery, CancellationToken.None);

        // Assert
        var query = QueryHelpers.ParseQuery(capturedRequest!.RequestUri!.Query);
        Assert.Equal(["from", "to"], query.Keys.Order());
        Assert.Equal("2026-10-25T21:00:00.000000Z", query["from"]);
        Assert.Equal("2026-10-27T21:00:00.000000Z", query["to"]);
    }

    [Fact]
    public async Task GetSummaryAsync_WhenOnlyFromIsSet_SendsOnlyFrom()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        using var httpClient = CreateHttpClient(request =>
        {
            capturedRequest = request;
            return Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, SummaryJson));
        });
        var client = new MonitoringClient(httpClient);
        var summaryQuery = new IntegrationSummaryQuery { From = new DateTimeOffset(2026, 10, 26, 0, 0, 0, TimeSpan.Zero) };

        // Act
        await client.GetSummaryAsync(Integration, summaryQuery, CancellationToken.None);

        // Assert
        var query = QueryHelpers.ParseQuery(capturedRequest!.RequestUri!.Query);
        Assert.Equal(["from"], query.Keys);
    }

    [Fact]
    public async Task GetSummaryAsync_WhenPeriodHasNoReports_ReturnsZerosAndNullTime()
    {
        // Arrange
        using var httpClient = CreateHttpClient(CreateJsonResponse(HttpStatusCode.OK, EmptySummaryJson));
        var client = new MonitoringClient(httpClient);

        // Act
        var read = await client.GetSummaryAsync(Integration, new IntegrationSummaryQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(MonitoringReadStatus.Found, read.Status);
        Assert.Equal(new MonitoringIntegrationSummary(Integration, 0, 0, 0, null), read.Value);
    }

    [Fact]
    public async Task GetSummaryAsync_WhenMonitoringRejectsPeriod_ReturnsInvalidParameters()
    {
        // Arrange
        using var httpClient = CreateHttpClient(CreateProblemResponse(HttpStatusCode.BadRequest, InvalidPeriodJson));
        var client = new MonitoringClient(httpClient);

        // Act
        var read = await client.GetSummaryAsync(Integration, new IntegrationSummaryQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(MonitoringReadStatus.InvalidRequest, read.Status);
        Assert.Equal(["from"], read.InvalidParameters!.Keys);
    }

    #endregion

    #region GetIntegrationSummariesAsync

    [Fact]
    public async Task GetIntegrationSummariesAsync_WhenPeriodIsNotSet_SendsSetWithoutBounds()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        string? body = null;

        using var httpClient = CreateHttpClient(async request =>
        {
            capturedRequest = request;
            body = await request.Content!.ReadAsStringAsync();
            return CreateJsonResponse(HttpStatusCode.OK, IntegrationSummariesJson);
        });
        var client = new MonitoringClient(httpClient);

        // Act
        await client.GetIntegrationSummariesAsync(
            [Integration, Guid.Parse(OtherIntegrationId)],
            new ProjectSummaryQuery(),
            CancellationToken.None
        );

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("/internal/v1/integration-summaries/query", capturedRequest.RequestUri!.AbsolutePath);
        // Параметры Monitoring принимает только в теле, непустой query он отклоняет
        Assert.Empty(capturedRequest.RequestUri.Query);
        Assert.Equal("application/json", capturedRequest.Content!.Headers.ContentType!.MediaType);

        // Явный null Monitoring отклоняет, поэтому границ без значения в теле нет
        using var json = JsonDocument.Parse(body!);
        Assert.Equal(
            ["integrationIds", "limit", "offset", "onlyFailed"],
            json.RootElement.EnumerateObject().Select(x => x.Name).Order()
        );
        Assert.Equal(
            [IntegrationId, OtherIntegrationId],
            json.RootElement.GetProperty("integrationIds").EnumerateArray().Select(x => x.GetString())
        );
        Assert.False(json.RootElement.GetProperty("onlyFailed").GetBoolean());
        Assert.Equal(0, json.RootElement.GetProperty("offset").GetInt32());
        Assert.Equal(20, json.RootElement.GetProperty("limit").GetInt32());
    }

    [Fact]
    public async Task GetIntegrationSummariesAsync_WhenFiltersAreSet_SendsThemInMonitoringFormat()
    {
        // Arrange
        string? body = null;

        using var httpClient = CreateHttpClient(async request =>
        {
            body = await request.Content!.ReadAsStringAsync();
            return CreateJsonResponse(HttpStatusCode.OK, IntegrationSummariesJson);
        });
        var client = new MonitoringClient(httpClient);

        var summaryQuery = new ProjectSummaryQuery
        {
            From = new DateTimeOffset(2026, 10, 26, 0, 0, 0, TimeSpan.FromHours(3)),
            To = new DateTimeOffset(2026, 10, 28, 0, 0, 0, TimeSpan.FromHours(3)).AddTicks(1_234_567),
            OnlyFailed = true,
            Offset = 40,
            Limit = 100,
        };

        // Act
        await client.GetIntegrationSummariesAsync([Integration], summaryQuery, CancellationToken.None);

        // Assert
        using var json = JsonDocument.Parse(body!);
        var root = json.RootElement;
        Assert.Equal("2026-10-25T21:00:00.000000Z", root.GetProperty("from").GetString());
        // Monitoring принимает не больше шести знаков дроби: седьмой отбрасывается
        Assert.Equal("2026-10-27T21:00:00.123456Z", root.GetProperty("to").GetString());
        Assert.True(root.GetProperty("onlyFailed").GetBoolean());
        Assert.Equal(40, root.GetProperty("offset").GetInt32());
        Assert.Equal(100, root.GetProperty("limit").GetInt32());
    }

    [Fact]
    public async Task GetIntegrationSummariesAsync_WhenMonitoringReturnsPage_ReturnsSummaries()
    {
        // Arrange
        using var httpClient = CreateHttpClient(CreateJsonResponse(HttpStatusCode.OK, IntegrationSummariesJson));
        var client = new MonitoringClient(httpClient);

        // Act
        var read = await client.GetIntegrationSummariesAsync(
            [Integration, Guid.Parse(OtherIntegrationId)],
            new ProjectSummaryQuery(),
            CancellationToken.None
        );

        // Assert
        Assert.Equal(MonitoringReadStatus.Found, read.Status);

        var page = read.Value!;
        Assert.Equal(2, page.Total);
        Assert.Equal(0, page.Offset);
        Assert.Equal(20, page.Limit);
        Assert.Collection(
            page.Items,
            failed =>
            {
                Assert.Equal(Integration, failed.IntegrationId);
                Assert.Equal(4, failed.TotalReports);
                Assert.Equal(2, failed.PassedReports);
                Assert.Equal(2, failed.FailedReports);
                Assert.Equal(new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc), failed.LastReceivedAt);
                Assert.Equal(DateTimeKind.Utc, failed.LastReceivedAt!.Value.Kind);
            },
            withoutReports =>
                Assert.Equal(
                    new MonitoringIntegrationSummary(Guid.Parse(OtherIntegrationId), 0, 0, 0, null),
                    withoutReports
                )
        );
    }

    [Fact]
    public async Task GetIntegrationSummariesAsync_WhenMonitoringRejectsPeriod_ReturnsInvalidParameters()
    {
        // Arrange
        using var httpClient = CreateHttpClient(CreateProblemResponse(HttpStatusCode.BadRequest, InvalidPeriodJson));
        var client = new MonitoringClient(httpClient);

        // Act
        var read = await client.GetIntegrationSummariesAsync([Integration], new ProjectSummaryQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(MonitoringReadStatus.InvalidRequest, read.Status);
        Assert.Equal(["from"], read.InvalidParameters!.Keys);
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestEntityTooLarge)]
    [InlineData(HttpStatusCode.UnsupportedMediaType)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task GetIntegrationSummariesAsync_WhenMonitoringFails_ReturnsUnavailable(HttpStatusCode statusCode)
    {
        // Arrange
        using var httpClient = CreateHttpClient(CreateProblemResponse(statusCode, """{"code":"STORAGE_UNAVAILABLE"}"""));
        var client = new MonitoringClient(httpClient);

        // Act
        var read = await client.GetIntegrationSummariesAsync([Integration], new ProjectSummaryQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(MonitoringReadStatus.Unavailable, read.Status);
        Assert.Contains($"HTTP {(int)statusCode}", read.Error);
    }

    [Fact]
    public async Task GetIntegrationSummariesAsync_WhenMonitoringIsUnreachable_ReturnsUnavailable()
    {
        // Arrange
        using var httpClient = CreateFailingHttpClient(new HttpRequestException("Connection refused"));
        var client = new MonitoringClient(httpClient);

        // Act
        var read = await client.GetIntegrationSummariesAsync([Integration], new ProjectSummaryQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(MonitoringReadStatus.Unavailable, read.Status);
        Assert.Contains("Connection refused", read.Error);
    }

    #endregion

    private static MonitoringReportRequest CreateMonitoringReportRequest() =>
        new(
            Integration,
            Guid.Parse(ContractId),
            new MonitoringReport(
                Guid.NewGuid(),
                1,
                ValidationStatus.Failed,
                new DateTime(2026, 10, 26, 19, 0, 0, DateTimeKind.Utc).AddTicks(1_234_560),
                [CreateTypeMismatch()]
            )
        );
}
