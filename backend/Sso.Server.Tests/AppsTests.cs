using System.Net;
using System.Net.Http.Json;
using System.Web;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Sso.Server.Apps;
using Sso.Server.Profiles;

namespace Sso.Server.Tests;

public class AppsTests(SsoServerFactory factory) : IDisposable
{
    private static readonly byte[] Icon = [0x89, 0x50, 0x4E, 0x47];

    private readonly DirectoryInfo iconDirectory = Directory.CreateTempSubdirectory();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Anonymous_user_cannot_list_apps()
    {
        var response = await factory.CreateClient().GetAsync("/api/apps", CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Lists_the_configured_apps_without_icons()
    {
        var client = await factory.CreateUserClientAsync();

        var apps = await client.GetFromJsonAsync<AppResponse[]>("/api/apps", CancellationToken);

        Assert.Equal(
            [new AppResponse(SsoServerFactory.AppKey, "Alpha", null), new AppResponse(SsoServerFactory.OtherAppKey, "Beta", null)],
            apps!);
    }

    [Fact]
    public async Task Lists_and_serves_the_icons_in_the_icon_directory()
    {
        await File.WriteAllBytesAsync(Path.Join(iconDirectory.FullName, $"{SsoServerFactory.AppKey}.png"), Icon, CancellationToken);
        var client = ServingIcons().CreateClient();
        await client.AuthorizeAsNewUserAsync(factory);

        var apps = await client.GetFromJsonAsync<AppResponse[]>("/api/apps", CancellationToken);
        var icon = await client.GetAsync(apps![0].IconUrl, CancellationToken);

        Assert.Equal($"/api/apps/{SsoServerFactory.AppKey}/icon", apps[0].IconUrl);
        Assert.Null(apps[1].IconUrl);
        Assert.Equal("image/png", icon.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Icon, await icon.Content.ReadAsByteArrayAsync(CancellationToken));
    }

    [Fact]
    public async Task Missing_icon_is_not_found()
    {
        var client = ServingIcons().CreateClient();
        await client.AuthorizeAsNewUserAsync(factory);

        var response = await client.GetAsync($"/api/apps/{SsoServerFactory.OtherAppKey}/icon", CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Launch_redirects_to_the_app_with_a_token_signed_by_the_sso_for_the_app()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var email = await client.AuthorizeAsNewUserAsync(factory);

        var response = await client.GetAsync($"/api/apps/{SsoServerFactory.AppKey}/launch", CancellationToken);
        var token = await ReadTokenAsync(response, factory.AppKeyPair);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith($"{SsoServerFactory.AppEndpoint}&token=", response.Headers.Location!.AbsoluteUri);
        Assert.True(token.IsValid);
        Assert.Equal(email, token.Claims[JwtRegisteredClaimNames.Email]);
        Assert.False(string.IsNullOrEmpty((string)token.Claims[JwtRegisteredClaimNames.Sub]));
        Assert.InRange(token.SecurityToken.ValidTo, DateTime.UtcNow, DateTime.UtcNow + factory.Services.GetRequiredService<LaunchTokens>().Lifetime);
    }

    [Fact]
    public async Task Launch_token_carries_the_users_profile()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await client.AuthorizeAsNewUserAsync(factory);
        var profile = new Profile("Ada", "Lovelace", new DateOnly(1815, 12, 10), "+44 20 7946 0000",
            new Address("12 St James's Square", "London", null, "SW1Y 4JH", "United Kingdom"));
        (await client.PutAsJsonAsync("/api/profile", profile, CancellationToken)).EnsureSuccessStatusCode();

        var response = await client.GetAsync($"/api/apps/{SsoServerFactory.AppKey}/launch", CancellationToken);
        var token = (JsonWebToken)(await ReadTokenAsync(response, factory.AppKeyPair)).SecurityToken;

        Assert.Equal("Ada", token.GetClaim(JwtRegisteredClaimNames.GivenName).Value);
        Assert.Equal("Lovelace", token.GetClaim(JwtRegisteredClaimNames.FamilyName).Value);
        Assert.Equal("1815-12-10", token.GetClaim(JwtRegisteredClaimNames.Birthdate).Value);
        Assert.Equal("+44 20 7946 0000", token.GetClaim(JwtRegisteredClaimNames.PhoneNumber).Value);
        Assert.Equal(
            """{"street_address":"12 St James's Square","locality":"London","postal_code":"SW1Y 4JH","country":"United Kingdom"}""",
            token.GetClaim(JwtRegisteredClaimNames.Address).Value);
    }

    [Fact]
    public async Task Launch_token_leaves_out_the_profile_the_user_has_not_set()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await client.AuthorizeAsNewUserAsync(factory);

        var response = await client.GetAsync($"/api/apps/{SsoServerFactory.AppKey}/launch", CancellationToken);
        var token = await ReadTokenAsync(response, factory.AppKeyPair);

        Assert.Equal(
            [JwtRegisteredClaimNames.Aud, JwtRegisteredClaimNames.Email, JwtRegisteredClaimNames.Exp, JwtRegisteredClaimNames.Iat,
                JwtRegisteredClaimNames.Jti, JwtRegisteredClaimNames.Nbf, JwtRegisteredClaimNames.Sub],
            token.Claims.Keys.Order());
    }

    [Fact]
    public async Task Launch_numbers_its_tokens_in_increasing_order()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await client.AuthorizeAsNewUserAsync(factory);

        var first = await LaunchNumberAsync(client);
        var second = await LaunchNumberAsync(client);

        Assert.True(second > first);
    }

    [Fact]
    public async Task Token_cannot_be_read_by_another_app()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await client.AuthorizeAsNewUserAsync(factory);

        var response = await client.GetAsync($"/api/apps/{SsoServerFactory.OtherAppKey}/launch", CancellationToken);
        var token = await ReadTokenAsync(response, factory.AppKeyPair);

        Assert.False(token.IsValid);
    }

    [Fact]
    public async Task Launching_an_unknown_app_is_not_found()
    {
        var client = await factory.CreateUserClientAsync();

        var response = await client.GetAsync("/api/apps/unknown/launch", CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_user_cannot_launch_an_app()
    {
        var response = await factory.CreateClient().GetAsync($"/api/apps/{SsoServerFactory.AppKey}/launch", CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("SigningKey:Path", "")]
    [InlineData($"ClientApps:{SsoServerFactory.AppKey}:Endpoint", "")]
    [InlineData("ClientApps:Not_A_Key:Name", "Invalid")]
    public void Invalid_settings_stop_the_app_from_starting(string key, string value)
    {
        var server = factory.WithStartupSettings(new() { [key] = value });

        Assert.Throws<InvalidOperationException>(() => server.CreateClient());
    }

    [Fact]
    public void Apps_sharing_a_public_key_stop_the_app_from_starting()
    {
        var server = factory.WithStartupSettings(new()
        {
            [$"ClientApps:{SsoServerFactory.OtherAppKey}:PublicKeyPath"] = factory.AppKeyPair.PublicKeyPath,
        });

        var error = Assert.Throws<InvalidOperationException>(() => server.CreateClient());
        Assert.Contains("share a public key", error.Message);
    }

    public void Dispose()
    {
        iconDirectory.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    private WebApplicationFactory<Program> ServingIcons() =>
        factory.WithStartupSettings(new() { ["AppIcons:Directory"] = iconDirectory.FullName });

    private async Task<long> LaunchNumberAsync(HttpClient client)
    {
        var response = await client.GetAsync($"/api/apps/{SsoServerFactory.AppKey}/launch", CancellationToken);
        var token = await ReadTokenAsync(response, factory.AppKeyPair);
        return long.Parse((string)token.Claims[JwtRegisteredClaimNames.Jti]);
    }

    /// <summary>Decrypts the token in the redirect with the app's key, as the app would, and checks the SSO signed it.</summary>
    private Task<TokenValidationResult> ReadTokenAsync(HttpResponseMessage redirect, KeyPair appKeyPair)
    {
        var token = HttpUtility.ParseQueryString(redirect.Headers.Location!.Query)["token"];
        return new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            IssuerSigningKey = factory.SigningKey.SecurityKey,
            TokenDecryptionKey = appKeyPair.SecurityKey,
            ValidateIssuer = false,
            ValidateAudience = false,
        });
    }
}
