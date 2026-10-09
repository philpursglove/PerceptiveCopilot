using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using OpenAI;
using PerceptiveCopilot.Data;


List<string> filePaths =
[
    "C:\\Users\\Phil\\Downloads\\aad-eod-art.pdf",
    "C:\\Users\\Phil\\Downloads\\resistance-ywing-loadout.pdf",
    "C:\\Users\\Phil\\Downloads\\scenarios.pdf",
    "C:\\Users\\Phil\\Downloads\\legends-and-relics.pdf",
    "C:\\Users\\Phil\\Downloads\\errata-v1.8.2.pdf",
    "C:\\Users\\Phil\\Downloads\\rules-reference-v1.4.6.pdf",
    "C:\\Users\\Phil\\Downloads\\rulebook-v1.1.1.pdf",
    "C:\\Users\\Phil\\Downloads\\Points-Update-50p-2-1-20260816.pdf",
    "C:\\Users\\Phil\\Downloads\\copr-v2.0.pdf",
    "C:\\Users\\Phil\\Downloads\\pnp-starwing-tie-phantom.pdf"
];

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>(optional: false)
    .Build();

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
