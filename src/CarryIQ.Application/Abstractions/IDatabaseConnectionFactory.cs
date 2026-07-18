using System.Data.Common;

namespace CarryIQ.Application;

/// <summary>
/// Creates database connections for the current application data store.
/// </summary>
public interface IDatabaseConnectionFactory
{
    DbConnection CreateConnection();
}
