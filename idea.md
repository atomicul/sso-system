# SSO Service

This repository contains a generic template for webapps. We will remove dapr and
the todo functionality and restructure it as a portal that supports logging in
and logging out.

We want this service to achieve a SSO experience for proprietary apps. We will
configure this service with a private key for signing JWTs, ClientApps[]:Key (will talk
about this later), and ClientApps[]:Endpoint. We will encrypt the signed JWT with
the client app key, which is a public key and redirect the user to the apps
'Endpoint'.

We will also build a client library that disables the /login and /register routes
and creates a route suitable for the endpoint property in the sso service. The
handler for that route should: 1. Creates/Updates an identity with the values
encoded in the token, this creates a replica of the identity in SSO essentialy.
We accept the fact that the replica may become stale and you would have to log
out in order to see changes to the identity done in the SSO service. 2. Creates
the cookie as usual. 3. Redirects to some route configurable in a parameter,
by default '/'. The SSO private key can also be configured as a parameter to
thus extension method but by default it should be taken from configuration.

This also implies a lot of restructuring in CD. The library should also be built,
not just the web server, though they share a single version number. Ansible should
generate the servers key pair similar to how the PEM certificate is done.

The configured ClientApps should show in the frontend and exposed as a GET /apps,
this also means that besides the Key and Endpoint we will also keep a Name. The
app resource will also have an optional icon url. They should be served from the
file system and the url will show as none if it is not detected. Store them as
{AppKey}.{jpg, png, webp}.

There should also be a imnimal implementation of the client library in this app
used when running make dev. So 3 .NET project, 1 PNPM, and 1 uv
