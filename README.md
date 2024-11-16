# Claude Custom Connector

A .NET 8 Azure Function that provides a secure intermediary service for interacting with Anthropic's Claude API while implementing content safety checks using Azure AI Content Safety.

## Features

- 🔒 Content safety validation for both input and output using Azure AI Content Safety
- 🤖 Integration with Claude 3.5 Sonnet API
- 🚀 Built on .NET 8 Azure Functions
- 📝 Comprehensive logging with Application Insights
- ⚡ Optimized HTTP client management
- 🛡️ Environment-based configuration

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Functions Core Tools](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) or [VS Code](https://code.visualstudio.com/)
- Azure Subscription (for Content Safety API)
- Anthropic API Key (for Claude API)

## Configuration

Create a `local.settings.json` file in your project root:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "CLAUDE_API_KEY": "your-claude-api-key",
    "CLAUDE_API_ENDPOINT": "https://api.anthropic.com/v1/messages",
    "CLAUDE_API_MODEL": "claude-3-5-sonnet-20241022",
    "CONTENT_SAFETY_KEY": "your-content-safety-key",
    "CONTENT_SAFETY_ENDPOINT": "https://your-instance.cognitiveservices.azure.com/",
    "AZURE_FUNCTION_ENVIRONMENT": "Development"
  }
}
```

## Project Structure

```
ClaudeCustomConnector/
├── GetResponse.cs          # Main function implementation
├── Program.cs             # Application startup and DI configuration
├── local.settings.json    # Configuration settings
└── ClaudeCustomConnector.csproj
```

## Installation

1. Clone the repository:
```bash
git clone https://github.com/yourusername/claude-custom-connector.git
```

2. Install dependencies:
```bash
dotnet restore
```

3. Update the `local.settings.json` with your API keys and endpoints.

## Usage

### Running Locally

1. Start the function app:
```bash
func start
```

2. Send a POST request to the endpoint:
```http
POST http://localhost:7071/api/GetResponse
Content-Type: application/json

{
    "prompt": "Your question here",
    "systemMessage": "Optional system message"
}
```

### Example Request

```bash
curl -X POST http://localhost:7071/api/GetResponse \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "What is the capital of France?",
    "systemMessage": "You are a helpful assistant."
  }'
```

### Response Format

```json
{
    "isSuccess": true,
    "message": "Content processed successfully.",
    "llmResponse": "The capital of France is Paris..."
}
```

## Error Handling

The connector handles various error scenarios:

- Content Safety Violations (HTTP 200 with error message)
- Authentication Errors (HTTP 401)
- Invalid Requests (HTTP 400)
- Server Errors (HTTP 500)

## Deployment

### To Azure

1. Create required Azure resources:
   - Azure Function App
   - Azure AI Content Safety instance
   - Application Insights (optional)

2. Deploy using Azure Functions Core Tools:
```bash
func azure functionapp publish YOUR_FUNCTION_APP_NAME
```

3. Configure application settings in Azure Portal.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request


## Dependencies

- Microsoft.Azure.Functions.Worker
- Microsoft.Azure.Functions.Worker.Extensions.Http
- Microsoft.Azure.Functions.Worker.Sdk
- Microsoft.ApplicationInsights.WorkerService
- Azure.AI.ContentSafety



