using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

[assembly: AssemblyFixture(typeof(Sso.Client.Tests.ClientAppHost))]

namespace Sso.Client.Tests;

/// <summary>
/// A minimal Identity app that logs users in with <see cref="SsoEndpoints.MapSsoIdentityApi{TUser}"/>, reading its
/// keys from settings, against SQL Server in Docker. <c>GET /me</c> answers the signed-in user's name.
/// </summary>
public class ClientAppHost : IAsyncLifetime
{
    public const string RedirectTo = "/home";

    /// <summary>The app's key in the SSO's <c>ClientApps</c>, which its tokens are addressed to.</summary>
    public const string ClientAppKey = "test-app";

    private readonly MsSqlContainer database = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-latest").Build();
    private readonly DirectoryInfo keyDirectory = Directory.CreateTempSubdirectory();
    private WebApplication? app;

    /// <summary>The key the SSO signs its tokens with.</summary>
    public RSA SsoKey { get; } = RSA.Create(2048);

    /// <summary>The app's key, which the SSO encrypts its tokens with.</summary>
    public RSA AppKey { get; } = RSA.Create(2048);

    private WebApplication App => app ?? throw new InvalidOperationException("The host hasn't started.");

    public async ValueTask InitializeAsync()
    {
        await database.StartAsync();
        var ssoPublicKeyPath = Path.Join(keyDirectory.FullName, "sso.pub.pem");
        var appPrivateKeyPath = Path.Join(keyDirectory.FullName, "app.pem");
        await File.WriteAllTextAsync(ssoPublicKeyPath, SsoKey.ExportSubjectPublicKeyInfoPem());
        await File.WriteAllTextAsync(appPrivateKeyPath, AppKey.ExportPkcs8PrivateKeyPem());

        app = CreateApp(new()
        {
            ["Sso:PublicKeyPath"] = ssoPublicKeyPath,
            ["Sso:AppPrivateKeyPath"] = appPrivateKeyPath,
            ["Sso:AppKey"] = ClientAppKey,
        });
        app.MapGroup("/auth").MapSsoIdentityApi<SsoUser>(redirectTo: RedirectTo);
        app.MapGet("/me", (ClaimsPrincipal user) => user.Identity!.Name).RequireAuthorization();
        await app.StartAsync();

        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<UsersDbContext>().Database.EnsureCreatedAsync();
    }

    /// <summary>An Identity app over the same database, with the given settings, that hasn't mapped any endpoint.</summary>
    public WebApplication CreateApp(Dictionary<string, string?> settings)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(settings);
        builder.Services.AddDbContext<UsersDbContext>(options => options.UseSqlServer(database.GetConnectionString()));
        builder.Services.AddAuthorization();
        builder.Services.AddIdentityApiEndpoints<SsoUser>().AddEntityFrameworkStores<UsersDbContext>();
        return builder.Build();
    }

    /// <summary>A client that keeps the cookies the app sets and doesn't follow redirects.</summary>
    public HttpClient CreateClient() =>
        new(new CookieContainerHandler { InnerHandler = App.GetTestServer().CreateHandler() })
        {
            BaseAddress = App.GetTestServer().BaseAddress,
        };

    public async Task<SsoUser?> FindUserAsync(string id)
    {
        using var scope = App.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<UserManager<SsoUser>>().FindByIdAsync(id);
    }

    public async ValueTask DisposeAsync()
    {
        if (app is not null)
        {
            await app.DisposeAsync();
        }
        await database.DisposeAsync();
        keyDirectory.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    private sealed class UsersDbContext(DbContextOptions<UsersDbContext> options) : IdentityDbContext<SsoUser>(options);
}
