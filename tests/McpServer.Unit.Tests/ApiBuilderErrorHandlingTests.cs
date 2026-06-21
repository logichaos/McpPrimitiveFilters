using McpServer.Infrastructure;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using ModelContextProtocol;

namespace McpServer.Unit.Tests;

public class ApiBuilderErrorHandlingTests
{
  [Test]
  public async Task AddErrorHandling_RegistersExceptionHandler()
  {
    var builder = WebApplication.CreateBuilder(Array.Empty<string>());
    builder.Services.AddErrorHandling();

    var app = builder.Build();
    var handlers = app.Services.GetServices<IExceptionHandler>().ToList();

    await Assert.That(handlers).IsNotEmpty();
  }

  [Test]
  public async Task AddErrorHandling_RegistersProblemDetails()
  {
    var builder = WebApplication.CreateBuilder(Array.Empty<string>());
    builder.Services.AddErrorHandling();

    var app = builder.Build();
    var problemDetailsService = app.Services.GetService<IProblemDetailsService>();

    await Assert.That(problemDetailsService).IsNotNull();
  }

  [Test]
  public async Task UseErrorHandling_ReturnsSameApp()
  {
    var builder = WebApplication.CreateBuilder(Array.Empty<string>());
    var app = builder.Build();

    var result = ApiBuilder.UseErrorHandling(app);

    await Assert.That(result).IsEqualTo(app);
  }

  [Test]
  public async Task UseErrorHandling_DoesNotThrow_WhenNoServicesRegistered()
  {
    var builder = WebApplication.CreateBuilder(Array.Empty<string>());
    var app = builder.Build();

    var result = ApiBuilder.UseErrorHandling(app);
    await Assert.That(result).IsNotNull();
  }

  [Test]
  [Arguments(typeof(McpProtocolException), 400)]
  [Arguments(typeof(InvalidOperationException), 422)]
  [Arguments(typeof(ArgumentException), 422)]
  [Arguments(typeof(BadHttpRequestException), 400)]
  [Arguments(typeof(FormatException), 400)]
  [Arguments(typeof(UnauthorizedAccessException), 403)]
  [Arguments(typeof(NotImplementedException), 500)]
  [Arguments(typeof(NullReferenceException), 500)]
  public async Task ExceptionHandler_MapsExceptionToExpectedStatusCode(
      Type exceptionType, int expectedStatusCode)
  {
    var builder = WebApplication.CreateBuilder(Array.Empty<string>());
    builder.Services.AddErrorHandling();

    var app = builder.Build();
    var exception = CreateException(exceptionType);

    using var scope = app.Services.CreateScope();
    var problemDetailsService = scope.ServiceProvider
        .GetRequiredService<IProblemDetailsService>();

    var handler = new McpExceptionHandlerAccessor(problemDetailsService);
    var httpContext = new DefaultHttpContext();
    httpContext.Response.Body = new MemoryStream();

    var handled = await handler.TryHandleAsync(
        httpContext, exception, CancellationToken.None);

    await Assert.That(handled).IsTrue();
    await Assert.That(httpContext.Response.StatusCode).IsEqualTo(expectedStatusCode);
    await Assert.That(httpContext.Response.ContentType!).Contains("application/problem+json");
  }

  private static Exception CreateException(Type exceptionType)
  {
    if (exceptionType == typeof(McpProtocolException))
    {
      return new McpProtocolException("test protocol error", McpErrorCode.InvalidParams);
    }

    if (exceptionType == typeof(BadHttpRequestException))
    {
      return new BadHttpRequestException("bad request");
    }

    try
    {
      return (Exception)Activator.CreateInstance(exceptionType, ["test message"])!;
    }
    catch
    {
      return (Exception)Activator.CreateInstance(exceptionType)!;
    }
  }
  
  private sealed class McpExceptionHandlerAccessor : IExceptionHandler
  {
    private readonly McpExceptionHandler _inner;

    public McpExceptionHandlerAccessor(IProblemDetailsService problemDetailsService)
    {
      _inner = new McpExceptionHandler(problemDetailsService);
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
      return await _inner.TryHandleAsync(httpContext, exception, cancellationToken);
    }
  }
}