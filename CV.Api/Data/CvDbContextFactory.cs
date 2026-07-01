using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CV.Api.Data;

/// <summary>
/// Used only by the EF Core tooling (dotnet ef migrations / database update).
/// Targets the SQL Server provider with a placeholder connection string so the
/// schema can be scaffolded without a live database. The runtime connection
/// string comes from configuration / environment variables in Program.cs.
/// </summary>
public class CvDbContextFactory : IDesignTimeDbContextFactory<CvDbContext>
{
    public CvDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CvDbContext>()
            .UseSqlServer("Server=(design-time);Database=Cv;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;

        return new CvDbContext(options);
    }
}
