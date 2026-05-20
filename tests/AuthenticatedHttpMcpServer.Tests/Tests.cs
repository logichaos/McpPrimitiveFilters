namespace AuthenticatedHttpMcpServer.Tests;

public class Tests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory WebApplicationFactory { get; init; }

    [Test]
    public async Task Test()
    {
        var client = WebApplicationFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1bmlxdWVfbmFtZSI6Im1heGkiLCJzdWIiOiJtYXhpIiwianRpIjoiMzY1OTg1YzkiLCJzY29wZSI6WyJ0b29sOmhlbGxvX3dvcmxkIiwidG9vbDpyYW5kb21fbnVtYmVyIiwidG9vbDphbGwiXSwicm9sZSI6ImF3ZXNvbWUiLCJhdWQiOlsiaHR0cDovL2xvY2FsaG9zdDo1MTA1IiwiaHR0cHM6Ly9sb2NhbGhvc3Q6NzA5MyJdLCJuYmYiOjE3NzkyODM5OTcsImV4cCI6MTc4NzIzMjc5NywiaWF0IjoxNzc5MjgzOTk3LCJpc3MiOiJkb3RuZXQtdXNlci1qd3RzIn0.PMEiAJ3wOIyu-IisOrTLtxp7_c3DO3H9LCeiVXIzO5Q");
        

        var response = await client.GetAsync("/health");

        var stringContent = await response.Content.ReadAsStringAsync();

        await Assert.That(stringContent).IsEqualTo("Healthy");
    }
}
