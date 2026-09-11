using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Shop.Api.Logging;
using Shop.Infrastructure;

// Console: readable text for docker logs. Logstash: Compact JSON for Kibana.
const string consoleTemplate =
    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

Log.Logger = new LoggerConfiguration()
    .Enrich.WithProperty("service", "shop")
    .WriteTo.Console(outputTemplate: consoleTemplate)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.WebHost.UseUrls("http://0.0.0.0:8080");

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
            .Enrich.WithProperty("service", "shop")
            .WriteTo.Console(outputTemplate: consoleTemplate)
            .WriteTo.LogstashTcp(logstashHost, logstashPort);
    });

    var connectionString = builder.Configuration.GetConnectionString("Shop")
        ?? throw new InvalidOperationException("ConnectionStrings:Shop is required.");

    builder.Services.AddShopInfrastructure(connectionString);

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.MapHealthChecks("/health");

    Log.Information("Shop.Api listening on :8080");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Shop.Api failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
