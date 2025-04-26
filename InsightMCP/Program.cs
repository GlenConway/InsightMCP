using InsightMCP.Services;
using InsightMCP.Tools;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configure logging for MCP debugging
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Logging.AddDebug();
builder.Logging.AddFilter("ModelContextProtocol", LogLevel.Debug);
builder.Logging.AddConsole();

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<ReportTools>()
    .WithTools<TestTools>();

builder.Services.AddHttpClient();
builder.Services.AddScoped<IReportService, ReportService>();

var app = builder.Build();

app.MapMcp();

app.Run();