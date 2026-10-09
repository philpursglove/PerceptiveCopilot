using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using OpenAI;
using PerceptiveCopilot.Data;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddUserSecrets<Program>(optional: false)
    .Build();

var documentsFolder = GetRequiredSetting(configuration, "Ingestion:DocumentsFolder");

var filePaths = configuration
    .GetSection("Ingestion:FilePaths")
    .GetChildren()
    .Select(section => Path.Combine(documentsFolder, section.Value))
    .Where(path => !string.IsNullOrWhiteSpace(path))
    .Cast<string>()
    .ToList();

var foundryBaseUri = GetRequiredSetting(configuration, "Foundry:BaseUri");
var foundryApiKey = GetRequiredSetting(configuration, "Foundry:ApiKey");
var connectionString = GetRequiredSetting(configuration, "ConnectionStrings:PerceptiveCopilot");
var embeddingModelDeploymentName = "text-embedding-3-small"; // Your MaaS embedding deployment

var openAIClient = new OpenAIClient(
    new System.ClientModel.ApiKeyCredential(foundryApiKey),
    new OpenAIClientOptions { Endpoint = new Uri(foundryBaseUri) }
);

var dbContextOptions = new DbContextOptionsBuilder<PerceptiveCopilotDbContext>()
    .UseSqlServer(connectionString)
    .Options;

IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = openAIClient
    .GetEmbeddingClient(embeddingModelDeploymentName)
    .AsIEmbeddingGenerator();

var ingestionService = new PdfIngestionService(embeddingGenerator, dbContextOptions);

foreach (var filePath in filePaths)
{
    if (File.Exists(filePath))
    {
        Console.WriteLine($"Ingesting PDF: {filePath}");
        await ingestionService.IngestPdfAsync(filePath, System.IO.Path.GetFileName(filePath));
        Console.WriteLine($"Finished ingesting PDF: {filePath}");
    }
    else
    {
        Console.WriteLine($"File not found: {filePath}");
    }
}

static string GetRequiredSetting(IConfiguration configuration, string key)
{
    return configuration[key]
        ?? throw new InvalidOperationException($"Missing required secret '{key}'. Configure it with 'dotnet user-secrets set \"{key}\" \"<value>\"'.");
}
