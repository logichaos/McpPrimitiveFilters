namespace AuthenticatedHttpMcpServer.Infrastructure;

internal static partial class Constants
{
  public static class Auth
  {
    public const string AzureApiKeyName = "Ocp-Apim-Subscription-Key";

    public static class Schemes
    {
      public const string Bearer = "Bearer";
      public const string ApiKey = "ApiKey-Header";
    }

    public static class Policies
    {
      public const string MrAwesome = "mrawesome";
      public const string McpSubscription = "mcp_subscription";
    }

    public static class Roles
    {
      public const string McpCaller = "mcpcaller";
      public const string Awesome = "awesome";
    }
  }

  public static class RateLimit
  {
    public static class Policies
    {
      public const string Fixed = "fixed";
    }
  }
}