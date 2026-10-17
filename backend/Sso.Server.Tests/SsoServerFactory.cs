using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Sso.Server.Data;

[assembly: AssemblyFixture(typeof(Sso.Server.Tests.SsoServerFactory))]

namespace Sso.Server.Tests;

/// <summary>
/// Runs the API against a migrated SQL Server in Docker, shared by every test in the run. Tests stay isolated by
/// registering their own users, since all data belongs to a user. Emails are captured instead of logged.
/// </summary>
public class SsoServerFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string SpaUrl = "https://spa.test";
    public const string AppKey = "alpha";
    public const string OtherAppKey = "beta";
    public const string AppEndpoint = "https://alpha.test/sso?from=portal";

    private readonly MsSqlContainer database = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-latest").Build();
    private readonly DirectoryInfo keyDirectory = Directory.CreateTempSubdirectory();

    public SsoServerFactory()
    {
        SigningKey = new KeyPair(keyDirectory, "sso");
        AppKeyPair = new KeyPair(keyDirectory, AppKey);
        OtherAppKeyPair = new KeyPair(keyDirectory, OtherAppKey);
    }

    public CapturingEmailSender Emails { get; } = new();

    /// <summary>The key the SSO signs its tokens with.</summary>
    public KeyPair SigningKey { get; }

    /// <summary>The key of the <see cref="AppKey"/> app, which its tokens are encrypted with.</summary>
    public KeyPair AppKeyPair { get; }

    public KeyPair OtherAppKeyPair { get; }

    public async ValueTask InitializeAsync()
    {
        await database.StartAsync();
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", database.GetConnectionString());
        UseSettings(builder, new()
        {
            ["SigningKey:Path"] = SigningKey.PrivateKeyPath,
            [$"ClientApps:{AppKey}:Name"] = "Alpha",
            [$"ClientApps:{AppKey}:Endpoint"] = AppEndpoint,
            [$"ClientApps:{AppKey}:PublicKeyPath"] = AppKeyPair.PublicKeyPath,
            [$"ClientApps:{OtherAppKey}:Name"] = "Beta",
            [$"ClientApps:{OtherAppKey}:Endpoint"] = "https://beta.test/sso",
            [$"ClientApps:{OtherAppKey}:PublicKeyPath"] = OtherAppKeyPair.PublicKeyPath,
        });
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SpaUrl"] = SpaUrl,
            ["RateLimits:Api:PermitLimit"] = "100000",
            ["RateLimits:Email:PermitLimit"] = "100000",
        }));
        builder.ConfigureTestServices(services =>
            services.AddKeyedSingleton<IEmailSender<SsoUser>>(SpaLinkEmailSender.TransportKey, Emails));
    }

    /// <summary>
    /// A separate server over the same database with the given settings on top. Each has its own rate limiter, so its
    /// requests don't count against other tests.
    /// </summary>
    public WebApplicationFactory<Program> WithSettings(Dictionary<string, string?> settings) =>
        WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(settings)));

    /// <summary>
    /// Like <see cref="WithSettings"/>, for the settings the API reads while it registers its services, which only
    /// reach it as host settings.
    /// </summary>
    public WebApplicationFactory<Program> WithStartupSettings(Dictionary<string, string> settings) =>
        WithWebHostBuilder(builder => UseSettings(builder, settings));

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await database.DisposeAsync();
        keyDirectory.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    private static void UseSettings(IWebHostBuilder builder, Dictionary<string, string> settings)
    {
        foreach (var (key, value) in settings)
        {
            builder.UseSetting(key, value);
        }
    }
}
