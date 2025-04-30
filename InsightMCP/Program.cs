using InsightMCP.Services;
using InsightMCP.Tools;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol.Transport;
using ModelContextProtocol.Protocol.Types;

var builder = WebApplication.CreateBuilder(args);

// Configure logging for MCP debugging
builder.Logging.SetMinimumLevel(LogLevel.Debug);
builder.Logging.AddDebug();
builder.Logging.AddFilter("ModelContextProtocol", LogLevel.Debug);
builder.Logging.AddConsole();

McpServerOptions options = new()
{
    ServerInfo = new Implementation() { Name = "Insight MCP Server", Version = "1.0.0" },
    Capabilities = new ServerCapabilities()
    {
        Tools = new ToolsCapability()
        {
            

            CallToolHandler = (request, cancellationToken) =>
            {
                
                if (request.Params?.Name == "organSystem")
                {
                    if (request.Params.Arguments?.TryGetValue("message", out var message) is not true)
                    {
                        throw new ModelContextProtocol.McpException("Missing required argument 'message'");
                    }

                    return ValueTask.FromResult(new CallToolResponse()
                    {
                        Content = [new Content() { Text = $"Echo: {message}", Type = "text" }]
                    });
                }

                throw new ModelContextProtocol.McpException($"Unknown tool: '{request.Params?.Name}'");
            },
        }
    },
};

builder.Services
    .AddMcpServer((options) => {})
    .WithHttpTransport()
    .WithToolsFromAssembly();

builder.Services.AddHttpClient();
builder.Services.AddScoped<IReportService, ReportService>();

var app = builder.Build();

app.MapMcp();

app.Run();