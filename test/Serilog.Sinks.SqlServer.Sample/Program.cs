using System.CommandLine;

using Serilog.Debugging;
using Serilog.Sinks.SqlServer.Sample.Commands;

namespace Serilog.Sinks.SqlServer.Sample;

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        // Enable Serilog self-logging for diagnostics
        SelfLog.Enable(Console.Error);

        var rootCommand = new RootCommand("Serilog SQL Server Sample Application");

        // Create 'standard' command for SQL Server Standard logging
        var standardCommand = new StandardCommand();
        rootCommand.Subcommands.Add(standardCommand);

        // Create 'extended' command for SQL Server Extended logging
        var extendedCommand = new ExtendedCommand();
        rootCommand.Subcommands.Add(extendedCommand);

        // Create 'config' command for configuration-based setup
        var configCommand = new ConfigCommand();
        rootCommand.Subcommands.Add(configCommand);

        return await rootCommand.Parse(args).InvokeAsync();
    }
}
