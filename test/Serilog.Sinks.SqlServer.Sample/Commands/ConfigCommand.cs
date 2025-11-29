using System.CommandLine;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog.Sinks.Mongo.Sample.Services;

namespace Serilog.Sinks.Mongo.Sample.Commands;

public class ConfigCommand : Command
{
    public const string CommandName = "config";
    public const string CommandDescription = "Use SQL Server configured from appsettings.json";

    public ConfigCommand() : base(CommandName, CommandDescription)
    {
        SetAction(ExecuteAsync);
    }

    public static async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var builder = Host.CreateApplicationBuilder();

        // Add appsettings.json configuration
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        // Configure Serilog from appsettings.json
        builder.Services
            .AddSerilog((services, loggerConfiguration) =>
            {
                loggerConfiguration
                    .ReadFrom.Configuration(builder.Configuration)
                    .ReadFrom.Services(services);
            });

        builder.Services
            .AddHostedService<LoggingService>();

        var host = builder.Build();

        var logger = host.Services.GetRequiredService<ILogger<ConfigCommand>>();

        logger.LogInformation("Starting Config-Based Sample...");
        logger.LogInformation("Serilog configured from appsettings.json");
        logger.LogInformation("SQL Server Table: LogEvent (from configuration)");
        logger.LogInformation("Press Ctrl+C to exit");

        await host.RunAsync(token: cancellationToken);

        return 0;
    }
}
