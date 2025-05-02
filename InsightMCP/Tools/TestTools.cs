using ModelContextProtocol.Server;
using System.ComponentModel;
using Microsoft.Extensions.Logging;

namespace InsightMCP.Tools;

[McpServerToolType]
public class TestTools
{
    private readonly ILogger<TestTools> _logger;

    public TestTools(ILogger<TestTools> logger)
    {
        _logger = logger;
    }

    [McpServerTool, Description("Just a quick test.")]
    public string Test1()
    {
        _logger.LogInformation("Test1 called");

        return "Hello from TestTools!";
    }

    [McpServerTool, Description("Just a quick async test.")]
    public Task<string> Test2Async()
    {
        _logger.LogInformation("Test2Async called");

        return Task.FromResult("Async hello from TestTools!");
    }
}