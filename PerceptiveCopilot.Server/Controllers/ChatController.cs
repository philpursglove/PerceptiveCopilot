using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using PerceptiveCopilot.Core;
using PerceptiveCopilot.Data;

namespace PerceptiveCopilot.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IChatClient _chatClient;
        private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
        private readonly PerceptiveCopilotDbContext _dbContext;

        public ChatController(IChatClient chatClient, IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
            PerceptiveCopilotDbContext dbContext)
        {
            _chatClient = chatClient;
            _embeddingGenerator = embeddingGenerator;
            _dbContext = dbContext;
        }

        [HttpPost("chat")]
        public async Task Chat(ChatRequest request)
        {
            // Implement your chat logic here

            // 1. Generate the coordinate vector from the user's string query
            var embeddingResult = await _embeddingGenerator.GenerateAsync(request.Message);
            var rawVectorArray = embeddingResult.Vector.ToArray();
            var queryVector = new SqlVector<float>(rawVectorArray);

            List<string> topChunks;
            // 2. Perform the semantic search via Entity Framework LINQ
            try
            {
                topChunks = await _dbContext.DocumentChunks
                    .OrderBy(chunk => EF.Functions.VectorDistance("cosine", chunk.Embedding, queryVector)) // Translated to T-SQL native VECTOR_DISTANCE
                    .Take(10)
                    .Select(chunk => chunk.ContentChunk)
                    .ToListAsync();

            }
            catch (Exception)
            {
                topChunks = await _dbContext.DocumentChunks
                    .OrderBy(chunk => EF.Functions.VectorDistance("cosine", chunk.Embedding, queryVector)) // Translated to T-SQL native VECTOR_DISTANCE
                    .Take(10)
                    .Select(chunk => chunk.ContentChunk)
                    .ToListAsync();
            }

            var retrievedContext = string.Join("\n\n", topChunks);

            // 3. System Instructions: Direct the model to act as a grounded knowledge-bot
            var promptMessages = new List<ChatMessage>
            {
                new(ChatRole.System, $"{Constants.SystemPrompt}\n\nContext:\n{retrievedContext}"),
                new(ChatRole.User, request.Message)
            };

            // 4. Stream the text tokens back to the TypeScript frontend using Server-Sent Events
            Response.ContentType = "text/event-stream";
            await foreach (var chunk in _chatClient.GetStreamingResponseAsync(promptMessages))
            {
                if (!string.IsNullOrEmpty(chunk.Text))
                {
                    await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(chunk.Text)}\n\n");
                    await Response.Body.FlushAsync();
                }
            }
        }
    }
}