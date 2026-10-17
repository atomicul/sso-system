using System.Globalization;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Sso.Client;

/// <summary>
/// Copies the profile of an SSO token, in OpenID Connect claims, onto the user: <c>given_name</c>, <c>family_name</c>,
/// <c>birthdate</c> (<c>YYYY-MM-DD</c>), <c>phone_number</c>, and <c>address</c>, an object of <c>street_address</c>,
/// <c>locality</c>, <c>region</c>, <c>postal_code</c> and <c>country</c>. The SSO leaves out the values the user
/// hasn't set, so a missing claim clears its value.
/// </summary>
internal static class ProfileClaims
{
    public static void CopyTo(JsonWebToken token, SsoUser user)
    {
        user.GivenName = Read(token, JwtRegisteredClaimNames.GivenName);
        user.FamilyName = Read(token, JwtRegisteredClaimNames.FamilyName);
        user.Birthdate = ReadDate(token, JwtRegisteredClaimNames.Birthdate);
        user.PhoneNumber = Read(token, JwtRegisteredClaimNames.PhoneNumber);

        using var address = ReadObject(token, JwtRegisteredClaimNames.Address);
        user.Address.StreetAddress = ReadPart(address, "street_address");
        user.Address.Locality = ReadPart(address, "locality");
        user.Address.Region = ReadPart(address, "region");
        user.Address.PostalCode = ReadPart(address, "postal_code");
        user.Address.Country = ReadPart(address, "country");
    }

    private static string? Read(JsonWebToken token, string name) =>
        token.TryGetClaim(name, out var claim) ? claim.Value : null;

    private static DateOnly? ReadDate(JsonWebToken token, string name) =>
        DateOnly.TryParseExact(Read(token, name), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;

    private static JsonDocument? ReadObject(JsonWebToken token, string name) =>
        Read(token, name) is { } json ? JsonDocument.Parse(json) : null;

    private static string? ReadPart(JsonDocument? address, string name) =>
        address?.RootElement is { ValueKind: JsonValueKind.Object } parts
        && parts.TryGetProperty(name, out var part)
        && part.ValueKind == JsonValueKind.String
            ? part.GetString()
            : null;
}
