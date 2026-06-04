using Microsoft.Extensions.Compliance.Classification;

namespace McpServer.Infrastructure.Compliance;

public class SensitiveDataAttribute : DataClassificationAttribute
{
    public SensitiveDataAttribute() : base(McpTaxonomy.SensitiveData) { }
}
