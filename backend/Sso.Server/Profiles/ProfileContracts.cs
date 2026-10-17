using System.ComponentModel.DataAnnotations;
using Sso.Server.Data;

namespace Sso.Server.Profiles;

/// <summary>
/// The user's profile, which the SSO shares with the apps it logs them in to. Every field may be null, and blank ones
/// are saved as null.
/// </summary>
public record Profile(
    [property: MaxLength(100)] string? GivenName,
    [property: MaxLength(100)] string? FamilyName,
    DateOnly? Birthdate,
    [property: Phone, MaxLength(30)] string? PhoneNumber,
    Address Address)
{
    public static Profile Of(SsoUser user) =>
        new(user.GivenName, user.FamilyName, user.Birthdate, user.PhoneNumber, Address.Of(user.Address));

    public void CopyTo(SsoUser user)
    {
        user.GivenName = Clean(GivenName);
        user.FamilyName = Clean(FamilyName);
        user.Birthdate = Birthdate;
        user.PhoneNumber = Clean(PhoneNumber);
        Address.CopyTo(user.Address);
    }

    internal static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>A postal address, in the parts of the OpenID Connect <c>address</c> claim.</summary>
public record Address(
    [property: MaxLength(200)] string? StreetAddress,
    [property: MaxLength(100)] string? Locality,
    [property: MaxLength(100)] string? Region,
    [property: MaxLength(20)] string? PostalCode,
    [property: MaxLength(100)] string? Country)
{
    public static Address Of(PostalAddress address) =>
        new(address.StreetAddress, address.Locality, address.Region, address.PostalCode, address.Country);

    public void CopyTo(PostalAddress address)
    {
        address.StreetAddress = Profile.Clean(StreetAddress);
        address.Locality = Profile.Clean(Locality);
        address.Region = Profile.Clean(Region);
        address.PostalCode = Profile.Clean(PostalCode);
        address.Country = Profile.Clean(Country);
    }
}
