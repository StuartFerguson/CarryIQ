using System.Globalization;

namespace CarryIQ.Infrastructure;

public sealed record DemoDataSeedOptions(int SessionCount, int WeekSpan);

public sealed record DemoDataSeedResult(int CreatedClubCount, int AvailableClubCount, int SessionCount, int ShotCount);

public interface IDemoDataSeeder
{
    Task<DemoDataSeedResult> SeedAsync(DemoDataSeedOptions options, CancellationToken cancellationToken);
}

public sealed class DemoDataSeeder : IDemoDataSeeder
{
    private readonly IClubRepository _clubRepository;
    private readonly IPracticeSessionRepository _sessionRepository;
    private readonly IShotRepository _shotRepository;
    private readonly IDatabaseConnectionFactory _connectionFactory;

    public DemoDataSeeder(
        IClubRepository clubRepository,
        IPracticeSessionRepository sessionRepository,
        IShotRepository shotRepository,
        IDatabaseConnectionFactory connectionFactory)
    {
        _clubRepository = clubRepository;
        _sessionRepository = sessionRepository;
        _shotRepository = shotRepository;
        _connectionFactory = connectionFactory;
    }

    public async Task<DemoDataSeedResult> SeedAsync(DemoDataSeedOptions options, CancellationToken cancellationToken)
    {
        Validate(options);

        var golferProfileId = await LoadDefaultGolferProfileIdAsync(cancellationToken);
        if (golferProfileId == Guid.Empty)
        {
            throw new InvalidOperationException("No golfer profile is available for demo data seeding.");
        }

        var clubSummaries = (await _clubRepository.SearchAsync(
            new ClubSearchCriteria(golferProfileId, ActiveOnly: true),
            cancellationToken)).ToList();

        var createdClubCount = 0;
        if (clubSummaries.Count == 0)
        {
            var seededClubs = BuildDefaultClubs(golferProfileId);
            foreach (var club in seededClubs)
            {
                await _clubRepository.SaveAsync(club, cancellationToken);
            }

            createdClubCount = seededClubs.Count;
            clubSummaries = (await _clubRepository.SearchAsync(
                new ClubSearchCriteria(golferProfileId, ActiveOnly: true),
                cancellationToken)).ToList();
        }

        if (clubSummaries.Count == 0)
        {
            throw new InvalidOperationException("Unable to seed demo data because no clubs are available.");
        }

        var dominantHand = await LoadDominantHandAsync(golferProfileId, cancellationToken);
        var rng = new Random(unchecked(options.SessionCount * 397 ^ options.WeekSpan * 991));
        var totalDays = Math.Max((options.WeekSpan * 7) - 1, 0);
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(-totalDays);
        var sessionTypes = Enum.GetValues<SessionType>();
        var surfaceBySessionType = new Dictionary<SessionType, SurfaceType>
        {
            [SessionType.DrivingRange] = SurfaceType.Grass,
            [SessionType.IndoorNet] = SurfaceType.IndoorMat,
            [SessionType.OutdoorNet] = SurfaceType.Mat,
            [SessionType.GolfCourse] = SurfaceType.Grass,
            [SessionType.Simulator] = SurfaceType.IndoorMat,
            [SessionType.Fitting] = SurfaceType.Grass,
            [SessionType.Other] = SurfaceType.Unknown,
        };
        var launchMonitorSources = new[] { "Trackman", "Foresight", "Mevo+", "Garmin" };
        var totalShots = 0;

        for (var index = 0; index < options.SessionCount; index++)
        {
            var sessionDate = CalculateSessionDate(startDate, totalDays, options.SessionCount, index);
            var sessionType = sessionTypes[index % sessionTypes.Length];
            var session = BuildPracticeSession(
                golferProfileId,
                sessionDate,
                index,
                sessionType,
                surfaceBySessionType[sessionType],
                launchMonitorSources[index % launchMonitorSources.Length],
                rng);

            await _sessionRepository.SaveAsync(session, cancellationToken);

            var shots = BuildShots(session, clubSummaries, dominantHand, index, rng);
            await _shotRepository.AddRangeAsync(shots, cancellationToken);
            totalShots += shots.Count;
        }

        return new DemoDataSeedResult(createdClubCount, clubSummaries.Count, options.SessionCount, totalShots);
    }

    private static void Validate(DemoDataSeedOptions options)
    {
        if (options.SessionCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.SessionCount), "Session count must be greater than zero.");
        }

        if (options.WeekSpan <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.WeekSpan), "Week span must be greater than zero.");
        }
    }

    private async Task<Guid> LoadDefaultGolferProfileIdAsync(CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id FROM GolferProfiles ORDER BY CreatedAt LIMIT 1;";

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value switch
        {
            Guid guid => guid,
            null => Guid.Empty,
            _ => Guid.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!),
        };
    }

    private async Task<DominantHand> LoadDominantHandAsync(Guid golferProfileId, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT DominantHand
            FROM GolferProfiles
            WHERE Id = $golferProfileId;
            """;
        DuckDbPersistenceHelpers.AddParameter(command, "$golferProfileId", golferProfileId);

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value switch
        {
            int dominantHand => (DominantHand)dominantHand,
            long dominantHand => (DominantHand)(int)dominantHand,
            string dominantHand => Enum.Parse<DominantHand>(dominantHand, ignoreCase: true),
            _ => DominantHand.Right,
        };
    }

    private static IReadOnlyList<Club> BuildDefaultClubs(Guid golferProfileId)
    {
        var now = DateTimeOffset.UtcNow;
        return
        [
            CreateClub(golferProfileId, "Driver", ClubType.Driver, 0, 10.5m, 45.5m, now),
            CreateClub(golferProfileId, "3 Wood", ClubType.FairwayWood, 1, 15m, 43m, now),
            CreateClub(golferProfileId, "5 Wood", ClubType.FairwayWood, 2, 18m, 42m, now),
            CreateClub(golferProfileId, "4 Hybrid", ClubType.Hybrid, 3, 22m, 40m, now),
            CreateClub(golferProfileId, "7 Iron", ClubType.Iron, 4, 34m, 37m, now),
            CreateClub(golferProfileId, "9 Iron", ClubType.Iron, 5, 41m, 35.5m, now),
            CreateClub(golferProfileId, "Pitching Wedge", ClubType.PitchingWedge, 6, 46m, 35m, now),
            CreateClub(golferProfileId, "Sand Wedge", ClubType.SandWedge, 7, 54m, 35m, now),
        ];
    }

    private static Club CreateClub(
        Guid golferProfileId,
        string name,
        ClubType clubType,
        int sortOrder,
        decimal loft,
        decimal lengthYards,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            GolferProfileId = golferProfileId,
            Name = name,
            ClubType = clubType,
            Loft = loft,
            Length = Distance.FromYards(lengthYards),
            IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = now,
            UpdatedAt = now,
        };

    private static PracticeSession BuildPracticeSession(
        Guid golferProfileId,
        DateOnly sessionDate,
        int sessionIndex,
        SessionType sessionType,
        SurfaceType surfaceType,
        string launchMonitorSource,
        Random rng)
    {
        var now = DateTimeOffset.UtcNow;
        var startTime = new TimeOnly(9 + (sessionIndex % 4), (sessionIndex * 7) % 60);
        var durationMinutes = 70 + (sessionIndex % 5) * 10;
        var endTime = startTime.AddMinutes(durationMinutes);
        var sessionName = $"{sessionDate:MMM d} practice {sessionIndex + 1:00}";

        return new PracticeSession
        {
            Id = Guid.NewGuid(),
            GolferProfileId = golferProfileId,
            Name = sessionName,
            SessionDate = sessionDate,
            StartTime = startTime,
            EndTime = endTime,
            LocationName = sessionType is SessionType.GolfCourse ? "Practice range" : "Local range",
            SessionType = sessionType,
            SurfaceType = surfaceType,
            BallType = "Titleist Pro V1",
            LaunchMonitorSource = launchMonitorSource,
            WeatherDescription = PickWeatherDescription(rng),
            TemperatureCelsius = 16m + sessionIndex % 7,
            WindSpeed = Speed.FromMilesPerHour(4m + (sessionIndex % 5)),
            WindDirection = PickWindDirection(rng),
            ElevationMetres = 22m,
            Notes = "Seeded demo data",
            IsArchived = false,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    private static IReadOnlyList<Shot> BuildShots(
        PracticeSession session,
        IReadOnlyList<ClubSummary> clubs,
        DominantHand dominantHand,
        int sessionIndex,
        Random rng)
    {
        var shotCount = 8 + (sessionIndex % 5);
        var sessionDateTime = session.SessionDate.ToDateTime(session.StartTime ?? new TimeOnly(9, 0));
        var shots = new List<Shot>(shotCount);

        for (var shotIndex = 0; shotIndex < shotCount; shotIndex++)
        {
            var club = clubs[(sessionIndex + shotIndex) % clubs.Count];
            var baseCarry = GetBaseCarryYards(club.ClubType);
            var carry = Math.Round(baseCarry + (sessionIndex * 0.5m) + RandomOffset(rng, 7m), 1);
            var target = Math.Round(baseCarry + RandomOffset(rng, 4m), 1);
            var launchDirection = Math.Round(GetDirectionalBias(club.ClubType, dominantHand) + RandomOffset(rng, 1.75m), 1);
            var offlineMagnitude = Math.Round(2m + Math.Abs(RandomOffset(rng, 5m)), 1);
            var isIncluded = rng.NextDouble() > 0.15d;
            var recordedAt = new DateTimeOffset(sessionDateTime.AddMinutes(shotIndex * 3), TimeSpan.Zero);

            shots.Add(new Shot
            {
                Id = Guid.NewGuid(),
                PracticeSessionId = session.Id,
                ClubId = club.Id,
                ShotSequence = shotIndex + 1,
                RecordedAt = recordedAt,
                Source = ShotSourceKind.Manual,
                CarryDistance = Distance.FromYards(carry),
                TotalDistance = Distance.FromYards(carry + 8m + Math.Abs(RandomOffset(rng, 3m))),
                BallSpeed = Speed.FromMilesPerHour(Math.Round(carry * 1.62m + 55m, 1)),
                ClubSpeed = Speed.FromMilesPerHour(Math.Round(carry * 1.05m + 18m, 1)),
                SmashFactor = Math.Round(1.25m + RandomOffset(rng, 0.08m), 2),
                LaunchAngle = Math.Round(10m + RandomOffset(rng, 4m), 1),
                LaunchDirection = launchDirection,
                ApexHeight = Math.Round(18m + RandomOffset(rng, 8m), 1),
                SpinRate = Math.Round(4500m + RandomOffset(rng, 1200m), 0),
                SpinAxis = Math.Round(RandomOffset(rng, 4m), 1),
                OfflineDistance = Distance.FromYards(offlineMagnitude),
                RollDistance = Distance.FromYards(Math.Max(2m, Math.Round((carry * 0.08m) + RandomOffset(rng, 2m), 1))),
                HangTime = Math.Round(3.5m + RandomOffset(rng, 1.2m), 1),
                AttackAngle = Math.Round(-3m + RandomOffset(rng, 2.5m), 1),
                ClubPath = Math.Round(RandomOffset(rng, 2.5m), 1),
                FaceAngle = Math.Round(RandomOffset(rng, 2m), 1),
                FaceToPath = Math.Round(RandomOffset(rng, 1.5m), 1),
                DynamicLoft = Math.Round(18m + RandomOffset(rng, 6m), 1),
                StrikeQuality = rng.NextDouble() > 0.75d ? StrikeQuality.Excellent : StrikeQuality.Good,
                ShotShape = launchDirection >= 0m ? ShotShape.Draw : ShotShape.Fade,
                LieType = "Tee",
                SwingType = GetSwingType(club.ClubType),
                TargetDistance = Distance.FromYards(target),
                IsIncluded = isIncluded,
                ExclusionReason = isIncluded ? null : "Seeded demo data",
                IsEstimated = false,
                Notes = $"Seeded shot {shotIndex + 1:00}",
                RawImportData = null,
                CreatedAt = recordedAt,
                UpdatedAt = recordedAt,
            });
        }

        return shots;
    }

    private static decimal GetBaseCarryYards(ClubType clubType) =>
        clubType switch
        {
            ClubType.Driver => 240m,
            ClubType.FairwayWood => 215m,
            ClubType.Hybrid => 195m,
            ClubType.UtilityIron => 185m,
            ClubType.Iron => 160m,
            ClubType.PitchingWedge => 125m,
            ClubType.GapWedge => 110m,
            ClubType.SandWedge => 85m,
            ClubType.LobWedge => 68m,
            ClubType.Putter => 12m,
            _ => 140m,
        };

    private static decimal GetDirectionalBias(ClubType clubType, DominantHand dominantHand)
    {
        var baseBias = clubType switch
        {
            ClubType.Driver => 1.4m,
            ClubType.FairwayWood => 1.0m,
            ClubType.Hybrid => 0.8m,
            ClubType.UtilityIron => 0.5m,
            ClubType.Iron => -0.4m,
            ClubType.PitchingWedge => -0.6m,
            ClubType.GapWedge => -0.5m,
            ClubType.SandWedge => -0.8m,
            ClubType.LobWedge => -1.0m,
            _ => 0m,
        };

        return dominantHand == DominantHand.Right ? baseBias : -baseBias;
    }

    private static SwingType GetSwingType(ClubType clubType) =>
        clubType switch
        {
            ClubType.PitchingWedge or ClubType.GapWedge or ClubType.SandWedge or ClubType.LobWedge => SwingType.Pitch,
            ClubType.Putter => SwingType.Other,
            _ => SwingType.Full,
        };

    private static DateOnly CalculateSessionDate(DateOnly startDate, int totalDays, int sessionCount, int index)
    {
        if (sessionCount <= 1 || totalDays <= 0)
        {
            return startDate;
        }

        var offset = (int)Math.Round(index * totalDays / (double)(sessionCount - 1));
        return startDate.AddDays(offset);
    }

    private static string PickWeatherDescription(Random rng)
    {
        var weather = new[] { "Clear", "Partly cloudy", "Light breeze", "Overcast", "Mild" };
        return weather[rng.Next(weather.Length)];
    }

    private static string PickWindDirection(Random rng)
    {
        var directions = new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        return directions[rng.Next(directions.Length)];
    }

    private static decimal RandomOffset(Random rng, decimal spread)
    {
        var value = (decimal)(rng.NextDouble() * 2d - 1d);
        return Math.Round(value * spread, 2);
    }
}
