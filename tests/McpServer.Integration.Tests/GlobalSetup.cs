using System.Runtime.CompilerServices;

// Here you could define global logic that would affect all tests

// You can use attributes at the assembly level to apply to all tests in the assembly
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