var builder = WebApplication.CreateBuilder(args)
    .AddLogging();

builder.Services
    .AddMcpServer()
    .WithHttpTransport(opts => opts.Stateless = true)
    .WithTools<RandomNumberTools>();
var app = builder.Build();

app.UseLogging();

app.MapGet("/", () => "this is working");
await app.RunAsync();