namespace CarryIQ.Application;

public interface IDashboardProjectionRepository
{
    Task<DashboardProjectionSource> LoadAsync(Guid golferProfileId, int recentSessionCount, CancellationToken cancellationToken);
}
