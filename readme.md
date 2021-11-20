# Proof of Concept for an Auth Gateway

_... aka Auth Reverse Proxy ... aka Backend for Frontend (BFF) ..._

## Features

- ☑️ Easily implementing Authentication, Authorization, and SSO for SPAs (e. g. Angular) by making this gateway taking care of the heavy lifting on the serve side
  
- ☑️ Tokens (id_token, access_token, refresh_token) are only stored on server-side in order to increase security

- ☑️ Opaque handling of XSRF tokens. Angular apps will use them automatically.

- ☑️ ``login``, ``logout``, and ``userinfo`` endpoints for SPA

- ☑️ Lots of further features by leveraging Microsoft's YARP Reverse Proxy (e. g. Loading Balancing, Health Checks, Distributed Tracing)

- ☑️ Configuration via ``appsettings.json``
  
## 🔥⚡️ Running the Example

This example shown a reverse proxy orchestrating a SPA with a RESTful API (resource server) and a OAuth2/OIDC authorization server.

1. Get [.NET 6](https://dotnet.microsoft.com/download/dotnet/6.0) for Windows, Linux, or Mac
2. Call ``dotnet restore`` in the project's root to download all libs
3. Call ``dotnet run`` in the project's root to start the reverse proxy
4. Call the Demo App via http://localhost:8080

Also, have a look into the ``appsettings.json``.

## Further Readings

[More on Microsoft's YARP](https://microsoft.github.io/reverse-proxy/articles/getting-started.html) (Yet Another Reverse Proxy).
