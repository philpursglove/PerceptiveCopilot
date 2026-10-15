using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using PerceptiveCopilot.Data;
using PerceptiveCopilot.Server.DTO;

namespace PerceptiveCopilot.Server
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
            float[] rawVectorArray = embeddingResult.Vector.ToArray();
            var queryVector = new SqlVector<float>(rawVectorArray);

            // 2. Perform the semantic search via Entity Framework LINQ
            var topChunks = await _dbContext.DocumentChunks
                .OrderBy(chunk => EF.Functions.VectorDistance("cosine", chunk.Embedding, queryVector)) // Translated to T-SQL native VECTOR_DISTANCE
                .Take(3)
                .Select(chunk => chunk.ContentChunk)
                .ToListAsync();

            string retrievedContext = string.Join("\n\n", topChunks);

            // 3. System Instructions: Direct the model to act as a grounded knowledge-bot
            var promptMessages = new List<ChatMessage>
            {
                new(ChatRole.System, $"You are a helpful website assistant. Answer the user's question using ONLY the provided text documentation context. If the answer cannot be found in the context, politely say 'I cannot find that information in our current documentation.'\n\nContext:\n{retrievedContext}"),
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