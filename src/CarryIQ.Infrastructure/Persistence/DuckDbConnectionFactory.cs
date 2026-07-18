using System.Data.Common;
using DuckDB.NET.Data;

namespace CarryIQ.Infrastructure;

public sealed class DuckDbConnectionFactory(IApplicationPaths applicationPaths) : IDatabaseConnectionFactory
{
    private readonly IApplicationPaths _applicationPaths = applicationPaths;

    public DbConnection CreateConnection() =>
        new DuckDBConnection($"Data Source={_applicationPaths.DatabasePath}");
}
