using System.ComponentModel;

using ModelContextProtocol.Server;

namespace McpServer.Prompts;

[McpServerPromptType]
public class DemoPrompts
{
  [McpServerPrompt]
  [Description("A friendly greeting prompt that introduces the assistant.")]
  public string Greeting()
      => "Greet the user warmly and ask how you can help them today.";

  [McpServerPrompt(Name = "code-review")]
  [Description("A prompt for reviewing code changes or snippets.")]
  public string CodeReview()
      => "You are a code reviewer. Examine the provided code carefully. "
       + "Identify bugs, security issues, performance problems, and style violations. "
       + "For each issue, explain why it matters and suggest a concrete fix.";

  [McpServerPrompt(Name = "summarize-text")]
  [Description("A prompt for summarizing long text into concise summaries.")]
  public string SummarizeText()
      => "Summarize the provided text in 3-5 bullet points. "
       + "Focus on the key facts, decisions, and conclusions. "
       + "Omit minor details and repetition.";

  [McpServerPrompt(Name = "explain-concept")]
  [Description("A prompt for explaining technical concepts in simple terms.")]
  public string ExplainConcept()
      => "Explain the concept in simple terms suitable for a beginner. "
       + "Use analogies where helpful. Avoid jargon. "
       + "If the concept builds on other ideas, briefly recap those first.";
}