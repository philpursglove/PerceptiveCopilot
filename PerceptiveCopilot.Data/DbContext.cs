using Microsoft.EntityFrameworkCore;

namespace PerceptiveCopilot.Data
{
    public class PerceptiveCopilotDbContext : DbContext
    {
        public PerceptiveCopilotDbContext(DbContextOptions<PerceptiveCopilotDbContext> options)
            : base(options)
        {
        }

        public DbSet<DocumentChunk> DocumentChunks { get; set; }
    }
}
