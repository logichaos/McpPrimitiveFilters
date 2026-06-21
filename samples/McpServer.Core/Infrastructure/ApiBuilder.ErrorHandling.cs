using Microsoft.AspNetCore.Diagnostics;

using ModelContextProtocol;

namespace McpServer.Infrastructure;

public static partial class ApiBuilder
{
  public static IServiceCollection AddErrorHandling(this IServiceCollection services)
  {
    services.AddExceptionHandler<McpExceptionHandler>();
    services.AddProblemDetails(options =>
    {
      options.CustomizeProblemDetails = context =>
          {
          context.ProblemDetails.Instance =
                  $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
          context.ProblemDetails.Extensions.TryAdd(
                  "requestId", context.HttpContext.TraceIdentifier);
        };
    });

    return services;
  }
  public static WebApplication UseErrorHandling(this WebApplication app)
  {
    app.UseExceptionHandler();
    return app;
  }
}

internal sealed class McpExceptionHandler(IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
  public async ValueTask<bool> TryHandleAsync(
      HttpContext httpContext,
      Exception exception,
      CancellationToken cancellationToken)
  {
    var statusCode = exception switch
    {
      McpProtocolException
          => StatusCodes.Status400BadRequest,
      InvalidOperationException or ArgumentException
          => StatusCodes.Status422UnprocessableEntity,
      BadHttpRequestException or FormatException
          => StatusCodes.Status400BadRequest,
      UnauthorizedAccessException
          => StatusCodes.Status403Forbidden,
      _ => StatusCodes.Status500InternalServerError
    };

    httpContext.Response.StatusCode = statusCode;
    httpContext.Response.ContentType = "application/problem+json";

    return await problemDetailsService.TryWriteAsync(new()
    {
      Exception = exception,
      HttpContext = httpContext,
      ProblemDetails =
            {
                Status = statusCode,
                Detail = exception.Message
            }
    });
  }
}