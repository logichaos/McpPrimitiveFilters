using Microsoft.Extensions.Compliance.Classification;

namespace McpServer.Infrastructure.Compliance;

public static class McpTaxonomy
{
  public static string TaxonomyName => typeof(McpTaxonomy).FullName!;

  public static DataClassification PublicData => new(TaxonomyName, nameof(PublicData));

  public static DataClassification SensitiveData => new(TaxonomyName, nameof(SensitiveData));
}