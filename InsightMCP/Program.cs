using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using InsightMCP.Services;

// Define command-line verbs for different launch modes

/// <summary>
/// Verb for launching in stdio mode (default mode)
/// Uses Host.CreateApplicationBuilder for console input/output
/// </summary>
[Verb("stdio", isDefault: true, HelpText = "Launch in stdio mode using Host.CreateApplicationBuilder for console I/O")]
public class StdioVerb
{
    // Add any stdio-specific options here if needed in the future
}

/// <summary>
/// Verb for launching in SSE mode 
/// Uses WebApplication.CreateBuilder for Server-Sent Events
/// </summary>
[Verb("sse", HelpText = "Launch in SSE mode using WebApplication.CreateBuilder for Server-Sent Events")]
public class SseVerb
{
    // Add any sse-specific options here if needed in the future
}

// Entry point of the application
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Create logs directory if it doesn't exist
        var logsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        Directory.CreateDirectory(logsDirectory);

        // Create initial bootstrap logger for startup
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(Path.Combine(logsDirectory, "startup.log"))
            .CreateBootstrapLogger();

        try
        {
            // Parse command-line verbs using CommandLineParser
            // Use Parser to match the appropriate verb and execute the corresponding action
            return await Parser.Default.ParseArguments<StdioVerb, SseVerb>(args)
                .MapResult(
                    // Map each verb to its corresponding action
                    (StdioVerb stdio) => RunStdioMode(args, logsDirectory),
                    (SseVerb sse) => RunSseMode(args, logsDirectory),
                    // Handle parsing errors
                    errors =>
                    {
                        foreach (var error in errors)
                        {
                            Log.Warning("Command-line parsing error: {Error}", error.Tag);
                        }
                        Log.Information("Using default 'stdio' mode due to parsing errors");
                        return RunStdioMode(args, logsDirectory);
                    });
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly at startup");
            return 1;
        }
    }

    /// <summary>
    /// Runs the application in stdio mode
    /// </summary>
    static async Task<int> RunStdioMode(string[] args, string logsDirectory)
    {
        try
        {
            Log.Information("Starting Insight MCP server in stdio mode...");

            // Create builder for stdio mode
            var builder = CreateStdioBuilder(args, logsDirectory);

            // Build and run the application
            var app = builder.Build();
            await app.RunAsync();

            Log.Information("Stopped Insight MCP server in stdio mode");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application in stdio mode terminated unexpectedly");
            return 1;
        }
    }

    /// <summary>
    /// Runs the application in SSE mode
    /// </summary>
    static async Task<int> RunSseMode(string[] args, string logsDirectory)
    {
        try
        {
            Log.Information("Starting Insight MCP server in SSE mode...");

            // Create builder for SSE mode
            var builder = CreateSseBuilder(args, logsDirectory);

            // Build and run the application
            var app = builder.Build();
            app.MapMcp();
            await app.RunAsync();

            Log.Information("Stopped Insight MCP server in SSE mode");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application in SSE mode terminated unexpectedly");
            return 1;
        }
    }

    /// <summary>
    /// Creates a WebApplicationBuilder configured for SSE mode
    /// </summary>
    static WebApplicationBuilder CreateSseBuilder(string[] args, string logsDirectory)
    {
        // Since we're using verbs, we need to filter out the verb command from args
        var filteredArgs = args.Skip(1).ToArray(); // Skip the verb ("sse")
        var builder = WebApplication.CreateBuilder(filteredArgs);

        // Configure common logging
        ConfigureLogging(builder, logsDirectory);

        // Configure SSE-specific services
        builder.Services
            .AddMcpServer()
            .WithHttpTransport()
            .WithToolsFromAssembly();

        builder.Services.AddScoped<IReportService, ReportService>();

        return builder;
    }

    /// <summary>
    /// Creates a HostApplicationBuilder configured for stdio mode
    /// </summary>
    static HostApplicationBuilder CreateStdioBuilder(string[] args, string logsDirectory)
    {
        // Since we're using verbs, we need to filter out the verb command from args
        var filteredArgs = args.Skip(1).ToArray(); // Skip the verb ("stdio")

        var builder = Host.CreateApplicationBuilder(filteredArgs);

        // Configure common logging
        ConfigureLogging(builder, logsDirectory);

        // Configure StdIO-specific services
        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly();

        builder.Services.AddScoped<IReportService, ReportService>();

        return builder;
    }

    /// <summary>
    /// Common logging configuration used by both modes
    /// </summary>
    /// <param name="builder">The builder object (either WebApplicationBuilder or HostApplicationBuilder)</param>
    /// <param name="logsDirectory">Directory where log files will be stored</param>
    static void ConfigureLogging(object builder, string logsDirectory)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("ModelContextProtocol", LogEventLevel.Warning)
            .MinimumLevel.Override("InsightMCP", LogEventLevel.Debug)
            .Enrich.FromLogContext()
            .WriteTo.File(
                new JsonFormatter(),
                Path.Combine(logsDirectory, "insight-mcp.log"),
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB
                retainedFileCountLimit: 10,
                restrictedToMinimumLevel: LogEventLevel.Verbose)
            .CreateLogger();

        // Clear default providers and add Serilog based on builder type
        switch (builder)
        {
            case WebApplicationBuilder webBuilder:
                webBuilder.Logging.ClearProviders();
                webBuilder.Services.AddSerilog(Log.Logger);
                break;
            case HostApplicationBuilder hostBuilder:
                hostBuilder.Logging.ClearProviders();
                hostBuilder.Services.AddSerilog(Log.Logger);
                break;
            default:
                throw new ArgumentException($"Unsupported builder type: {builder.GetType().Name}", nameof(builder));
        }
    }
}
