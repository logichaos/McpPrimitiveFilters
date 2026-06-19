using System.Reflection;

using McpServer.Prompts;

using Microsoft.Extensions.Logging.Abstractions;

using ModelContextProtocol.Server;

namespace McpServer.Unit.Tests;

public class DemoPromptsTests
{
  private readonly DemoPrompts _sut = new();

  // ── Attributes ──────────────────────────────────────────────────────

  [Test]
  public async Task Class_HasMcpServerPromptTypeAttribute()
  {
    var attr = typeof(DemoPrompts).GetCustomAttribute<McpServerPromptTypeAttribute>();
    await Assert.That(attr).IsNotNull();
  }

  [Test]
  public async Task Greeting_HasMcpServerPromptAttribute()
  {
    var method = typeof(DemoPrompts).GetMethod(nameof(DemoPrompts.Greeting));
    var attr = method!.GetCustomAttribute<McpServerPromptAttribute>();
    await Assert.That(attr).IsNotNull();
  }

  [Test]
  public async Task CodeReview_HasPromptAttributeWithName()
  {
    var method = typeof(DemoPrompts).GetMethod(nameof(DemoPrompts.CodeReview));
    var attr = method!.GetCustomAttribute<McpServerPromptAttribute>();
    await Assert.That(attr).IsNotNull();
    await Assert.That(attr!.Name).IsEqualTo("code-review");
  }

  [Test]
  public async Task SummarizeText_HasPromptAttributeWithName()
  {
    var method = typeof(DemoPrompts).GetMethod(nameof(DemoPrompts.SummarizeText));
    var attr = method!.GetCustomAttribute<McpServerPromptAttribute>();
    await Assert.That(attr).IsNotNull();
    await Assert.That(attr!.Name).IsEqualTo("summarize-text");
  }

  [Test]
  public async Task ExplainConcept_HasPromptAttributeWithName()
  {
    var method = typeof(DemoPrompts).GetMethod(nameof(DemoPrompts.ExplainConcept));
    var attr = method!.GetCustomAttribute<McpServerPromptAttribute>();
    await Assert.That(attr).IsNotNull();
    await Assert.That(attr!.Name).IsEqualTo("explain-concept");
  }

  // ── Return values ────────────────────────────────────────────────────

  [Test]
  public async Task Greeting_ReturnsNonEmptyString()
  {
    var result = _sut.Greeting();
    await Assert.That(result).IsNotNull().And.IsNotEmpty();
  }

  [Test]
  public async Task CodeReview_ReturnsNonEmptyString()
  {
    var result = _sut.CodeReview();
    await Assert.That(result).IsNotNull().And.IsNotEmpty();
  }

  [Test]
  public async Task SummarizeText_ReturnsNonEmptyString()
  {
    var result = _sut.SummarizeText();
    await Assert.That(result).IsNotNull().And.IsNotEmpty();
  }

  [Test]
  public async Task ExplainConcept_ReturnsNonEmptyString()
  {
    var result = _sut.ExplainConcept();
    await Assert.That(result).IsNotNull().And.IsNotEmpty();
  }

  // ── Methods exist ────────────────────────────────────────────────────

  [Test]
  public async Task AllPromptMethods_ReturnString()
  {
    var methods = typeof(DemoPrompts).GetMethods()
        .Where(m => m.GetCustomAttribute<McpServerPromptAttribute>() is not null);

    foreach (var m in methods)
      await Assert.That(m.ReturnType).IsEqualTo(typeof(string));
  }

  [Test]
  public async Task AllPromptMethods_AreParameterless()
  {
    var methods = typeof(DemoPrompts).GetMethods()
        .Where(m => m.GetCustomAttribute<McpServerPromptAttribute>() is not null);

    foreach (var m in methods)
      await Assert.That(m.GetParameters()).IsEmpty();
  }
}