using System.Reflection;
using System.Text;

using DbUp;
using DbUp.Engine;

using Microsoft.Data.SqlClient;

namespace Serilog.Sinks.SqlServer.Tests.Fixtures;

public static class DatabaseInitialize
{
    public static void Initialize(string connectionString)
    {
        // create database
        EnsureDatabase.For.SqlDatabase(connectionString);

        // parse connection string
        var builder = new SqlConnectionStringBuilder(connectionString);
        var database = builder.InitialCatalog;

        var upgradeEngine = DeployChanges.To
                .SqlDatabase(connectionString)
                .WithScriptsEmbeddedInAssembly(
                    Assembly.GetExecutingAssembly(),
                    Encoding.Default,
                    new SqlScriptOptions { RunGroupOrder = 1 }
                )
                .Build();

        var result = upgradeEngine.PerformUpgrade();
    }
}
