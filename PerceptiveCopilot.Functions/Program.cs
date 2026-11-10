using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI;
using PerceptiveCopilot.Data;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

var configuration = new ConfigurationBuilder()
    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>(optional: true)
    .Build();

var embeddingSettings = configuration.GetSection("Foundry:Embedding").Get<FoundrySettings>();
var chatSettings = configuration.GetSection("Foundry:Chat").Get<FoundrySettings>();

var chatOpenAIClient = new OpenAIClient(
    new System.ClientModel.ApiKeyCredential(chatSettings.ApiKey),
    new OpenAIClientOptions { Endpoint = new Uri(chatSettings.BaseUrl) }
);
// Add services to the container.
builder.Services.AddChatClient(services =>
        chatOpenAIClient
            .GetChatClient(chatSettings.DeploymentName)
            .AsIChatClient() // e.g. "llama-3-3-70b-instruct"
);

var embeddingOpenAIClient = new OpenAIClient(
    new System.ClientModel.ApiKeyCredential(embeddingSettings.ApiKey),
    new OpenAIClientOptions { Endpoint = new Uri(embeddingSettings.BaseUrl) }
);

// 2. Register the Vector Embedding Generator
builder.Services.AddEmbeddingGenerator<string, Embedding<float>>(services =>
    embeddingOpenAIClient
        .GetEmbeddingClient(embeddingSettings.DeploymentName)
        .AsIEmbeddingGenerator()
);

builder.Services.AddDbContext<PerceptiveCopilotDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("PerceptiveCopilot"))
);

builder.Build().Run();
