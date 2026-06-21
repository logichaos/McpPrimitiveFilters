using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace ModelContextProtocol.TestOAuthServer;

public class Program
{
  private const int DefaultPort = 7029;

  private readonly TaskCompletionSource _serverStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
  private WebApplication? _app;
  private OAuthServerState? _state;

  public Task ServerStarted => _serverStarted.Task;

  public OAuthServerState State => _state ?? throw new InvalidOperationException("Server not started. Call RunServerAsync first.");

  public static void Main(string[] args)
  {
    var app = BuildApp(args);
    app.Run();
  }

  public async Task RunServerAsync(string[]? args = null, CancellationToken cancellationToken = default,
      Microsoft.AspNetCore.Connections.IConnectionListenerFactory? kestrelTransport = null)
  {
    _app = BuildApp(args, kestrelTransport);
    _state = _app.Services.GetRequiredService<OAuthServerState>();
    await _app.StartAsync(cancellationToken);
    _serverStarted.TrySetResult();

    try
    {
      await Task.Delay(Timeout.Infinite, cancellationToken);
    }
    catch (OperationCanceledException)
    {
    }

    await _app.StopAsync();
  }

  public static WebApplication BuildApp(string[]? args = null,
      Microsoft.AspNetCore.Connections.IConnectionListenerFactory? kestrelTransport = null,
      IConfiguration? configuration = null)
  {
    var builder = WebApplication.CreateBuilder(args ?? []);

    if (configuration is not null)
    {
      builder.Configuration.AddConfiguration(configuration);
    }

    if (kestrelTransport is not null)
    {
      builder.Services.AddSingleton(kestrelTransport);
    }

    builder.Services.AddSingleton<OAuthServerState>();
    builder.Services.AddCors();
    builder.Services.ConfigureHttpJsonOptions(jsonOptions =>
    {
      jsonOptions.SerializerOptions.TypeInfoResolverChain.Add(OAuthJsonContext.Default);
    });
    builder.Logging.AddConsole();

    var port = builder.Configuration.GetValue<int?>("OAuthServer:Port") ?? DefaultPort;
    builder.WebHost.UseKestrel(kestrelOptions =>
    {
      if (port == 0)
      {
        kestrelOptions.Listen(System.Net.IPAddress.Loopback, 0, listenOptions => listenOptions.UseHttps());
        kestrelOptions.Listen(System.Net.IPAddress.IPv6Loopback, 0, listenOptions => listenOptions.UseHttps());
      }
      else
      {
        kestrelOptions.ListenLocalhost(port, listenOptions => listenOptions.UseHttps());
      }
    });

    var app = builder.Build();

    var state = app.Services.GetRequiredService<OAuthServerState>();
    var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                      ?? ["http://localhost:6274", "http://localhost:5173"];

    app.UseCors(policy => policy
        .WithOrigins(corsOrigins)
        .WithMethods("GET", "POST", "OPTIONS")
        .WithHeaders("Content-Type", "Authorization")
        .WithExposedHeaders("WWW-Authenticate"));

    RegisterDemoClients(state, port);
    ConfigurePipeline(app);

    Console.WriteLine($"OAuth Authorization Server running at https://localhost:{port}");
    Console.WriteLine($"OAuth Server Metadata at https://localhost:{port}/.well-known/oauth-authorization-server");
    Console.WriteLine($"JWT keys available at https://localhost:{port}/.well-known/jwks.json");
    Console.WriteLine($"Demo Client ID: demo-client");

    return app;
  }

  private static void RegisterDemoClients(OAuthServerState state, int port)
  {
    var url = $"https://localhost:{port}";
    var clientMetadataDocumentUrl = $"{url}/client-metadata/cimd-client.json";

    state.Clients["demo-client"] = new ClientInfo
    {
      ClientId = "demo-client",
      ClientSecret = "demo-secret",
      RequiresClientSecret = true,
      RedirectUris = ["http://localhost:1179/callback", "http://localhost:6274/oauth/callback", "http://localhost:6274/oauth/callback/debug"],
    };

    state.Clients[clientMetadataDocumentUrl] = new ClientInfo
    {
      ClientId = clientMetadataDocumentUrl,
      RequiresClientSecret = false,
      RedirectUris = ["http://localhost:1179/callback", "http://localhost:6274/oauth/callback"],
    };
  }

  internal static void ConfigurePipeline(WebApplication app)
  {
    static IResult HandleMetadataRequest(HttpContext context, string? issuerPath = null)
    {
      var state = context.RequestServices.GetRequiredService<OAuthServerState>();
      state.MetadataRequests.Enqueue(context.Request.Path);

      if (state.DisabledMetadataPaths.Contains(context.Request.Path))
      {
        return Results.NotFound();
      }

      var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";

      if (!string.IsNullOrEmpty(issuerPath))
      {
        issuerPath = $"/{issuerPath}";
      }

      var metadata = new OAuthServerMetadata
      {
        Issuer = $"{baseUrl}{issuerPath}",
        AuthorizationEndpoint = $"{baseUrl}/authorize",
        TokenEndpoint = $"{baseUrl}/token",
        JwksUri = $"{baseUrl}/.well-known/jwks.json",
        ResponseTypesSupported = ["code"],
        SubjectTypesSupported = ["public"],
        IdTokenSigningAlgValuesSupported = ["RS256"],
        ScopesSupported = state.IncludeOfflineAccessInMetadata
              ? ["openid", "profile", "email", "mcp:tools", "offline_access"]
              : ["openid", "profile", "email", "mcp:tools"],
        TokenEndpointAuthMethodsSupported = ["client_secret_post"],
        ClaimsSupported = ["sub", "iss", "name", "email", "aud"],
        CodeChallengeMethodsSupported = ["S256"],
        GrantTypesSupported = ["authorization_code", "refresh_token"],
        IntrospectionEndpoint = $"{baseUrl}/introspect",
        RegistrationEndpoint = $"{baseUrl}/register",
        ClientIdMetadataDocumentSupported = state.ClientIdMetadataDocumentSupported,
      };

      return Results.Ok(metadata);
    }

    app.MapGet("/.well-known/oauth-authorization-server", HandleMetadataRequest);
    app.MapGet("/.well-known/openid-configuration", HandleMetadataRequest);
    app.MapGet("/.well-known/oauth-authorization-server/{**issuerPath}", HandleMetadataRequest);
    app.MapGet("/.well-known/openid-configuration/{**issuerPath}", HandleMetadataRequest);
    app.MapGet("/{**fullPath}", (HttpContext context, string fullPath) =>
    {
      if (fullPath.EndsWith("/.well-known/openid-configuration", StringComparison.OrdinalIgnoreCase))
      {
        return HandleMetadataRequest(context, fullPath[..^"/.well-known/openid-configuration".Length]);
      }

      return Results.NotFound();
    });

    app.MapGet("/.well-known/jwks.json", (HttpContext context) =>
    {
      var state = context.RequestServices.GetRequiredService<OAuthServerState>();
      var parameters = state.RsaKey.ExportParameters(false);

      var e = WebEncoders.Base64UrlEncode(parameters.Exponent ?? Array.Empty<byte>());
      var n = WebEncoders.Base64UrlEncode(parameters.Modulus ?? Array.Empty<byte>());

      var jwks = new JsonWebKeySet
      {
        Keys = [
                  new JsonWebKey
                    {
                        KeyType = "RSA",
                        Use = "sig",
                        KeyId = state.KeyId,
                        Algorithm = "RS256",
                        Exponent = e,
                        Modulus = n
                    }
              ]
      };

      return Results.Ok(jwks);
    });

    app.MapGet("/authorize", (
        HttpContext context,
        [FromQuery] string client_id,
        [FromQuery] string? redirect_uri,
        [FromQuery] string response_type,
        [FromQuery] string code_challenge,
        [FromQuery] string code_challenge_method,
        [FromQuery] string? scope,
        [FromQuery] string? state_param,
        [FromQuery] string? resource) =>
    {
      var serverState = context.RequestServices.GetRequiredService<OAuthServerState>();

      if (!serverState.Clients.TryGetValue(client_id, out var client))
      {
        return Results.BadRequest(new OAuthErrorResponse
        {
          Error = "invalid_client",
          ErrorDescription = "Client not found"
        });
      }

      if (string.IsNullOrEmpty(redirect_uri))
      {
        if (client.RedirectUris.Count == 1)
        {
          redirect_uri = client.RedirectUris[0];
        }
        else
        {
          return Results.BadRequest(new OAuthErrorResponse
          {
            Error = "invalid_request",
            ErrorDescription = "redirect_uri is required when client has multiple registered URIs"
          });
        }
      }
      else if (!client.RedirectUris.Contains(redirect_uri))
      {
        return Results.BadRequest(new OAuthErrorResponse
        {
          Error = "invalid_request",
          ErrorDescription = "Unregistered redirect_uri"
        });
      }

      if (response_type != "code")
      {
        return Results.Redirect($"{redirect_uri}?error=unsupported_response_type&error_description=Only+code+response_type+is+supported&state={state_param}");
      }

      if (code_challenge_method != "S256")
      {
        return Results.Redirect($"{redirect_uri}?error=invalid_request&error_description=Only+S256+code_challenge_method+is+supported&state={state_param}");
      }

      if (serverState.ExpectResource
              ? (string.IsNullOrEmpty(resource) || !serverState.ValidResources.Any(vr => new Uri(vr).Equals(new Uri(resource))))
              : !string.IsNullOrEmpty(resource))
      {
        return Results.Redirect($"{redirect_uri}?error=invalid_target&error_description=The+specified+resource+is+not+valid&state={state_param}");
      }

      var code = OAuthServerState.GenerateRandomToken();
      var requestedScopes = scope?.Split(' ').ToList() ?? [];

      serverState.AuthCodes[code] = new AuthorizationCodeInfo
      {
        ClientId = client_id,
        RedirectUri = redirect_uri,
        CodeChallenge = code_challenge,
        Scope = requestedScopes,
        Resource = !string.IsNullOrEmpty(resource) ? new Uri(resource) : null
      };

      var redirectUrl = $"{redirect_uri}?code={code}";
      if (!string.IsNullOrEmpty(state_param))
      {
        redirectUrl += $"&state={Uri.EscapeDataString(state_param)}";
      }

      return Results.Redirect(redirectUrl);
    });

    app.MapPost("/token", async (HttpContext context) =>
    {
      var state = context.RequestServices.GetRequiredService<OAuthServerState>();
      var form = await context.Request.ReadFormAsync();

      var client = AuthenticateClient(context, form, state);
      if (client == null)
      {
        context.Response.StatusCode = 401;
        return Results.Problem(
                statusCode: 401,
                title: "Unauthorized",
                detail: "Invalid client credentials",
                type: "https://tools.ietf.org/html/rfc6749#section-5.2");
      }

      var resource = form["resource"].ToString();
      if (state.ExpectResource
              ? (string.IsNullOrEmpty(resource) || !state.ValidResources.Any(vr => new Uri(vr).Equals(new Uri(resource))))
              : !string.IsNullOrEmpty(resource))
      {
        return Results.BadRequest(new OAuthErrorResponse
        {
          Error = "invalid_target",
          ErrorDescription = "The specified resource is not valid."
        });
      }

      var grant_type = form["grant_type"].ToString();
      if (grant_type == "authorization_code")
      {
        var code = form["code"].ToString();
        var code_verifier = form["code_verifier"].ToString();
        var redirect_uri = form["redirect_uri"].ToString();

        if (string.IsNullOrEmpty(code) || !state.AuthCodes.TryRemove(code, out var codeInfo))
        {
          return Results.BadRequest(new OAuthErrorResponse
          {
            Error = "invalid_grant",
            ErrorDescription = "Invalid authorization code"
          });
        }

        if (codeInfo.ClientId != client.ClientId)
        {
          return Results.BadRequest(new OAuthErrorResponse
          {
            Error = "invalid_grant",
            ErrorDescription = "Authorization code was not issued to this client"
          });
        }

        if (!string.IsNullOrEmpty(redirect_uri) && redirect_uri != codeInfo.RedirectUri)
        {
          return Results.BadRequest(new OAuthErrorResponse
          {
            Error = "invalid_grant",
            ErrorDescription = "Redirect URI mismatch"
          });
        }

        if (string.IsNullOrEmpty(code_verifier)
                || !OAuthServerState.VerifyCodeChallenge(code_verifier, codeInfo.CodeChallenge))
        {
          return Results.BadRequest(new OAuthErrorResponse
          {
            Error = "invalid_grant",
            ErrorDescription = "Code verifier does not match the challenge"
          });
        }

        var response = GenerateJwtTokenResponse(client.ClientId, codeInfo.Scope, codeInfo.Resource, state, context);
        return Results.Ok(response);
      }
      else if (grant_type == "refresh_token")
      {
        var refresh_token = form["refresh_token"].ToString();

        if (string.IsNullOrEmpty(refresh_token)
                || !state.Tokens.TryGetValue(refresh_token, out var tokenInfo)
                || tokenInfo.ClientId != client.ClientId)
        {
          return Results.BadRequest(new OAuthErrorResponse
          {
            Error = "invalid_grant",
            ErrorDescription = "Invalid refresh token"
          });
        }

        var response = GenerateJwtTokenResponse(client.ClientId, tokenInfo.Scopes, tokenInfo.Resource, state, context);

        if (!string.IsNullOrEmpty(refresh_token))
        {
          state.Tokens.TryRemove(refresh_token, out _);
        }

        state.HasRefreshedToken = true;
        return Results.Ok(response);
      }
      else
      {
        return Results.BadRequest(new OAuthErrorResponse
        {
          Error = "unsupported_grant_type",
          ErrorDescription = "Unsupported grant type"
        });
      }
    });

    app.MapPost("/introspect", async (HttpContext context) =>
    {
      var state = context.RequestServices.GetRequiredService<OAuthServerState>();
      var form = await context.Request.ReadFormAsync();
      var token = form["token"].ToString();

      if (string.IsNullOrEmpty(token))
      {
        return Results.BadRequest(new OAuthErrorResponse
        {
          Error = "invalid_request",
          ErrorDescription = "Token is required"
        });
      }

      if (state.Tokens.TryGetValue(token, out var tokenInfo))
      {
        if (tokenInfo.ExpiresAt < DateTimeOffset.UtcNow)
        {
          return Results.Ok(new TokenIntrospectionResponse { Active = false });
        }

        return Results.Ok(new TokenIntrospectionResponse
        {
          Active = true,
          ClientId = tokenInfo.ClientId,
          Scope = string.Join(" ", tokenInfo.Scopes),
          ExpirationTime = tokenInfo.ExpiresAt.ToUnixTimeSeconds(),
          Audience = tokenInfo.Resource?.ToString()
        });
      }

      return Results.Ok(new TokenIntrospectionResponse { Active = false });
    });

    app.MapPost("/register", async (HttpContext context) =>
    {
      var state = context.RequestServices.GetRequiredService<OAuthServerState>();
      using var stream = context.Request.Body;
      var registrationRequest = await JsonSerializer.DeserializeAsync(
              stream,
              OAuthJsonContext.Default.ClientRegistrationRequest,
              context.RequestAborted);

      if (registrationRequest is null)
      {
        return Results.BadRequest(new OAuthErrorResponse
        {
          Error = "invalid_request",
          ErrorDescription = "Invalid registration request"
        });
      }

      state.LastRegistrationScope = registrationRequest.Scope;

      if (registrationRequest.RedirectUris.Count == 0)
      {
        return Results.BadRequest(new OAuthErrorResponse
        {
          Error = "invalid_redirect_uri",
          ErrorDescription = "At least one redirect URI must be provided"
        });
      }

      foreach (var redirectUri in registrationRequest.RedirectUris)
      {
        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https"))
        {
          return Results.BadRequest(new OAuthErrorResponse
          {
            Error = "invalid_redirect_uri",
            ErrorDescription = $"Invalid redirect URI: {redirectUri}"
          });
        }
      }

      var clientId = $"dyn-{Guid.NewGuid():N}";
      var clientSecret = OAuthServerState.GenerateRandomToken();
      var issuedAt = DateTimeOffset.UtcNow;

      state.Clients[clientId] = new ClientInfo
      {
        ClientId = clientId,
        RequiresClientSecret = true,
        ClientSecret = clientSecret,
        RedirectUris = registrationRequest.RedirectUris,
      };

      var registrationResponse = new ClientRegistrationResponse
      {
        ClientId = clientId,
        ClientSecret = clientSecret,
        ClientIdIssuedAt = issuedAt.ToUnixTimeSeconds(),
        RedirectUris = registrationRequest.RedirectUris,
        GrantTypes = ["authorization_code", "refresh_token"],
        ResponseTypes = ["code"],
        TokenEndpointAuthMethod = "client_secret_post",
      };

      return Results.Ok(registrationResponse);
    });

    app.MapGet("/", () => "Demo In-Memory OAuth 2.0 Server with JWT Support");
  }

  private static ClientInfo? AuthenticateClient(HttpContext context, IFormCollection form, OAuthServerState state)
  {
    var clientId = form["client_id"].ToString();
    var clientSecret = form["client_secret"].ToString();

    if (string.IsNullOrEmpty(clientId) || !state.Clients.TryGetValue(clientId, out var client))
    {
      return null;
    }

    if (client.RequiresClientSecret && client.ClientSecret != clientSecret)
    {
      return null;
    }

    return client;
  }

  private static TokenResponse GenerateJwtTokenResponse(
      string clientId,
      List<string> scopes,
      Uri? resource,
      OAuthServerState state,
      HttpContext context)
  {
    var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
    var expiresIn = TimeSpan.FromHours(1);
    var issuedAt = DateTimeOffset.UtcNow;
    var expiresAt = issuedAt.Add(expiresIn);
    var jwtId = Guid.NewGuid().ToString();

    var header = new Dictionary<string, string>
        {
            { "alg", "RS256" },
            { "typ", "JWT" },
            { "kid", state.KeyId },
        };

    var payload = new JsonObject
    {
      ["iss"] = baseUrl,
      ["sub"] = $"user-{clientId}",
      ["name"] = $"user-{clientId}",
      ["aud"] = resource?.ToString() ?? clientId,
      ["client_id"] = clientId,
      ["jti"] = jwtId,
      ["iat"] = issuedAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
      ["exp"] = expiresAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
      ["scope"] = new JsonArray([.. scopes.Select(s => JsonValue.Create(s))])
    };

    var headerJson = JsonSerializer.Serialize(header, OAuthJsonContext.Default.DictionaryStringString);
    var payloadJson = payload.ToJsonString();

    var headerBase64 = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
    var payloadBase64 = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

    var dataToSign = $"{headerBase64}.{payloadBase64}";
    var signature = state.RsaKey.SignData(
        Encoding.UTF8.GetBytes(dataToSign),
        HashAlgorithmName.SHA256,
        RSASignaturePadding.Pkcs1);
    var signatureBase64 = WebEncoders.Base64UrlEncode(signature);

    var jwtToken = $"{headerBase64}.{payloadBase64}.{signatureBase64}";

    var refreshToken = OAuthServerState.GenerateRandomToken();

    var tokenInfo = new TokenInfo
    {
      ClientId = clientId,
      Scopes = scopes,
      IssuedAt = issuedAt,
      ExpiresAt = expiresAt,
      Resource = resource,
      JwtId = jwtId
    };

    state.Tokens[refreshToken] = tokenInfo;

    return new TokenResponse
    {
      AccessToken = jwtToken,
      RefreshToken = refreshToken,
      TokenType = "Bearer",
      ExpiresIn = (int)expiresIn.TotalSeconds,
      Scope = string.Join(" ", scopes)
    };
  }
}