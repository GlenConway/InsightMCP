# InsightMCP

A Model Context Protocol (MCP) server for managing and retrieving medical reports. This service provides an API for accessing structured medical report data stored in CSV format, including detailed questions and answers associated with each report.

## Features

- Load and manage medical reports from CSV files
- Access report details including questions and answers
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

## Project Structure

- `InsightMCP/` - Main project containing the MCP server implementation
  - `Models/` - Data models for Reports and Question/Answers
  - `Services/` - Report service implementation
  - `Tools/` - MCP tools for accessing report data
- `InsightMCP.Tests/` - Test project with unit tests

## License

This project is licensed under the MIT License - see the LICENSE file for details.