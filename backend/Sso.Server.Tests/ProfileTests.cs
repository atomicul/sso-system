using System.Net;
using System.Net.Http.Json;
using Sso.Server.Profiles;

namespace Sso.Server.Tests;

public class ProfileTests(SsoServerFactory factory)
{
    private static readonly Profile Ada = new(
        "Ada", "Lovelace", new DateOnly(1815, 12, 10), "+44 20 7946 0000",
        new Address("12 St James's Square", "London", null, "SW1Y 4JH", "United Kingdom"));

    private static readonly Address NoAddress = new(null, null, null, null, null);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Anonymous_user_cannot_read_a_profile()
    {
        var response = await factory.CreateClient().GetAsync("/api/profile", CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task New_user_has_an_empty_profile()
    {
        var client = await factory.CreateUserClientAsync();

        var profile = await client.GetFromJsonAsync<Profile>("/api/profile", CancellationToken);

        Assert.Equal(new Profile(null, null, null, null, NoAddress), profile);
    }

    [Fact]
    public async Task Saved_profile_is_read_back()
    {
        var client = await factory.CreateUserClientAsync();

        var saved = await client.PutAsJsonAsync("/api/profile", Ada, CancellationToken);
        var profile = await client.GetFromJsonAsync<Profile>("/api/profile", CancellationToken);

        Assert.Equal(Ada, await saved.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<Profile>(CancellationToken));
        Assert.Equal(Ada, profile);
    }

    [Fact]
    public async Task Blank_values_are_saved_as_null_and_others_trimmed()
    {
        var client = await factory.CreateUserClientAsync();
        await client.PutAsJsonAsync("/api/profile", Ada, CancellationToken);

        await client.PutAsJsonAsync("/api/profile",
            Ada with { GivenName = "  Ada  ", FamilyName = " ", Address = NoAddress with { Locality = "" } },
            CancellationToken);
        var profile = await client.GetFromJsonAsync<Profile>("/api/profile", CancellationToken);

        Assert.Equal(Ada with { GivenName = "Ada", FamilyName = null, Address = NoAddress }, profile);
    }

    [Theory]
    [InlineData("GivenName", "#/givenName", "MaxLength")]
    [InlineData("Address.PostalCode", "#/address/postalCode", "MaxLength")]
    [InlineData("PhoneNumber", "#/phoneNumber", "Phone")]
    public async Task Invalid_profile_is_rejected(string field, string pointer, string code)
    {
        var client = await factory.CreateUserClientAsync();
        var profile = field switch
        {
            "GivenName" => Ada with { GivenName = new string('a', 101) },
            "Address.PostalCode" => Ada with { Address = Ada.Address with { PostalCode = new string('1', 21) } },
            _ => Ada with { PhoneNumber = "not a phone" },
        };

        var response = await client.PutAsJsonAsync("/api/profile", profile, CancellationToken);
        var (problem, issues) = await response.ReadValidationProblemAsync();

        Assert.Contains(field, problem.Errors.Keys);
        Assert.Contains(issues, issue => issue.Pointer == pointer && issue.Code == code);
    }
}
