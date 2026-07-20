namespace CarryIQ.Domain;

public sealed record WedgeMatrixClub
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required ClubType ClubType { get; init; }

    public required int SortOrder { get; init; }

    public required bool IsActive { get; init; }
}
