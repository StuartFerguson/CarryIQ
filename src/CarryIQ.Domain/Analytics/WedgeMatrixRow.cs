namespace CarryIQ.Domain;

public sealed record WedgeMatrixRow
{
    public required WedgeMatrixClub Club { get; init; }

    public string ClubName => Club.Name;

    public ClubType ClubType => Club.ClubType;

    public bool IsActive => Club.IsActive;

    public int SortOrder => Club.SortOrder;

    public WedgeMatrixCell? A1 { get; init; }

    public WedgeMatrixCell? A2 { get; init; }

    public WedgeMatrixCell? A3 { get; init; }
}
