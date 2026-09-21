using Microsoft.EntityFrameworkCore;

namespace PerceptiveCopilot.Data
{
    public class PerceptiveCopilotDbContext : DbContext
    {
        public DbSet<DocumentChunk> DocumentChunks { get; set; }
    }
}
