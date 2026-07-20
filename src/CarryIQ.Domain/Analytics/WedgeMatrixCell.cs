namespace CarryIQ.Domain;

public sealed record WedgeMatrixCell
{
    public required WedgeSetupType SetupType { get; init; }

    public Distance? TargetDistance { get; init; }

    public Distance? AverageCarry { get; init; }

    public Distance? CarryStandardDeviation { get; init; }

    public required int SampleSize { get; init; }

    public required bool IsManualOverride { get; init; }
}
