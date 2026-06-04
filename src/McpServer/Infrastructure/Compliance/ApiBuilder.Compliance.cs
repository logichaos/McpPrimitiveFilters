using McpServer.Infrastructure.Compliance;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;

namespace McpServer.Infrastructure;

public static partial class ApiBuilder_Compliance
{
    public static IHostApplicationBuilder AddComplianceServices(this IHostApplicationBuilder builder)
    {
        builder.Logging.EnableRedaction();

        builder.Services.AddRedaction(redaction =>
        {
            redaction.SetRedactor<RedactedRedactor>(
                new DataClassificationSet(McpTaxonomy.SensitiveData));

            redaction.SetRedactor<NullRedactor>(
                new DataClassificationSet(McpTaxonomy.PublicData));
        });

        return builder;
    }
}
