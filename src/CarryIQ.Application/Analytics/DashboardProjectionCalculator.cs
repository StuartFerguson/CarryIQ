using System.Globalization;

namespace CarryIQ.Application;

public static class DashboardProjectionCalculator
{
    public static DashboardProjection Calculate(
        IEnumerable<Shot> shots,
        IEnumerable<PracticeSessionSummary> recentSessions,
        DominantHand dominantHand,
        int recentSessionCount)
    {
        ArgumentNullException.ThrowIfNull(shots);
        ArgumentNullException.ThrowIfNull(recentSessions);

        var includedShots = shots
            .Where(shot => shot.IsIncluded)
            .ToArray();

        var metrics = BuildMetrics(includedShots, dominantHand);
        var sessionsById = includedShots
            .GroupBy(shot => shot.PracticeSessionId)
            .ToDictionary(group => group.Key, group => group.ToArray());

        var selectedSessions = recentSessions
            .OrderByDescending(session => session.SessionDate)
            .ThenByDescending(session => session.StartTime ?? TimeOnly.MinValue)
            .ThenBy(session => session.Name, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(recentSessionCount, 0))
            .Select(session => new RecentSessionSummary(
                session.Id,
                session.SessionDate,
                session.Name,
                session.ShotCount,
                session.ValidShotCount,
                sessionsById.TryGetValue(session.Id, out var shotsForSession)
                    ? CalculateAverageCarry(shotsForSession)
                    : null))
            .ToArray();

        return new DashboardProjection(metrics, selectedSessions);
    }

    private static DashboardMetrics BuildMetrics(IReadOnlyCollection<Shot> includedShots, DominantHand dominantHand)
    {
        if (includedShots.Count == 0)
        {
            return new DashboardMetrics(0m, 0m, 0m, 0m, 0m, 0);
        }

        var carryValues = includedShots
            .Where(shot => shot.CarryDistance is not null)
            .Select(shot => shot.CarryDistance!.Value.Yards)
            .ToArray();

        var averageCarry = carryValues.Length == 0 ? 0m : carryValues.Average();
        var carryStandardDeviation = CalculatePopulationStandardDeviation(carryValues, averageCarry);
        var offlineDistances = includedShots
            .Where(shot => shot.OfflineDistance is not null)
            .Select(shot => shot.OfflineDistance!.Value.Yards)
            .ToArray();
        var offlineSpread = offlineDistances.Length == 0 ? 0m : offlineDistances.Select(Math.Abs).Average();
        var leftRightBias = CalculateSignedOfflineBias(includedShots, dominantHand);
        var longShortBias = CalculateLongShortBias(includedShots);

        return new DashboardMetrics(
            averageCarry,
            carryStandardDeviation,
            offlineSpread,
            leftRightBias,
            longShortBias,
            includedShots.Count);
    }

    private static decimal CalculateAverageCarry(IReadOnlyCollection<Shot> shots)
    {
        var carryValues = shots
            .Where(shot => shot.IsIncluded && shot.CarryDistance is not null)
            .Select(shot => shot.CarryDistance!.Value.Yards)
            .ToArray();

        return carryValues.Length == 0 ? 0m : carryValues.Average();
    }

    private static decimal CalculatePopulationStandardDeviation(decimal[] values, decimal mean)
    {
        if (values.Length == 0)
        {
            return 0m;
        }

        var variance = values
            .Select(value => Math.Pow((double)(value - mean), 2))
            .Average();

        return (decimal)Math.Sqrt(variance);
    }

    private static decimal CalculateSignedOfflineBias(IReadOnlyCollection<Shot> shots, DominantHand dominantHand)
    {
        var signedValues = shots
            .Where(shot => shot.OfflineDistance is not null)
            .Select(shot =>
            {
                var offline = shot.OfflineDistance!.Value.Yards;
                var direction = shot.LaunchDirection ?? 0m;
                var multiplier = dominantHand == DominantHand.Right ? 1m : -1m;
                var sign = direction < 0m ? -1m : direction > 0m ? 1m : 0m;
                return offline * sign * multiplier;
            })
            .ToArray();

        return signedValues.Length == 0 ? 0m : signedValues.Average();
    }

    private static decimal CalculateLongShortBias(IReadOnlyCollection<Shot> shots)
    {
        var deltas = shots
            .Where(shot => shot.CarryDistance is not null && shot.TargetDistance is not null)
            .Select(shot => shot.CarryDistance!.Value.Yards - shot.TargetDistance!.Value.Yards)
            .ToArray();

        return deltas.Length == 0 ? 0m : deltas.Average();
    }
}
