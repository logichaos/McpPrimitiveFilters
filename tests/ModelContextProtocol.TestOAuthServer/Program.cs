using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ModelContextProtocol.TestOAuthServer;

public sealed class Program
{
    private readonly int _port;
    private readonly string _url;
    private readonly string _clientMetadataDocumentUrl;
    private readonly bool _useHttps;

    public Program(int port = 7029, bool useHttps = true, ILoggerProvider? loggerProvider = null, IConnectionListenerFactory? kestrelTransport = null)
    {
        _port = port;
        _useHttps = useHttps;
        var scheme = useHttps ? "https" : "http";
        _url = $"{scheme}://localhost:{_port}";
        _clientMetadataDocumentUrl = $"{_url}/client-metadata/cimd-client.json";
        _rsa = RSA.Create(2048);
        _keyId = Guid.NewGuid().ToString();
        _loggerProvider = loggerProvider;
        _kestrelTransport = kestrelTransport;
    }

    // Valid resource URLs include the AuthenticatedHttpMcpServer.
    // Per MCP spec, URIs should not have trailing slashes unless semantically significant.
    public string[] ValidResources { get; set; } = [
        "http://localhost:5105",
        "http://localhost:5105/mcp",
        "https://localhost:7093",
        "https://localhost:7093/mcp",
    ];

    private readonly ConcurrentDictionary<string, AuthorizationCodeInfo> _authCodes = new();
    private readonly ConcurrentDictionary<string, TokenInfo> _tokens = new();
    private readonly ConcurrentDictionary<string, ClientInfo> _clients = new();

    // ── SCOPE EXPANSION: Maps client_id → (user_name, roles, allowed_tools) ──
    private static readonly Dictionary<string, (string UserName, string Roles, string[] AllowedTools)> _userPermissions = new()
    {
        ["alice-client"] = (
            UserName: "Alice",
            Roles: "mcpcaller awesome",
            AllowedTools: ["hello_world", "random_number"]
        ),
        ["bob-client"] = (
            UserName: "Bob (Admin)",
            Roles: "mcpcaller awesome",
            AllowedTools: ["hello_world", "random_number"]
        ),
        ["demo-client"] = (
            UserName: "Demo User",
            Roles: "mcpcaller",
            AllowedTools: ["hello_world"]
        ),
    };

    private readonly ConcurrentQueue<string> _metadataRequests = new();

    private readonly RSA _rsa;
    private readonly string _keyId;

    private readonly ILoggerProvider? _loggerProvider;
    private readonly IConnectionListenerFactory? _kestrelTransport;
    private readonly TaskCompletionSource _serverStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task ServerStarted => _serverStarted.Task;

    public bool HasRefreshedToken { get; set; }

    public bool ClientIdMetadataDocumentSupported { get; set; } = true;

    public bool ExpectResource { get; set; } = true;

    public bool IncludeOfflineAccessInMetadata { get; set; }

    public HashSet<string> DisabledMetadataPaths { get; } = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyCollection<string> MetadataRequests => _metadataRequests.ToArray();

    public static Task Main(string[] args) => new Program().RunServerAsync(args);

    public async Task RunServerAsync(string[]? args = null, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Starting in-memory test-only OAuth Server...");

        var builder = WebApplication.CreateEmptyBuilder(new()
        {
            Args = args,
        });

        if (_kestrelTransport is not null)
        {
            builder.Services.AddSingleton(_kestrelTransport);
        }

        builder.WebHost.UseKestrel(kestrelOptions =>
        {
            kestrelOptions.ListenLocalhost(_port, listenOptions =>
            {
                if (_useHttps)
                    listenOptions.UseHttps();
            });
        });

        builder.Services.AddRoutingCore();
        builder.Services.AddLogging();

        builder.Services.ConfigureHttpJsonOptions(jsonOptions =>
        {
            jsonOptions.SerializerOptions.TypeInfoResolverChain.Add(OAuthJsonContext.Default);
        });

        builder.Logging.AddConsole();
        if (_loggerProvider is not null)
        {
            builder.Logging.AddProvider(_loggerProvider);
        }

        var app = builder.Build();

        // ── Pre-registered clients ──
        var aliceClientId = "alice-client";
        var aliceClientSecret = "alice-secret";
        var bobClientId = "bob-client";
        var bobClientSecret = "bob-secret";
        var demoClientId = "demo-client";
        var demoClientSecret = "demo-secret";

        _clients[aliceClientId] = new ClientInfo
        {
            ClientId = aliceClientId,
            ClientSecret = aliceClientSecret,
            RequiresClientSecret = true,
            RedirectUris = ["http://localhost:1179/callback"],
        };

        _clients[bobClientId] = new ClientInfo
        {
            ClientId = bobClientId,
            ClientSecret = bobClientSecret,
            RequiresClientSecret = true,
            RedirectUris = ["http://localhost:1179/callback"],
        };

        _clients[demoClientId] = new ClientInfo
        {
            ClientId = demoClientId,
            ClientSecret = demoClientSecret,
            RequiresClientSecret = true,
            RedirectUris = ["http://localhost:1179/callback"],
        };

        _clients[_clientMetadataDocumentUrl] = new ClientInfo
        {
            ClientId = _clientMetadataDocumentUrl,
            RequiresClientSecret = false,
            RedirectUris = ["http://localhost:1179/callback"],
        };

        IResult HandleMetadataRequest(HttpContext context, string? issuerPath = null)
        {
            _metadataRequests.Enqueue(context.Request.Path);

            if (DisabledMetadataPaths.Contains(context.Request.Path))
            {
                return Results.NotFound();
            }

            if (!string.IsNullOrEmpty(issuerPath))
            {
                issuerPath = $"/{issuerPath}";
            }

            var metadata = new OAuthServerMetadata
            {
                Issuer = $"{_url}{issuerPath}",
                AuthorizationEndpoint = $"{_url}/authorize",
                TokenEndpoint = $"{_url}/token",
                JwksUri = $"{_url}/.well-known/jwks.json",
                ResponseTypesSupported = ["code"],
                SubjectTypesSupported = ["public"],
                IdTokenSigningAlgValuesSupported = ["RS256"],
                ScopesSupported = IncludeOfflineAccessInMetadata
                    ? ["openid", "profile", "email", "mcp:tools", "offline_access"]
                    : ["openid", "profile", "email", "mcp:tools"],
                TokenEndpointAuthMethodsSupported = ["client_secret_post"],
                ClaimsSupported = ["sub", "iss", "name", "email", "aud"],
                CodeChallengeMethodsSupported = ["S256"],
                GrantTypesSupported = ["authorization_code", "refresh_token"],
                IntrospectionEndpoint = $"{_url}/introspect",
                RegistrationEndpoint = $"{_url}/register",
                ClientIdMetadataDocumentSupported = ClientIdMetadataDocumentSupported,
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

        // JWKS endpoint
        app.MapGet("/.well-known/jwks.json", () =>
        {
            var parameters = _rsa.ExportParameters(false);

            var e = WebEncoders.Base64UrlEncode(parameters.Exponent ?? Array.Empty<byte>());
            var n = WebEncoders.Base64UrlEncode(parameters.Modulus ?? Array.Empty<byte>());

            var jwks = new JsonWebKeySet
            {
                Keys = [
                    new JsonWebKey
                    {
                        KeyType = "RSA",
                        Use = "sig",
                        KeyId = _keyId,
                        Algorithm = "RS256",
                        Exponent = e,
                        Modulus = n
                    }
                ]
            };

            return Results.Ok(jwks);
        });

        // Authorize endpoint
        app.MapGet("/authorize", (
            [FromQuery] string client_id,
            [FromQuery] string? redirect_uri,
            [FromQuery] string response_type,
            [FromQuery] string code_challenge,
            [FromQuery] string code_challenge_method,
            [FromQuery] string? scope,
            [FromQuery] string? state,
            [FromQuery] string? resource) =>
        {
            if (!_clients.TryGetValue(client_id, out var client))
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
                return Results.Redirect($"{redirect_uri}?error=unsupported_response_type&error_description=Only+code+response_type+is+supported&state={state}");
            }

            if (code_challenge_method != "S256")
            {
                return Results.Redirect($"{redirect_uri}?error=invalid_request&error_description=Only+S256+code_challenge_method+is+supported&state={state}");
            }

            if (ExpectResource ? (string.IsNullOrEmpty(resource) || !ValidResources.Contains(resource)) : !string.IsNullOrEmpty(resource))
            {
                return Results.Redirect($"{redirect_uri}?error=invalid_target&error_description=The+specified+resource+is+not+valid&state={state}");
            }

            var code = GenerateRandomToken();
            var requestedScopes = scope?.Split(' ').ToList() ?? [];

            var approvedScopes = ExpandScopes(client_id, resource, requestedScopes);
            Console.WriteLine($"Scope expansion: client={client_id}, requested=[{string.Join(" ", requestedScopes)}] → approved=[{string.Join(" ", approvedScopes)}]");

            _authCodes[code] = new AuthorizationCodeInfo
            {
                ClientId = client_id,
                RedirectUri = redirect_uri,
                CodeChallenge = code_challenge,
                Scope = approvedScopes,
                Resource = !string.IsNullOrEmpty(resource) ? new Uri(resource) : null
            };

            var redirectUrl = $"{redirect_uri}?code={code}";
            if (!string.IsNullOrEmpty(state))
            {
                redirectUrl += $"&state={Uri.EscapeDataString(state)}";
            }

            return Results.Redirect(redirectUrl);
        });

        // Token endpoint
        app.MapPost("/token", async (HttpContext context) =>
        {
            var form = await context.Request.ReadFormAsync();

            var client = AuthenticateClient(context, form);
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
            if (ExpectResource ? (string.IsNullOrEmpty(resource) || !ValidResources.Contains(resource)) : !string.IsNullOrEmpty(resource))
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

                if (string.IsNullOrEmpty(code) || !_authCodes.TryRemove(code, out var codeInfo))
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

                if (string.IsNullOrEmpty(code_verifier) || !VerifyCodeChallenge(code_verifier, codeInfo.CodeChallenge))
                {
                    return Results.BadRequest(new OAuthErrorResponse
                    {
                        Error = "invalid_grant",
                        ErrorDescription = "Code verifier does not match the challenge"
                    });
                }

                var response = GenerateJwtTokenResponse(client.ClientId, codeInfo.Scope, codeInfo.Resource);
                return Results.Ok(response);
            }
            else if (grant_type == "refresh_token")
            {
                var refresh_token = form["refresh_token"].ToString();

                if (string.IsNullOrEmpty(refresh_token) || !_tokens.TryGetValue(refresh_token, out var tokenInfo) || tokenInfo.ClientId != client.ClientId)
                {
                    return Results.BadRequest(new OAuthErrorResponse
                    {
                        Error = "invalid_grant",
                        ErrorDescription = "Invalid refresh token"
                    });
                }

                var response = GenerateJwtTokenResponse(client.ClientId, tokenInfo.Scopes, tokenInfo.Resource);

                if (!string.IsNullOrEmpty(refresh_token))
                {
                    _tokens.TryRemove(refresh_token, out _);
                }

                HasRefreshedToken = true;
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

        // Introspection endpoint
        app.MapPost("/introspect", async (HttpContext context) =>
        {
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

            if (_tokens.TryGetValue(token, out var tokenInfo))
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

        // Dynamic Client Registration endpoint (RFC 7591)
        app.MapPost("/register", async (HttpContext context) =>
        {
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
            var clientSecret = GenerateRandomToken();
            var issuedAt = DateTimeOffset.UtcNow;

            _clients[clientId] = new ClientInfo
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

        app.MapGet("/", () => "In-Memory OAuth 2.0 Server for AuthenticatedHttpMcpServer");

        Console.WriteLine($"OAuth Authorization Server running at {_url}");
        Console.WriteLine($"OAuth Server Metadata at {_url}/.well-known/oauth-authorization-server");
        Console.WriteLine($"JWT keys available at {_url}/.well-known/jwks.json");
        Console.WriteLine();
        Console.WriteLine("Pre-registered clients:");
        Console.WriteLine($"  👤 Alice (all tools):    client_id={aliceClientId}  secret={aliceClientSecret}");
        Console.WriteLine($"  👤 Bob   (all tools):    client_id={bobClientId}    secret={bobClientSecret}");
        Console.WriteLine($"  👤 Demo  (hello_world):  client_id={demoClientId}     secret={demoClientSecret}");
        Console.WriteLine();
        Console.WriteLine("Per-user tool permissions:");
        foreach (var (cid, (name, roles, tools)) in _userPermissions)
        {
            Console.WriteLine($"  {name}: roles=[{roles}]  allowed_tools=[{string.Join(", ", tools)}]");
        }

        await app.StartAsync(cancellationToken);
        _serverStarted.TrySetResult();

        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }

        await app.StopAsync();
    }

    private ClientInfo? AuthenticateClient(HttpContext context, IFormCollection form)
    {
        var clientId = form["client_id"].ToString();
        var clientSecret = form["client_secret"].ToString();

        if (string.IsNullOrEmpty(clientId) || !_clients.TryGetValue(clientId, out var client))
        {
            return null;
        }

        if (client.RequiresClientSecret && client.ClientSecret != clientSecret)
        {
            return null;
        }

        return client;
    }

    private List<string> ExpandScopes(string clientId, string? resource, List<string> requestedScopes)
    {
        var result = new HashSet<string>(requestedScopes);

        if (requestedScopes.Contains("mcp:tools"))
        {
            if (_userPermissions.TryGetValue(clientId, out var perm))
            {
                foreach (var tool in perm.AllowedTools)
                {
                    result.Add($"mcp:tools.{tool}");
                }
            }
        }

        return result.ToList();
    }

    private TokenResponse GenerateJwtTokenResponse(string clientId, List<string> scopes, Uri? resource)
    {
        var expiresIn = TimeSpan.FromHours(1);
        var issuedAt = DateTimeOffset.UtcNow;
        var expiresAt = issuedAt.Add(expiresIn);
        var jwtId = Guid.NewGuid().ToString();

        if (!_userPermissions.TryGetValue(clientId, out var perm))
        {
            perm = ($"user-{clientId}", "mcpcaller", new[] { "hello_world" });
        }
        var (userName, roles, allowedTools) = perm;

        var header = new Dictionary<string, string>
        {
            { "alg", "RS256" },
            { "typ", "JWT" },
            { "kid", _keyId },
        };

        var payload = new Dictionary<string, string>
        {
            { "iss", _url },
            { "sub", userName },
            { "name", userName },
            { "aud", resource?.ToString() ?? clientId },
            { "client_id", clientId },
            { "roles", roles },
            { "allowed_tools", string.Join(" ", allowedTools) },
            { "jti", jwtId },
            { "iat", issuedAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture) },
            { "exp", expiresAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture) },
            { "scope", string.Join(" ", scopes) },
        };

        var headerJson = JsonSerializer.Serialize(header, OAuthJsonContext.Default.DictionaryStringString);
        var payloadJson = JsonSerializer.Serialize(payload, OAuthJsonContext.Default.DictionaryStringString);

        var headerBase64 = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
        var payloadBase64 = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

        var dataToSign = $"{headerBase64}.{payloadBase64}";
        var signature = _rsa.SignData(Encoding.UTF8.GetBytes(dataToSign), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var signatureBase64 = WebEncoders.Base64UrlEncode(signature);

        var jwtToken = $"{headerBase64}.{payloadBase64}.{signatureBase64}";

        var refreshToken = GenerateRandomToken();

        var tokenInfo = new TokenInfo
        {
            ClientId = clientId,
            Scopes = scopes,
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt,
            Resource = resource,
            JwtId = jwtId
        };

        _tokens[refreshToken] = tokenInfo;

        return new TokenResponse
        {
            AccessToken = jwtToken,
            RefreshToken = refreshToken,
            TokenType = "Bearer",
            ExpiresIn = (int)expiresIn.TotalSeconds,
            Scope = string.Join(" ", scopes)
        };
    }

    public static string GenerateRandomToken()
    {
        var bytes = new byte[32];
        Random.Shared.NextBytes(bytes);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    public static bool VerifyCodeChallenge(string codeVerifier, string codeChallenge)
    {
        using var sha256 = SHA256.Create();
        var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
        var computedChallenge = WebEncoders.Base64UrlEncode(challengeBytes);

        return computedChallenge == codeChallenge;
    }
}
