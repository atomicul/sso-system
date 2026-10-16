using Microsoft.IdentityModel.Tokens;

namespace Sso.Server.Apps;

/// <summary>An app users open from the portal, configured under <c>ClientApps:{Key}</c>.</summary>
/// <param name="Key">Names the app in URLs and its icon file.</param>
/// <param name="Endpoint">Where the app receives the token that logs a user in.</param>
/// <param name="EncryptingCredentials">The app's public key, which only the app can decrypt its tokens with.</param>
public record ClientApp(string Key, string Name, Uri Endpoint, EncryptingCredentials EncryptingCredentials);
