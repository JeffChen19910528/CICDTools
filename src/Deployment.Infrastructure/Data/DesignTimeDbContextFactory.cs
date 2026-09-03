using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Deployment.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DeploymentDbContext>
{
    public DeploymentDbContext CreateDbContext(string[] args)
    {
        var opt = new DbContextOptionsBuilder<DeploymentDbContext>()
            .UseSqlite("Data Source=design_time.db")
            .Options;
        return new DeploymentDbContext(opt);
    }
}
