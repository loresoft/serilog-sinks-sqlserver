using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Serilog.Events;

using Testcontainers.MsSql;

namespace Serilog.Sinks.SqlServer.Tests.Fixtures;

public class DatabaseFixture : TestApplicationFixture, IAsyncLifetime
{
    private const string OutputTemplate = "{Timestamp:HH:mm:ss.fff} [{Level:u1}] {Message:lj}{NewLine}{Exception}";

    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Bn87bBYhLjYRj%9zRgUc")
        .Build();

    public async ValueTask InitializeAsync()
    {
        await _msSqlContainer.StartAsync();

        // initialize database
        var connectionString = GetConnectionString();
        DatabaseInitialize.Initialize(connectionString);
    }

    public async ValueTask DisposeAsync()
    {
        await _msSqlContainer.DisposeAsync();
    }

    public string GetConnectionString()
    {
        var builder = new SqlConnectionStringBuilder(_msSqlContainer.GetConnectionString());
        builder.InitialCatalog = "SerilogDocker";

        return builder.ToString();
    }

    protected override void ConfigureApplication(HostApplicationBuilder builder)
    {
        base.ConfigureApplication(builder);

        var connectionString = GetConnectionString();

        // override connection string to use docker container
        var configurationData = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Serilog"] = connectionString
        };

        builder.Configuration.AddInMemoryCollection(configurationData);

        var services = builder.Services;

        builder.Services.AddSerilog((services, configuration) => configuration
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Debug)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ApplicationName", builder.Environment.ApplicationName)
            .Enrich.WithProperty("EnvironmentName", builder.Environment.EnvironmentName)
            .Filter.ByExcluding(logEvent => logEvent.Exception is OperationCanceledException)
            .WriteTo.Console(outputTemplate: OutputTemplate)
            .WriteTo.SqlServer(options =>
            {
                options.ConnectionString = connectionString;
                options.TableName = "LogEvent";
            })
        );

    }
}
