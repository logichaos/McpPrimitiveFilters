using System.Runtime.CompilerServices;

[assembly: System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]

internal static class GlobalConfigReloadDisabler
{
  [ModuleInitializer]
  public static void DisableConfigReload()
  {
    Environment.SetEnvironmentVariable(
        "DOTNET_hostBuilder__reloadConfigOnChange",
        "false",
        EnvironmentVariableTarget.Process);
  }
}