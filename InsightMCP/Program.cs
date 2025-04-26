using InsightMCP;
using InsightMCP.Services;
using InsightMCP.Tools;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;
var builder = WebApplication.CreateBuilder(args);

// Configure logging for MCP debugging
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddFilter("ModelContextProtocol", LogLevel.Debug);
builder.Logging.AddConsole();

builder.Services
    .AddMcpServer()
    .WithTools<ReportTools>();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<ReportService>();

var app = builder.Build();

app.Run();