using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Sso.Server.Data;

/// <summary>A user of the SSO, with the profile it shares with the apps it logs them in to.</summary>
public class SsoUser : IdentityUser
{
    [MaxLength(100)]
    public string? GivenName { get; set; }

    [MaxLength(100)]
    public string? FamilyName { get; set; }

    public DateOnly? Birthdate { get; set; }

    public PostalAddress Address { get; set; } = new();
}

/// <summary>A postal address, split into the parts of the OpenID Connect <c>address</c> claim.</summary>
[ComplexType]
public class PostalAddress
{
    [MaxLength(200)]
    public string? StreetAddress { get; set; }

    [MaxLength(100)]
    public string? Locality { get; set; }

    [MaxLength(100)]
    public string? Region { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }
}
