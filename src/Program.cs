using System.Reflection;
using ContractWatcher.Core.Data;
using ContractWatcher.Core.Data.Interceptors;
using ContractWatcher.Core.Extensions;
using ContractWatcher.Core.Services.ApiKeys;
using ContractWatcher.Core.Services.Contracts;
using ContractWatcher.Core.Services.Integrations;
using ContractWatcher.Core.Services.Projects;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.AddSettingsJson();
builder.Services
    .AddValidators(Assembly.GetExecutingAssembly())
    .AddHttpContextAccessor()
    .AddLogging()
    .AddSingleton(TimeProvider.System)
    .AddSingleton<IInterceptor, TimestampInterceptor>();

builder.Services.AddDataContext<DataContext>(
    builder.Configuration.GetValue<string>("DbConnections:Postgres:ConnectionString")!,
    builder.Configuration.GetValue<int>("DbConnections:Postgres:MaxRetry"),
    builder.Configuration.GetValue<int>("DbConnections:Postgres:MaxDelaySec")
);

builder.Services.AddAuth(builder.Configuration);

builder.Services
    .AddScoped<ProjectService>()
    .AddScoped<IntegrationService>()
    .AddScoped<ApiKeyService>()
    .AddScoped<ContractService>();

builder.Services.AddControllers();

var app = builder.Build();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.PrepareAndRun<DataContext>(args);