namespace CarryIQ.Domain;

public sealed record Club
{
    public required Guid Id { get; init; }

    public required Guid GolferProfileId { get; init; }

    public required string Name { get; init; }

    public required ClubType ClubType { get; init; }

    public string? Manufacturer { get; init; }

    public string? Model { get; init; }

    public decimal? Loft { get; init; }

    public string? Shaft { get; init; }

    public string? ShaftFlex { get; init; }

    public Distance? Length { get; init; }

    public required bool IsActive { get; init; }

    public required int SortOrder { get; init; }

    public string? Notes { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
