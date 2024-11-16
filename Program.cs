using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Azure.AI.ContentSafety;
using Azure;
using ClaudeCustomConnector;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Add HttpClient
        services.AddHttpClient();

        // Add logging
        services.AddLogging();

        // Configure Content Safety Client
        services.AddSingleton(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var endpoint = configuration["CONTENT_SAFETY_ENDPOINT"];
            var key = configuration["CONTENT_SAFETY_KEY"];

            return new ContentSafetyClient(
                new Uri(endpoint ?? throw new InvalidOperationException("Content Safety endpoint not configured")),
                new AzureKeyCredential(key ?? throw new InvalidOperationException("Content Safety key not configured")));
        });

        // Register GetResponse
        services.AddSingleton<GetResponse>();
    })
    .ConfigureLogging((context, builder) =>
    {
        builder.AddConsole();
        
    })
    .Build();

host.Run();