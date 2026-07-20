namespace CarryIQ.Domain;

public static class WedgeMatrixCalculator
{
    private static readonly HashSet<ClubType> WedgeClubTypes =
    [
        ClubType.PitchingWedge,
        ClubType.GapWedge,
        ClubType.SandWedge,
        ClubType.LobWedge,
    ];

    public static WedgeMatrixResult Calculate(
        IEnumerable<WedgeMatrixClub> clubs,
        IEnumerable<WedgeSwingReference> references,
        bool includeInactive)
    {
        ArgumentNullException.ThrowIfNull(clubs);
        ArgumentNullException.ThrowIfNull(references);

        var wedgeClubs = clubs
            .Where(club => WedgeClubTypes.Contains(club.ClubType))
            .Where(club => includeInactive || club.IsActive)
            .OrderBy(club => club.SortOrder)
            .ThenBy(club => club.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var referenceLookup = references
            .Select(reference => new
            {
                Reference = reference,
                SetupType = ResolveSetupType(reference),
            })
            .Where(item => item.SetupType is not null)
            .GroupBy(item => (item.Reference.ClubId, item.SetupType!.Value))
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(item => item.Reference.IsManualOverride)
                    .ThenByDescending(item => item.Reference.UpdatedAt)
                    .First().Reference);

        var rows = wedgeClubs
            .Select(club => new WedgeMatrixRow
            {
                Club = club,
                A1 = BuildCell(referenceLookup, club.Id, WedgeSetupType.A1),
                A2 = BuildCell(referenceLookup, club.Id, WedgeSetupType.A2),
                A3 = BuildCell(referenceLookup, club.Id, WedgeSetupType.A3),
            })
            .ToArray();

        return new WedgeMatrixResult
        {
            Rows = rows,
        };
    }

    private static WedgeMatrixCell? BuildCell(
        Dictionary<(Guid ClubId, WedgeSetupType SetupType), WedgeSwingReference> references,
        Guid clubId,
        WedgeSetupType setupType)
    {
        if (!references.TryGetValue((clubId, setupType), out var reference))
        {
            return null;
        }

        return new WedgeMatrixCell
        {
            SetupType = setupType,
            TargetDistance = reference.TargetDistance,
            AverageCarry = reference.AverageCarry,
            CarryStandardDeviation = reference.CarryStandardDeviation,
            SampleSize = reference.SampleSize,
            IsManualOverride = reference.IsManualOverride,
        };
    }

    private static WedgeSetupType? ResolveSetupType(WedgeSwingReference reference)
    {
        if (TryParseSetupType(reference.SwingLabel, out var setupType))
        {
            return setupType;
        }

        if (TryParseSetupType(reference.ClockPosition, out setupType))
        {
            return setupType;
        }

        return null;
    }

    private static bool TryParseSetupType(string? value, out WedgeSetupType setupType)
    {
        if (Enum.TryParse(value, ignoreCase: true, out setupType) &&
            Enum.IsDefined(setupType))
        {
            return true;
        }

        setupType = default;
        return false;
    }
}
