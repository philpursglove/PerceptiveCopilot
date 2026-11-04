using Azure;
using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using PerceptiveCopilot.Core;
using PerceptiveCopilot.Data;

namespace PerceptiveCopilot.Functions;

public class ChatFunction
{
    private readonly ILogger<ChatFunction> _logger;
    private readonly IChatClient _chatClient;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly PerceptiveCopilotDbContext _dbContext;

    public ChatFunction(ILogger<ChatFunction> logger, IChatClient chatClient, IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        PerceptiveCopilotDbContext dbContext)
    {
        _logger = logger;
        _chatClient = chatClient;
        _embeddingGenerator = embeddingGenerator;
        _dbContext = dbContext;
    }

    [Function("ChatFunction")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "chat")] HttpRequest req,
        [FromBody] ChatRequest chatRequest)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        // Implement your chat logic here

        // 1. Generate the coordinate vector from the user's string query
        var embeddingResult = await _embeddingGenerator.GenerateAsync(chatRequest.Message);
        var rawVectorArray = embeddingResult.Vector.ToArray();
        var queryVector = new SqlVector<float>(rawVectorArray);

        // 2. Perform the semantic search via Entity Framework LINQ
        var topChunks = await _dbContext.DocumentChunks
            .OrderBy(chunk => EF.Functions.VectorDistance("cosine", chunk.Embedding, queryVector)) // Translated to T-SQL native VECTOR_DISTANCE
            .Take(3)
            .Select(chunk => chunk.ContentChunk)
            .ToListAsync();

        var retrievedContext = string.Join("\n\n", topChunks);

        // 3. System Instructions: Direct the model to act as a grounded knowledge-bot
        var promptMessages = new List<ChatMessage>
            {
                new(ChatRole.System, $"{Constants.SystemPrompt}\n\nContext:\n{retrievedContext}"),
                new(ChatRole.User, chatRequest.Message)
            };

        var response = req.HttpContext.Response;

        // 4. Stream the text tokens back to the TypeScript frontend using Server-Sent Events
        response.ContentType = "text/event-stream";
        await foreach (var chunk in _chatClient.GetStreamingResponseAsync(promptMessages))
        {
            if (!string.IsNullOrEmpty(chunk.Text))
            {
                await response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(chunk.Text)}\n\n");
                await response.Body.FlushAsync();
            }
        }

        return new EmptyResult();
    }
}