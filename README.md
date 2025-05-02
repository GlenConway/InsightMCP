# InsightMCP

A Model Context Protocol (MCP) server for managing and retrieving medical reports. This service provides an API for accessing structured medical report data stored in CSV format, including detailed questions and answers associated with each report.

## Features

- Load and manage medical reports from CSV files
- Access report details including questions and answers
- Advanced protocol search functionality with multiple filters:
  - Filter by organ system
  - Filter by year
  - Filter by protocol version
  - Pagination support
- Custom JSON serialization for robust data handling
- MCP server implementation for remote access to report data
- Structured data model for medical reports
- Async operations for efficient data access

## Getting Started

### Prerequisites

- .NET 9.0 or later
- Visual Studio 2022 or VS Code with C# extension

### Installation

1. Clone the repository
2. Open the solution in Visual Studio or VS Code
3. Build the solution:
   ```bash
   dotnet build
   ```

### Running the Service

Run the service using:
```bash
dotnet run --project InsightMCP/InsightMCP.csproj
```

### Running Tests

Run the test suite using:
```bash
dotnet test
```

## Project Structure

- `InsightMCP/` - Main project containing the MCP server implementation
  - `Models/` - Data models for Reports and Question/Answers
    - `Report.cs` - Report model with custom JSON serialization
  - `Services/` - Report service implementation
  - `Tools/` - MCP tools for accessing report data
    - `ProtocolSearchFilter.cs` - Advanced protocol search functionality
- `InsightMCP.Tests/` - Test project with unit tests

## Protocol Search API

The `ProtocolSearchFilter` tool provides a flexible search API for medical protocols:

```csharp
SearchProtocols(
    string? organSystem = null,    // Filter by organ system name
    int? year = null,             // Filter by year
    string? version = null,        // Filter by version number
    int pageSize = 10,            // Results per page (max 100)
    string? cursor = null         // Pagination cursor
)
```

Response format:
```json
{
  "items": [...],              // Array of matching protocols
  "nextCursor": "base64==",    // Next page cursor
  "hasMore": true/false,       // More results available
  "totalCount": 42             // Total matching protocols
}
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.