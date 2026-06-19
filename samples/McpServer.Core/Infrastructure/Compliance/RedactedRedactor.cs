using Microsoft.Extensions.Compliance.Redaction;

namespace McpServer.Infrastructure.Compliance;

public sealed class RedactedRedactor : Redactor
{
  private const string RedactedText = "[redacted]";

  public override int GetRedactedLength(ReadOnlySpan<char> input)
      => RedactedText.Length;

  public override int Redact(ReadOnlySpan<char> source, Span<char> destination)
  {
    RedactedText.CopyTo(destination);
    return RedactedText.Length;
  }
}