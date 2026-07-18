namespace CarryIQ.Application;

/// <summary>
/// Reads and writes shots.
/// </summary>
public interface IShotRepository
{
    Task AddAsync(Shot shot, CancellationToken cancellationToken);

    Task AddRangeAsync(IReadOnlyCollection<Shot> shots, CancellationToken cancellationToken);

    Task UpdateAsync(Shot shot, CancellationToken cancellationToken);

    Task<IReadOnlyList<Shot>> SearchAsync(ShotSearchCriteria criteria, CancellationToken cancellationToken);
}
