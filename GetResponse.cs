using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.AI.ContentSafety;
using Azure;

namespace ClaudeCustomConnector
{
    public class ContentSafetyRequest
    {
        public required string Prompt { get; set; }
        public required string SystemMessage { get; set; }
    }

    public class ContentSafetyResponse
    {
        public bool IsSuccess { get; set; }
        public required string Message { get; set; }
        public required string? LlmResponse { get; set; }
    }

    public class GetResponse
    {
        private readonly ILogger<GetResponse> _logger;
        private readonly string CLAUDE_API_KEY;
        private readonly string CLAUDE_API_ENDPOINT;
        private readonly string CLAUDE_API_MODEL_NAME;
        private readonly string CONTENT_SAFETY_KEY;
        private readonly string CONTENT_SAFETY_ENDPOINT;
        private readonly ContentSafetyClient _contentSafetyClient;
        private readonly HttpClient _httpClient;

        public GetResponse(ILogger<GetResponse> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            CLAUDE_API_KEY = Environment.GetEnvironmentVariable("CLAUDE_API_KEY");
            CLAUDE_API_ENDPOINT = Environment.GetEnvironmentVariable("CLAUDE_API_ENDPOINT");
            CLAUDE_API_MODEL_NAME = Environment.GetEnvironmentVariable("CLAUDE_API_MODEL");
            CONTENT_SAFETY_KEY = Environment.GetEnvironmentVariable("CONTENT_SAFETY_KEY");
            CONTENT_SAFETY_ENDPOINT = Environment.GetEnvironmentVariable("CONTENT_SAFETY_ENDPOINT");

            _contentSafetyClient = new ContentSafetyClient(
                new Uri(CONTENT_SAFETY_ENDPOINT),
                new AzureKeyCredential(CONTENT_SAFETY_KEY));

            _httpClient = httpClientFactory.CreateClient();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", CLAUDE_API_KEY);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        }

        [Function("GetResponse")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("Processing request in GetResponse function.");

            try
            {
                // Read request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var data = JsonConvert.DeserializeObject<ContentSafetyRequest>(requestBody);

                if (string.IsNullOrEmpty(data?.Prompt))
                {
                    return new BadRequestObjectResult("Please provide a prompt in the request body.");
                }

                // Check input content safety
                var inputSafetyResult = await CheckContentSafety(data.Prompt);
                if (!inputSafetyResult)
                {
                    return new OkObjectResult(new ContentSafetyResponse
                    {
                        IsSuccess = false,
                        Message = "Input content violates content safety guidelines.",
                        LlmResponse = null
                    });
                }

                // Call Claude API
                var llmResponse = await CallClaudeApi(data.Prompt, data.SystemMessage);
                if (string.IsNullOrEmpty(llmResponse))
                {
                    return new OkObjectResult(new ContentSafetyResponse
                    {
                        IsSuccess = false,
                        Message = "Failed to get response from Claude API.",
                        LlmResponse = null
                    });
                }

                // Check output content safety
                var outputSafetyResult = await CheckContentSafety(llmResponse);
                if (!outputSafetyResult)
                {
                    return new OkObjectResult(new ContentSafetyResponse
                    {
                        IsSuccess = false,
                        Message = "LLM response violates content safety guidelines.",
                        LlmResponse = null
                    });
                }

                // Return successful response
                return new OkObjectResult(new ContentSafetyResponse
                {
                    IsSuccess = true,
                    Message = "Content processed successfully.",
                    LlmResponse = llmResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing request: {ex.Message}");
                return new StatusCodeResult(500);
            }
        }

        private async Task<bool> CheckContentSafety(string content)
        {
            try
            {
                var request = new AnalyzeTextOptions(content);
                var response = await _contentSafetyClient.AnalyzeTextAsync(request);

                // Check for harmful content categories
                foreach (var category in response.Value.CategoriesAnalysis)
                {
                    if (category.Severity >= 4) // High severity threshold
                    {
                        _logger.LogWarning($"Content safety violation detected: {category.Category} with severity {category.Severity}");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking content safety: {ex.Message}");
                return false;
            }
        }

        private async Task<string> CallClaudeApi(string prompt, string systemMessage)
        {
            try
            {
                // Reset HttpClient headers to ensure clean state
                _httpClient.DefaultRequestHeaders.Clear();

                // Add required headers for Claude API
                _httpClient.DefaultRequestHeaders.Add("x-api-key", CLAUDE_API_KEY);
                _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

                // Add bearer token authentication
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CLAUDE_API_KEY);

                var requestBody = new
                {
                    model = "claude-3-sonnet-20240229",
                    max_tokens = 1024,
                    messages = new[]
                    {
                new { role = "system", content = systemMessage },
                new { role = "user", content = prompt }
            }
                };

                // Log request details (for debugging)
                _logger.LogInformation($"Calling Claude API with headers: {string.Join(", ", _httpClient.DefaultRequestHeaders)}");

                var response = await _httpClient.PostAsync(
                    "https://api.anthropic.com/v1/messages",
                    new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json")
                );

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Claude API Response Status: {response.StatusCode}");
                _logger.LogInformation($"Claude API Response Content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    dynamic responseObj = JsonConvert.DeserializeObject(responseContent);
                    return responseObj.content[0].text;
                }
                else
                {
                    _logger.LogError($"Claude API error: {response.StatusCode} - {responseContent}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calling Claude API: {ex.Message}");
                return null;
            }
        }
    }
}