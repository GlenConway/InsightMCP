using ModelContextProtocol.Server;
using System.ComponentModel;

namespace InsightMCP.Tools;

[McpServerToolType]
public class TestTools
{
    [McpServerTool, Description("Just a quick test.")]
    public string Test1()
    {
        return "Hello from TestTools!";
    }

    [McpServerTool, Description("Just a quick async test.")]
    public Task<string> Test2Async()
    {
        return Task.FromResult("Async hello from TestTools!");
    }
}