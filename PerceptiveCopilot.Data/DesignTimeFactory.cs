using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PerceptiveCopilot.Data;

public class PerceptiveCopilotDbContextFactory : IDesignTimeDbContextFactory<PerceptiveCopilotDbContext>
{
    public PerceptiveCopilotDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PerceptiveCopilotDbContext>();
        optionsBuilder.UseSqlServer();
        
        return new PerceptiveCopilotDbContext(optionsBuilder.Options);
    }
}