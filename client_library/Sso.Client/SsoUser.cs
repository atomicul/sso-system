using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Sso.Client;

/// <summary>
/// The local copy of a user of the SSO, with the profile its tokens carry; the phone number is Identity's own
/// <see cref="IdentityUser{TKey}.PhoneNumber"/>. Every login replaces the profile with the token's, so a value the user
/// cleared in the SSO is cleared here too.
/// </summary>
public class SsoUser : IdentityUser
{
    public string? GivenName { get; set; }

    public string? FamilyName { get; set; }

    public DateOnly? Birthdate { get; set; }

    public PostalAddress Address { get; set; } = new();
}

/// <summary>A postal address, in the parts of the OpenID Connect <c>address</c> claim.</summary>
[ComplexType]
public class PostalAddress
{
    public string? StreetAddress { get; set; }

    public string? Locality { get; set; }

    public string? Region { get; set; }

    public string? PostalCode { get; set; }

    public string? Country { get; set; }
}
