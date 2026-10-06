using System.Reflection;
using ContractWatcher.Core.Data;
using ContractWatcher.Core.Data.Interceptors;
using ContractWatcher.Core.Extensions;
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

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.PrepareAndRun<DataContext>(args);