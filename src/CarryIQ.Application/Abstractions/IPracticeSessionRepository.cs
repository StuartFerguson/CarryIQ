namespace CarryIQ.Application;

/// <summary>
/// Reads and writes practice sessions.
/// </summary>
public interface IPracticeSessionRepository
{
    Task<PracticeSession?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<PracticeSessionSummary>> SearchAsync(
        SessionSearchCriteria criteria,
        CancellationToken cancellationToken);

    Task SaveAsync(PracticeSession session, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
