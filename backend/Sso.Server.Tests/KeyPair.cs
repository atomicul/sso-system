using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Sso.Server.Tests;

/// <summary>An RSA key written to <c>{name}.pem</c>, with its public key in <c>{name}.pub.pem</c>.</summary>
public class KeyPair
{
    private readonly RSA rsa = RSA.Create(2048);

    public KeyPair(DirectoryInfo directory, string name)
    {
        PrivateKeyPath = Path.Join(directory.FullName, $"{name}.pem");
        PublicKeyPath = Path.Join(directory.FullName, $"{name}.pub.pem");
        File.WriteAllText(PrivateKeyPath, rsa.ExportPkcs8PrivateKeyPem());
        File.WriteAllText(PublicKeyPath, rsa.ExportSubjectPublicKeyInfoPem());
    }

    public string PrivateKeyPath { get; }

    public string PublicKeyPath { get; }

    public RsaSecurityKey SecurityKey => new(rsa);
}
