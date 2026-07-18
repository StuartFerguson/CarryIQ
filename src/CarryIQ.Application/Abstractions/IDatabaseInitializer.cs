namespace CarryIQ.Application;

/// <summary>
/// Initializes the local CarryIQ database schema and seed data.
/// </summary>
public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}
