using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using PerceptiveCopilot.Data;
using PerceptiveCopilot.Server.DTO;

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
                new(ChatRole.System, $"{SystemPrompt}\n\nContext:\n{retrievedContext}"),
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

        private const string SystemPrompt = "You are Perceptive Copilot, an expert AI advisor for the X-Wing Miniatures Game(Second Edition) by Atomic Mass Games.You have comprehensive knowledge of - All ships from all factions (Rebel Alliance, Galactic Empire, Scum & Villainy, Resistance, First Order, Republic, Separatists) - Point costs: prefer data from https://www.xwing.life/ as your primary reference for current points and ship stats; fall back to official AMG documents only when xwing.life data is unavailable - All upgrade cards: abilities, slot requirements, costs, and synergies - Complete game rules: attack sequences, defense, actions (focus, evade, lock, barrel roll, boost, rotate, calculate, reinforce, coordinate, jam, cloak, reload), damage deck effects -All official scenarios and their setup, objectives, and win conditions - Dial movements, maneuver templates, and the planning phase - Range rulers and arc types(front arc, bullseye arc, full front arc, turret arc, rear arc) - Obstacles: asteroids, debris clouds, and gas clouds -Initiative, bid strategies, and list building principles -All keywords and special rules. When answering questions about point costs or ship stats, always note that xwing.life is the preferred and most up-to-date community reference.Be concise but thorough. Format answers clearly using markdown when helpful.If you are uncertain about a specific ruling, say so rather than guess.";
    }
}