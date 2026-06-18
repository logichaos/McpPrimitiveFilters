using McpPrimitiveFilters;
using McpPrimitiveFilters.Logging;
using McpPrimitiveFilters.Strategies;

using Microsoft.Extensions.DependencyInjection.Extensions;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Microsoft.Extensions.DependencyInjection;

public static class McpPrimitiveFiltersExtensions
{
    public static IMcpServerBuilder AddMcpPrimitiveFilters(
        this IMcpServerBuilder builder,
        Action<McpPrimitiveFiltersOptions>? configure = null)
    {
        var options = new McpPrimitiveFiltersOptions();
        configure?.Invoke(options);

        builder.Services.AddHttpContextAccessor();

        if (options.AppSettingsEnabled)
            builder.Services.TryAddSingleton<
                McpPrimitiveFilteringStrategy, AppSettingsPrimitiveFilteringStrategy>();

        if (options.OAuthClaimsEnabled)
            builder.Services.TryAddSingleton<
                McpPrimitiveFilteringStrategy, OAuthClaimsFilteringStrategy>();

        builder.WithRequestFilters(filters =>
        {
            if (options.FilterTools)
            {
                filters.AddListToolsFilter(next => async (context, ct) =>
                {
                    var result = await next(context, ct);

                    if (result.Tools is { Count: > 0 })
                    {
                        var httpContextAccessor = context.Services?.GetService<IHttpContextAccessor>();
                        var strategies = context.Services?.GetServices<McpPrimitiveFilteringStrategy>();

                        if (httpContextAccessor?.HttpContext is { } httpContext && strategies is not null)
                        {
                            var toolNames = result.Tools.Select(t => t.Name).ToList();
                            foreach (var strategy in strategies)
                                toolNames = strategy.FilterPrimitives(
                                    httpContext, McpPrimitiveType.Tool, toolNames).ToList();

                            var allowedNames = new HashSet<string>(toolNames, StringComparer.OrdinalIgnoreCase);
                            result.Tools = result.Tools.Where(t => allowedNames.Contains(t.Name)).ToList();
                        }
                    }

                    return result;
                });

                filters.AddCallToolFilter(next => async (context, ct) =>
                {
                    var httpContextAccessor = context.Services?.GetService<IHttpContextAccessor>();
                    var strategies = context.Services?.GetServices<McpPrimitiveFilteringStrategy>();
                    var logger = context.Services?.GetService<ILoggerFactory>()?.CreateLogger("McpPrimitiveFilters");

                    if (httpContextAccessor?.HttpContext is { } httpContext
                        && strategies is not null
                        && context.Params is { } requestParams)
                    {
                        var toolName = requestParams.Name;
                        var names = new[] { toolName }.AsEnumerable();
                        foreach (var strategy in strategies)
                            names = strategy.FilterPrimitives(
                                httpContext, McpPrimitiveType.Tool, names);

                        if (!names.Any())
                        {
                            McpFilteringLogMessages.CallDenied(
                                logger!, McpPrimitiveType.Tool,
                                httpContext.User.Identity?.Name, toolName);
                            return new CallToolResult
                            {
                                Content = [new TextContentBlock { Text = $"Tool '{toolName}' is not authorized." }],
                                IsError = true
                            };
                        }
                    }

                    return await next(context, ct);
                });
            }

            if (options.FilterResources)
            {
                filters.AddListResourcesFilter(next => async (context, ct) =>
                {
                    var result = await next(context, ct);

                    if (result.Resources is { Count: > 0 })
                    {
                        var httpContextAccessor = context.Services?.GetService<IHttpContextAccessor>();
                        var strategies = context.Services?.GetServices<McpPrimitiveFilteringStrategy>();

                        if (httpContextAccessor?.HttpContext is { } httpContext && strategies is not null)
                        {
                            var resourceNames = result.Resources.Select(r => r.Name).ToList();
                            foreach (var strategy in strategies)
                                resourceNames = strategy.FilterPrimitives(
                                    httpContext, McpPrimitiveType.Resource, resourceNames).ToList();

                            var allowedNames = new HashSet<string>(resourceNames, StringComparer.OrdinalIgnoreCase);
                            result.Resources = result.Resources.Where(r => allowedNames.Contains(r.Name)).ToList();
                        }
                    }

                    return result;
                });

                filters.AddListResourceTemplatesFilter(next => async (context, ct) =>
                {
                    var result = await next(context, ct);

                    if (result.ResourceTemplates is { Count: > 0 })
                    {
                        var httpContextAccessor = context.Services?.GetService<IHttpContextAccessor>();
                        var strategies = context.Services?.GetServices<McpPrimitiveFilteringStrategy>();

                        if (httpContextAccessor?.HttpContext is { } httpContext && strategies is not null)
                        {
                            var resourceNames = result.ResourceTemplates.Select(r => r.Name).ToList();
                            foreach (var strategy in strategies)
                                resourceNames = strategy.FilterPrimitives(
                                    httpContext, McpPrimitiveType.Resource, resourceNames).ToList();

                            var allowedNames = new HashSet<string>(resourceNames, StringComparer.OrdinalIgnoreCase);
                            result.ResourceTemplates = result.ResourceTemplates.Where(r => allowedNames.Contains(r.Name)).ToList();
                        }
                    }

                    return result;
                });

                filters.AddReadResourceFilter(next => async (context, ct) =>
                {
                    var httpContextAccessor = context.Services?.GetService<IHttpContextAccessor>();
                    var strategies = context.Services?.GetServices<McpPrimitiveFilteringStrategy>();
                    var logger = context.Services?.GetService<ILoggerFactory>()?.CreateLogger("McpPrimitiveFilters");

                    if (httpContextAccessor?.HttpContext is { } httpContext
                        && strategies is not null
                        && context.Params?.Uri is { } uri)
                    {
                        var serverResources = context.Services?.GetServices<McpServerResource>();
                        if (serverResources is not null)
                        {
                            foreach (var resource in serverResources)
                            {
                                if (resource.IsMatch(uri))
                                {
                                    var resourceName = resource.ProtocolResource?.Name
                                        ?? resource.ProtocolResourceTemplate?.Name;

                                    if (resourceName is not null)
                                    {
                                        var names = new[] { resourceName }.AsEnumerable();
                                        foreach (var strategy in strategies)
                                            names = strategy.FilterPrimitives(
                                                httpContext, McpPrimitiveType.Resource, names);

                                        if (!names.Any())
                                        {
                                            McpFilteringLogMessages.CallDenied(
                                                logger!, McpPrimitiveType.Resource,
                                                httpContext.User.Identity?.Name, resourceName);
                                            return new ReadResourceResult
                                            {
                                                Contents = [new TextResourceContents
                                                {
                                                    Uri = uri,
                                                    MimeType = "text/plain",
                                                    Text = $"Resource '{uri}' is not authorized."
                                                }]
                                            };
                                        }
                                    }

                                    break;
                                }
                            }
                        }
                    }

                    return await next(context, ct);
                });
            }
        });

        return builder;
    }
}
