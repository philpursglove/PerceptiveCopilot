using Microsoft.Extensions.AI;
using OpenAI;


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

string foundryBaseUri = "https://azure.com";
string foundryApiKey = "YOUR_AZURE_AI_FOUNDRY_PROJECT_KEY";
string embeddingModelDeploymentName = "text-embedding-3-small"; // Your MaaS embedding deployment

var openAIClient = new OpenAIClient(
    new System.ClientModel.ApiKeyCredential(foundryApiKey),
    new OpenAIClientOptions { Endpoint = new Uri(foundryBaseUri) }
);

IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = openAIClient
    .GetEmbeddingClient(embeddingModelDeploymentName)
    .AsIEmbeddingGenerator();

var ingestionService = new PdfIngestionService(embeddingGenerator);

foreach (var filePath in filePaths)
{
    await ingestionService.IngestPdfAsync(filePath, System.IO.Path.GetFileName(filePath));
}