using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using PerceptiveCopilot.Data;
using System.Text;
using UglyToad.PdfPig;

public class PdfIngestionService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly DbContextOptions<PerceptiveCopilotDbContext> _dbContextOptions;

    public PdfIngestionService(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        DbContextOptions<PerceptiveCopilotDbContext> dbContextOptions)
    {
        _embeddingGenerator = embeddingGenerator;
        _dbContextOptions = dbContextOptions;
    }

    public async Task IngestPdfAsync(string filePath, string documentName)
    {
        // 1. Extract raw text from PDF using PdfPig
        var rawText = ExtractTextFromPdf(filePath);

        // 2. Chunk text into ~500 token windows with 50 token overlap
        var textChunks = ChunkText(rawText, maxChunkSize: 2000, overlapSize: 200);

        // 3. Persist chunks using EF Core DbContext
        await using var dbContext = new PerceptiveCopilotDbContext(_dbContextOptions);

        foreach (var chunk in textChunks)
        {
            // 4. Generate the 1536-dimensional float vector for this specific chunk
            var embeddingResult = await _embeddingGenerator.GenerateAsync(chunk);
            var vectorArray = embeddingResult.Vector.ToArray();

            var documentChunk = new DocumentChunk
            {
                DocumentName = documentName,
                ContentChunk = chunk,
                Embedding = new SqlVector<float>(vectorArray)
            };

            dbContext.DocumentChunks.Add(documentChunk);
        }

        await dbContext.SaveChangesAsync();
    }

    private string ExtractTextFromPdf(string filePath)
    {
        var sb = new StringBuilder();
        using (var pdf = PdfDocument.Open(filePath))
        {
            foreach (var page in pdf.GetPages())
            {
                sb.AppendLine(page.Text);
            }
        }
        return sb.ToString();
    }

    private List<string> ChunkText(string text, int maxChunkSize, int overlapSize)
    {
        var chunks = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return chunks;

        var wordsPerChunk = maxChunkSize / 5; // Rough estimate of character length to tokens
        var wordsOverlap = overlapSize / 5;

        var words = text.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        for (var i = 0; i < words.Length; i += (wordsPerChunk - wordsOverlap))
        {
            var chunkWords = words.Skip(i).Take(wordsPerChunk);
            var chunk = string.Join(" ", chunkWords);

            if (!string.IsNullOrWhiteSpace(chunk))
            {
                chunks.Add(chunk);
            }

            if (i + wordsPerChunk >= words.Length) break;
        }

        return chunks;
    }
}