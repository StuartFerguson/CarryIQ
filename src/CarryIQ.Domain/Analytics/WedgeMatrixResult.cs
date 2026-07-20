namespace CarryIQ.Domain;

public sealed record WedgeMatrixResult
{
    public required IReadOnlyList<WedgeMatrixRow> Rows { get; init; }
}
