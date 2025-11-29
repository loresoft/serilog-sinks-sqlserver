using System.CommandLine;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog.Sinks.Mongo.Sample.Services;
using Serilog.Sinks.SqlServer;
using Serilog.Sinks.SystemConsole.Themes;

namespace Serilog.Sinks.Mongo.Sample.Commands;

public class ExtendedCommand : Command
{
    private const string OutputTemplate = "{Timestamp:HH:mm:ss.fff} [{Level:u1}] {Message:lj}{NewLine}{Exception}";
    private const string ConnectionString = "Data Source=(local);Initial Catalog=Serilog;Integrated Security=True;TrustServerCertificate=True;";

    public const string CommandName = "extended";
    public const string CommandDescription = "Use SQL Server Extended logging";

    public ExtendedCommand() : base(CommandName, CommandDescription)
    {
        SetAction(ExecuteAsync);
    }

    public static async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var builder = Host.CreateApplicationBuilder();

        // Configure Serilog with SQL Server
        builder.Services
            .AddSerilog(loggerConfiguration =>
            {
                loggerConfiguration
                    .MinimumLevel.Information()
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("ApplicationName", builder.Environment.ApplicationName)
                    .Enrich.WithProperty("ApplicationVersion", ThisAssembly.InformationalVersion)
                    .Enrich.WithProperty("EnvironmentName", builder.Environment.EnvironmentName)
                    .WriteTo.Console(
                        outputTemplate: OutputTemplate,
                        theme: AnsiConsoleTheme.Code
                    )
                    .WriteTo.SqlServer(config =>
                    {
                        config.ConnectionString = ConnectionString;
                        config.TableName = "Extended";

                        // add custom column mappings
                        config.AddPropertyMapping("ApplicationName");
                        config.AddPropertyMapping("ApplicationVersion");
                        config.AddPropertyMapping("EnvironmentName");
                    });
            });

        builder.Services
            .AddHostedService<LoggingService>();

        var host = builder.Build();

        var logger = host.Services.GetRequiredService<ILogger<ExtendedCommand>>();

        logger.LogInformation("Starting Extended Command Sample...");
        logger.LogInformation("SQL Server Table: Extended");
        logger.LogInformation("Press Ctrl+C to exit");

        await host.RunAsync(token: cancellationToken);

        return 0;
    }
}
