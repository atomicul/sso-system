# Sso.Client

Logs users in to an ASP.NET Core Identity app with the tokens of the SSO portal, in place of the app's own login and
registration.

```csharp
builder.Services.AddIdentityApiEndpoints<SsoUser>()
    .AddEntityFrameworkStores<AppDbContext>();

app.MapGroup("/api/auth").MapSsoIdentityApi<SsoUser>();
```

The app's users are `SsoUser`s, or a class derived from it: an `IdentityUser` with the profile the SSO keeps.
`GivenName`, `FamilyName`, `Birthdate` and `Address` (a complex type of `StreetAddress`, `Locality`, `Region`,
`PostalCode` and `Country`) add columns to the app's users table, so add a migration after switching to it.

`MapSsoIdentityApi` maps Identity's endpoints and `GET /api/auth/sso?token=`, the URL to configure as the app's
`Endpoint` in the SSO. Opening the app from the portal sends the user there with a token, and the endpoint:

1. Checks the token: the SSO signed it, it was encrypted with the app's key, it is addressed to this app, it hasn't
   expired, and it is newer than the last token the user logged in with, so each token logs in once.
2. Creates or updates the user's local copy with the id, email and profile in the token, replacing the whole
   profile, so values the user cleared in the SSO are cleared too. The copy only changes on the next login, so changes
   to the user in the SSO show after logging out and in again.
3. Signs the user in with Identity's cookie and redirects them to `/`.

The endpoints that log in with or change local credentials (`/login`, `/register`, `/refresh`, `/confirmEmail`,
`/resendConfirmationEmail`, `/forgotPassword`, `/resetPassword`, `POST /manage/info` and `/manage/2fa`) answer
`404 Not Found`. Requests that need a session answer `401 Unauthorized` without one: users start from the portal.

## Settings

| Setting | Holds |
|---|---|
| `Sso:PublicKeyPath` | The SSO's PEM public key, which its tokens are signed with |
| `Sso:AppPrivateKeyPath` | The app's PEM private key, whose public key the SSO encrypts the app's tokens with |
| `Sso:AppKey` | The app's key in the SSO's `ClientApps`, e.g. `billing`, which the SSO addresses the app's tokens to |

The app accepts only tokens addressed to its key. Encryption alone doesn't keep other apps' tokens out: an app can
decrypt its own token and encrypt it again with this app's public key.

Pass `ssoPublicKey`, `appPrivateKey` and `appKey` to `MapSsoIdentityApi` to give them some other way,
`callbackPattern` to change the endpoint's route, and `redirectTo` to send users elsewhere after logging in. It throws
when one of them is missing, or when the user store doesn't keep authentication tokens, as Identity's Entity Framework
store does in its `AspNetUserTokens` table.

The user's last token number is kept there as the `LastTokenNumber` token of the `Sso` login provider. If the SSO's
database is ever restored from a backup, its numbers start over below the ones apps remember, and apps reject the
user's logins until they catch up; deleting those rows lets users in again.
