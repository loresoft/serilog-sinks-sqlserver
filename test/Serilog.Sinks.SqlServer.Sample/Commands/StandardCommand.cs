using System.CommandLine;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog.Sinks.Mongo.Sample.Services;
using Serilog.Sinks.SqlServer;
using Serilog.Sinks.SystemConsole.Themes;

namespace Serilog.Sinks.Mongo.Sample.Commands;

public class StandardCommand : Command
{
    private const string OutputTemplate = "{Timestamp:HH:mm:ss.fff} [{Level:u1}] {Message:lj}{NewLine}{Exception}";
    private const string ConnectionString = "Data Source=(local);Initial Catalog=Serilog;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=False;";

    public const string CommandName = "standard";
    public const string CommandDescription = "Use SQL Server Standard logging";


    public StandardCommand() : base(CommandName, CommandDescription)
    {
        SetAction(ExecuteAsync);
    }

    public static async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var builder = Host.CreateApplicationBuilder();

        // Configure Serilog with MongoDB Capped Collection
        builder.Services
            .AddSerilog(loggerConfiguration =>
            {
                loggerConfiguration
                    .MinimumLevel.Debug()
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
                        config.TableName = "LogEvent";
                        //config.Mappings.Clear();
                        //config.Mappings.Add(MappingDefaults.TimestampMapping);
                        //config.Mappings.Add(MappingDefaults.LevelMapping);
                        //config.Mappings.Add(MappingDefaults.MessageMapping);

                        //config.Mappings.Add(MappingDefaults.ExceptionMapping);
                        //config.Mappings.Add(MappingDefaults.PropertiesMapping);
                        //config.Mappings.Add(MappingDefaults.SourceContextMapping);


                    });
            });

        builder.Services
            .AddHostedService<LoggingService>();

        var host = builder.Build();

        var logger = host.Services.GetRequiredService<ILogger<StandardCommand>>();

        logger.LogInformation("Starting Standard Logging Sample...");
        logger.LogInformation("SQL Server Table: LogEvent");
        logger.LogInformation("Press Ctrl+C to exit");

        await host.RunAsync(token: cancellationToken);

        return 0;
    }
}
