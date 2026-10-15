using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OpenAI;
using PerceptiveCopilot.Data;

var builder = WebApplication.CreateBuilder(args);

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddUserSecrets<Program>(optional: false)
    .Build();

var foundrySettings = configuration.GetSection("Foundry").Get<FoundrySettings>();
var deploymentSettings = configuration.GetSection("Deployments").Get<DeploymentSettings>();

var openAIClient = new OpenAIClient(
    new System.ClientModel.ApiKeyCredential(foundrySettings.ApiKey),
    new OpenAIClientOptions { Endpoint = new Uri(foundrySettings.BaseUrl) }
);
// Add services to the container.
builder.Services.AddChatClient(services =>
        openAIClient
            .GetChatClient(deploymentSettings.ChatModelDeploymentName)
            .AsIChatClient() // e.g. "llama-3-3-70b-instruct"
    );

// 2. Register the Vector Embedding Generator
builder.Services.AddEmbeddingGenerator<string, Embedding<float>>(services =>
        openAIClient
            .GetEmbeddingClient(deploymentSettings.EmbeddingModelDeploymentName)
            .AsIEmbeddingGenerator()
    );

builder.Services.AddDbContext<PerceptiveCopilotDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("PerceptiveCopilot"))
);

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseDefaultFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();

public class FoundrySettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

public class DeploymentSettings
{
    public string ChatModelDeploymentName { get; set; } = string.Empty;
    public string EmbeddingModelDeploymentName { get; set; } = string.Empty;
}