namespace CarryIQ.Application;

/// <summary>
/// Reads wedge swing reference rows used to build the wedge matrix.
/// </summary>
public interface IWedgeSwingReferenceRepository
{
    Task<IReadOnlyList<WedgeSwingReference>> SearchAsync(Guid golferProfileId, CancellationToken cancellationToken);

    Task SaveAsync(WedgeSwingReference reference, CancellationToken cancellationToken);
}
