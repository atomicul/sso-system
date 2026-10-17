using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Sso.Server.Data;

/// <summary>Identity store, the Data Protection key ring, and the numbers of the tokens that log users in to apps.</summary>
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<SsoUser>(options), IDataProtectionKeyContext
{
    private const string LaunchTokenNumbers = nameof(LaunchTokenNumbers);
    private const string NextLaunchTokenNumberSql = "SELECT NEXT VALUE FOR " + LaunchTokenNumbers + " AS [Value]";

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    /// <summary>
    /// The next launch token number. Numbers only grow, so a client app that remembers the last one it accepted
    /// rejects any token it sees again. SQL Server refuses <c>NEXT VALUE FOR</c> under <c>TOP</c>, so the query is
    /// read as a list rather than composed with <c>Single</c>.
    /// </summary>
    public async Task<long> NextLaunchTokenNumberAsync(CancellationToken cancellationToken = default)
    {
        var numbers = await Database
            .SqlQueryRaw<long>(NextLaunchTokenNumberSql)
            .ToListAsync(cancellationToken);
        return numbers.Single();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasSequence<long>(LaunchTokenNumbers);
    }
}
