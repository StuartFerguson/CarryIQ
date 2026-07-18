namespace CarryIQ.Application;

/// <summary>
/// Reads and writes clubs in the golfer's bag.
/// </summary>
public interface IClubRepository
{
    Task<Club?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ClubSummary>> SearchAsync(
        ClubSearchCriteria criteria,
        CancellationToken cancellationToken);

    Task SaveAsync(Club club, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
