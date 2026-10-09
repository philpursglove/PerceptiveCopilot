using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlTypes;
using Microsoft.Extensions.AI;
using System.Text;
using UglyToad.PdfPig;

public class PdfIngestionService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly string _connectionString;

    public PdfIngestionService(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator, string connectionString)
    {
        _embeddingGenerator = embeddingGenerator;
        _connectionString = connectionString;
    }

    public async Task IngestPdfAsync(string filePath, string documentName)
    {
        // 1. Extract raw text from PDF using PdfPig
        string rawText = ExtractTextFromPdf(filePath);

        // 2. Chunk text into ~500 token windows with 50 token overlap
        List<string> textChunks = ChunkText(rawText, maxChunkSize: 2000, overlapSize: 200);

        // 3. Connect to Azure SQL Database
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        string insertQuery = @"
            INSERT INTO DocumentChunks (DocumentName, ContentChunk, Embedding) 
            VALUES (@DocumentName, @ContentChunk, @Embedding);";

        foreach (var chunk in textChunks)
        {
            // 4. Generate the 1536-dimensional float vector for this specific chunk
            var embeddingResult = await _embeddingGenerator.GenerateAsync(chunk);
            float[] vectorArray = embeddingResult.First().Vector.ToArray();

            using var command = new SqlCommand(insertQuery, connection);
            command.Parameters.AddWithValue("@DocumentName", documentName);
            command.Parameters.AddWithValue("@ContentChunk", chunk);

            // Pass the vector array using Azure SQL's native binary SqlVector type
            var vectorParam = new SqlParameter("@Embedding", System.Data.SqlDbType.VarBinary)
            {
                Value = new SqlVector<float>(vectorArray)
            };
            command.Parameters.Add(vectorParam);

            await command.ExecuteNonQueryAsync();
        }
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

        int wordsPerChunk = maxChunkSize / 5; // Rough estimate of character length to tokens
        int wordsOverlap = overlapSize / 5;

        string[] words = text.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < words.Length; i += (wordsPerChunk - wordsOverlap))
        {
            var chunkWords = words.Skip(i).Take(wordsPerChunk);
            string chunk = string.Join(" ", chunkWords);

            if (!string.IsNullOrWhiteSpace(chunk))
            {
                chunks.Add(chunk);
            }

            if (i + wordsPerChunk >= words.Length) break;
        }

        return chunks;
    }
}