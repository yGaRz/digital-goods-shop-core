using Bank.Api.Health;
using Bank.Api.Logging;
using Bank.Api.Payments;
using Serilog;
using Serilog.Events;

const string consoleTemplate =
    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

Log.Logger = new LoggerConfiguration()
    .Enrich.WithProperty("service", "bank")
    .WriteTo.Console(outputTemplate: consoleTemplate)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.WebHost.UseUrls("http://0.0.0.0:8082");

    builder.Host.UseSerilog((context, _, configuration) =>
    {
        var logstashHost = context.Configuration["Logging:Logstash:Host"] ?? "localhost";
        var logstashPort = int.TryParse(context.Configuration["Logging:Logstash:Port"], out var port)
            ? port
            : 5000;

        configuration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("service", "bank")
            .WriteTo.Console(outputTemplate: consoleTemplate)
            .WriteTo.LogstashTcp(logstashHost, logstashPort);
    });

    var shopBaseUrl = builder.Configuration["Shop:BaseUrl"]
        ?? throw new InvalidOperationException("Shop:BaseUrl is required.");

    builder.Services.AddHttpClient("shop", client =>
    {
        client.BaseAddress = new Uri(shopBaseUrl.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(10);
    });

    builder.Services.AddHealthChecks()
        .AddCheck<ShopReachableHealthCheck>("shop");

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.MapHealthChecks("/health");
    app.MapPaymentsEndpoints();

    Log.Information("Bank.Api listening on :8082 → Shop {ShopBaseUrl}", shopBaseUrl);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Bank.Api failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
