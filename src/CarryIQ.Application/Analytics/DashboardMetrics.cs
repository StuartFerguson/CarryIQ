namespace CarryIQ.Application;

public sealed record DashboardMetrics(
    decimal AverageCarryYards,
    decimal CarryStandardDeviationYards,
    decimal OfflineSpreadYards,
    decimal LeftRightBiasYards,
    decimal LongShortBiasYards,
    int SampleSize);
