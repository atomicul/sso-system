using System.Net;
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Sso.Client.Tests;

public class SsoCallbackTests(ClientAppHost host)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task First_login_creates_the_user_signs_them_in_and_redirects()
    {
        var client = host.CreateClient();
        var (userId, email) = NewUser();

        var response = await LogInAsync(client, userId, email, number: 1);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(ClientAppHost.RedirectTo, response.Headers.Location?.OriginalString);
        Assert.Equal(email, await client.GetStringAsync("/me", CancellationToken));
        var user = await host.FindUserAsync(userId);
        Assert.Equal(email, user?.Email);
        Assert.True(user?.EmailConfirmed);
    }

    [Fact]
    public async Task Next_login_updates_the_users_email()
    {
        var (userId, email) = NewUser();
        var newEmail = $"new-{email}";
        await LogInAsync(host.CreateClient(), userId, email, number: 1);

        var response = await LogInAsync(host.CreateClient(), userId, newEmail, number: 2);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(newEmail, (await host.FindUserAsync(userId))?.Email);
    }

    [Fact]
    public async Task Login_copies_the_profile_in_the_token()
    {
        var (userId, email) = NewUser();

        await LogInAsync(host.CreateClient(), userId, email, number: 1, Ada);

        var user = await host.FindUserAsync(userId);
        Assert.Equal("Ada", user?.GivenName);
        Assert.Equal("Lovelace", user?.FamilyName);
        Assert.Equal(new DateOnly(1815, 12, 10), user?.Birthdate);
        Assert.Equal("+44 20 7946 0000", user?.PhoneNumber);
        Assert.Equal("12 St James's Square", user?.Address.StreetAddress);
        Assert.Equal("London", user?.Address.Locality);
        Assert.Null(user?.Address.Region);
        Assert.Equal("SW1Y 4JH", user?.Address.PostalCode);
        Assert.Equal("United Kingdom", user?.Address.Country);
    }

    [Fact]
    public async Task Next_login_clears_the_profile_left_out_of_the_token()
    {
        var (userId, email) = NewUser();
        await LogInAsync(host.CreateClient(), userId, email, number: 1, Ada);

        await LogInAsync(host.CreateClient(), userId, email, number: 2,
            new() { [JwtRegisteredClaimNames.GivenName] = "Ada" });

        var user = await host.FindUserAsync(userId);
        Assert.Equal("Ada", user?.GivenName);
        Assert.Null(user?.FamilyName);
        Assert.Null(user?.Birthdate);
        Assert.Null(user?.PhoneNumber);
        Assert.Null(user?.Address.StreetAddress);
        Assert.Null(user?.Address.Country);
    }

    [Fact]
    public async Task Token_logs_in_once()
    {
        var (userId, email) = NewUser();
        var token = SsoTokens.Write(host.SsoKey, host.AppKey, userId, email, number: 1);
        await host.CreateClient().GetAsync(SsoTokens.CallbackUrl(token), CancellationToken);

        var replay = await host.CreateClient().GetAsync(SsoTokens.CallbackUrl(token), CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
    }

    [Fact]
    public async Task Token_older_than_the_last_login_is_rejected()
    {
        var (userId, email) = NewUser();
        await LogInAsync(host.CreateClient(), userId, email, number: 5);

        var response = await LogInAsync(host.CreateClient(), userId, email, number: 4);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Concurrent_logins_with_the_same_token_let_one_in()
    {
        var (userId, email) = NewUser();
        await LogInAsync(host.CreateClient(), userId, email, number: 1);
        var url = SsoTokens.CallbackUrl(SsoTokens.Write(host.SsoKey, host.AppKey, userId, email, number: 2));

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 5).Select(_ => host.CreateClient().GetAsync(url, CancellationToken)));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Token_signed_by_another_key_is_rejected()
    {
        var (userId, email) = NewUser();
        using var otherKey = RSA.Create(2048);
        var token = SsoTokens.Write(otherKey, host.AppKey, userId, email, number: 1);

        var response = await host.CreateClient().GetAsync(SsoTokens.CallbackUrl(token), CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(await host.FindUserAsync(userId));
    }

    [Fact]
    public async Task Token_for_another_app_is_rejected()
    {
        var (userId, email) = NewUser();
        using var otherAppKey = RSA.Create(2048);
        var token = SsoTokens.Write(host.SsoKey, otherAppKey, userId, email, number: 1);

        var response = await host.CreateClient().GetAsync(SsoTokens.CallbackUrl(token), CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Token_addressed_to_another_app_is_rejected_even_when_encrypted_for_this_one()
    {
        var (userId, email) = NewUser();
        var token = SsoTokens.Write(host.SsoKey, host.AppKey, userId, email, number: 1, audience: "other-app");

        var response = await host.CreateClient().GetAsync(SsoTokens.CallbackUrl(token), CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(await host.FindUserAsync(userId));
    }

    [Fact]
    public async Task Expired_token_is_rejected()
    {
        var (userId, email) = NewUser();
        var token = SsoTokens.Write(host.SsoKey, host.AppKey, userId, email, number: 1, expires: DateTime.UtcNow.AddMinutes(-1));

        var response = await host.CreateClient().GetAsync(SsoTokens.CallbackUrl(token), CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/login")]
    [InlineData("POST", "/register")]
    [InlineData("POST", "/refresh")]
    [InlineData("GET", "/confirmEmail")]
    [InlineData("POST", "/resendConfirmationEmail")]
    [InlineData("POST", "/forgotPassword")]
    [InlineData("POST", "/resetPassword")]
    [InlineData("POST", "/manage/info")]
    [InlineData("POST", "/manage/2fa")]
    public async Task Local_credential_endpoints_are_not_found(string method, string route)
    {
        var client = host.CreateClient();
        var (userId, email) = NewUser();
        await LogInAsync(client, userId, email, number: 1);

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), $"/auth{route}"), CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Session_endpoints_still_answer()
    {
        var client = host.CreateClient();
        var (userId, email) = NewUser();
        await LogInAsync(client, userId, email, number: 1);

        var response = await client.GetAsync("/auth/manage/info", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Mapping_without_the_key_settings_throws()
    {
        await using var app = host.CreateApp([]);

        Assert.Throws<InvalidOperationException>(() => app.MapSsoIdentityApi<SsoUser>());
    }

    [Fact]
    public async Task Mapping_with_given_keys_needs_no_settings()
    {
        await using var app = host.CreateApp([]);

        app.MapSsoIdentityApi<SsoUser>(
            ssoPublicKey: new Microsoft.IdentityModel.Tokens.RsaSecurityKey(host.SsoKey),
            appPrivateKey: new Microsoft.IdentityModel.Tokens.RsaSecurityKey(host.AppKey),
            appKey: ClientAppHost.ClientAppKey);
    }

    private static readonly Dictionary<string, object> Ada = new()
    {
        [JwtRegisteredClaimNames.GivenName] = "Ada",
        [JwtRegisteredClaimNames.FamilyName] = "Lovelace",
        [JwtRegisteredClaimNames.Birthdate] = "1815-12-10",
        [JwtRegisteredClaimNames.PhoneNumber] = "+44 20 7946 0000",
        [JwtRegisteredClaimNames.Address] = new Dictionary<string, object>
        {
            ["street_address"] = "12 St James's Square",
            ["locality"] = "London",
            ["postal_code"] = "SW1Y 4JH",
            ["country"] = "United Kingdom",
        },
    };

    private static (string UserId, string Email) NewUser() => (Guid.NewGuid().ToString(), $"{Guid.NewGuid()}@test.local");

    private Task<HttpResponseMessage> LogInAsync(
        HttpClient client, string userId, string email, long number, Dictionary<string, object>? profile = null) =>
        client.GetAsync(
            SsoTokens.CallbackUrl(SsoTokens.Write(host.SsoKey, host.AppKey, userId, email, number, profile: profile)),
            CancellationToken);
}
