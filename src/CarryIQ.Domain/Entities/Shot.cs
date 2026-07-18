namespace CarryIQ.Domain;

public sealed record Shot
{
    public required Guid Id { get; init; }

    public required Guid PracticeSessionId { get; init; }

    public required Guid ClubId { get; init; }

    public required int ShotSequence { get; init; }

    public required DateTimeOffset RecordedAt { get; init; }

    public required ShotSourceKind Source { get; init; }

    public Distance? CarryDistance { get; init; }

    public Distance? TotalDistance { get; init; }

    public Speed? BallSpeed { get; init; }

    public Speed? ClubSpeed { get; init; }

    public decimal? SmashFactor { get; init; }

    public decimal? LaunchAngle { get; init; }

    public decimal? LaunchDirection { get; init; }

    public decimal? ApexHeight { get; init; }

    public decimal? SpinRate { get; init; }

    public decimal? SpinAxis { get; init; }

    public Distance? OfflineDistance { get; init; }

    public Distance? RollDistance { get; init; }

    public decimal? HangTime { get; init; }

    public decimal? AttackAngle { get; init; }

    public decimal? ClubPath { get; init; }

    public decimal? FaceAngle { get; init; }

    public decimal? FaceToPath { get; init; }

    public decimal? DynamicLoft { get; init; }

    public StrikeQuality? StrikeQuality { get; init; }

    public ShotShape? ShotShape { get; init; }

    public string? LieType { get; init; }

    public SwingType? SwingType { get; init; }

    public Distance? TargetDistance { get; init; }

    public required bool IsIncluded { get; init; }

    public string? ExclusionReason { get; init; }

    public required bool IsEstimated { get; init; }

    public string? Notes { get; init; }

    public string? RawImportData { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
