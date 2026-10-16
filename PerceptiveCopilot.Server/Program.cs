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
    public string DeploymentName { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}