CREATE TABLE IF NOT EXISTS SchemaVersion (
    Version INTEGER NOT NULL,
    AppliedAtUtc TIMESTAMP NOT NULL
);

CREATE TABLE IF NOT EXISTS GolferProfiles (
    Id UUID PRIMARY KEY,
    DisplayName TEXT NOT NULL,
    HandicapIndex DECIMAL(6, 2) NULL,
    DominantHand INTEGER NOT NULL,
    DefaultDistanceUnit INTEGER NOT NULL,
    DefaultSpeedUnit INTEGER NOT NULL,
    DefaultTemperatureUnit INTEGER NOT NULL,
    CreatedAt TIMESTAMP NOT NULL,
    UpdatedAt TIMESTAMP NOT NULL
);

CREATE TABLE IF NOT EXISTS Clubs (
    Id UUID PRIMARY KEY,
    GolferProfileId UUID NOT NULL,
    Name TEXT NOT NULL,
    ClubType INTEGER NOT NULL,
    Manufacturer TEXT NULL,
    Model TEXT NULL,
    Loft DECIMAL(6, 2) NULL,
    Shaft TEXT NULL,
    ShaftFlex TEXT NULL,
    LengthYards DOUBLE NULL,
    IsActive BOOLEAN NOT NULL,
    SortOrder INTEGER NOT NULL,
    Notes TEXT NULL,
    CreatedAt TIMESTAMP NOT NULL,
    UpdatedAt TIMESTAMP NOT NULL
);

CREATE TABLE IF NOT EXISTS PracticeSessions (
    Id UUID PRIMARY KEY,
    GolferProfileId UUID NOT NULL,
    Name TEXT NOT NULL,
    SessionDate DATE NOT NULL,
    StartTime TIME NULL,
    EndTime TIME NULL,
    LocationName TEXT NULL,
    SessionType INTEGER NOT NULL,
    SurfaceType INTEGER NOT NULL,
    BallType TEXT NULL,
    LaunchMonitorSource TEXT NULL,
    WeatherDescription TEXT NULL,
    TemperatureCelsius DOUBLE NULL,
    WindSpeedMilesPerHour DOUBLE NULL,
    WindDirection TEXT NULL,
    ElevationMetres DOUBLE NULL,
    Notes TEXT NULL,
    CreatedAt TIMESTAMP NOT NULL,
    UpdatedAt TIMESTAMP NOT NULL
);

CREATE TABLE IF NOT EXISTS Shots (
    Id UUID PRIMARY KEY,
    PracticeSessionId UUID NOT NULL,
    ClubId UUID NOT NULL,
    ShotSequence INTEGER NOT NULL,
    RecordedAt TIMESTAMP NOT NULL,
    Source INTEGER NOT NULL,
    CarryDistanceYards DOUBLE NULL,
    TotalDistanceYards DOUBLE NULL,
    BallSpeedMilesPerHour DOUBLE NULL,
    ClubSpeedMilesPerHour DOUBLE NULL,
    SmashFactor DOUBLE NULL,
    LaunchAngle DOUBLE NULL,
    LaunchDirection DOUBLE NULL,
    ApexHeight DOUBLE NULL,
    SpinRate DOUBLE NULL,
    SpinAxis DOUBLE NULL,
    OfflineDistanceYards DOUBLE NULL,
    RollDistanceYards DOUBLE NULL,
    HangTime DOUBLE NULL,
    AttackAngle DOUBLE NULL,
    ClubPath DOUBLE NULL,
    FaceAngle DOUBLE NULL,
    FaceToPath DOUBLE NULL,
    DynamicLoft DOUBLE NULL,
    StrikeQuality INTEGER NULL,
    ShotShape INTEGER NULL,
    LieType TEXT NULL,
    SwingType INTEGER NULL,
    TargetDistanceYards DOUBLE NULL,
    IsIncluded BOOLEAN NOT NULL,
    ExclusionReason TEXT NULL,
    IsEstimated BOOLEAN NOT NULL,
    Notes TEXT NULL,
    RawImportData TEXT NULL,
    CreatedAt TIMESTAMP NOT NULL,
    UpdatedAt TIMESTAMP NOT NULL
);

CREATE TABLE IF NOT EXISTS WedgeSwingReferences (
    Id UUID PRIMARY KEY,
    GolferProfileId UUID NOT NULL,
    ClubId UUID NOT NULL,
    SwingLabel TEXT NOT NULL,
    SwingType INTEGER NOT NULL,
    ClockPosition TEXT NULL,
    TargetDistanceYards DOUBLE NULL,
    AverageCarryYards DOUBLE NULL,
    CarryStandardDeviationYards DOUBLE NULL,
    SampleSize INTEGER NOT NULL,
    IsManualOverride BOOLEAN NOT NULL,
    UpdatedAt TIMESTAMP NOT NULL
);

CREATE TABLE IF NOT EXISTS ImportJobs (
    Id UUID PRIMARY KEY,
    FileName TEXT NOT NULL,
    Importer TEXT NOT NULL,
    StartedAtUtc TIMESTAMP NOT NULL,
    CompletedAtUtc TIMESTAMP NULL,
    Status TEXT NOT NULL,
    RowsRead INTEGER NOT NULL,
    RowsImported INTEGER NOT NULL,
    RowsSkipped INTEGER NOT NULL,
    RowsFailed INTEGER NOT NULL,
    PracticeSessionId UUID NULL,
    ErrorSummary TEXT NULL
);

CREATE TABLE IF NOT EXISTS ImportErrors (
    Id UUID PRIMARY KEY,
    ImportJobId UUID NOT NULL,
    RowNumber INTEGER NOT NULL,
    FieldName TEXT NOT NULL,
    Message TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS SavedMappings (
    Id UUID PRIMARY KEY,
    Name TEXT NOT NULL,
    Importer TEXT NOT NULL,
    MappingJson TEXT NOT NULL,
    CreatedAtUtc TIMESTAMP NOT NULL,
    UpdatedAtUtc TIMESTAMP NOT NULL
);

CREATE TABLE IF NOT EXISTS ApplicationSettings (
    Id UUID PRIMARY KEY,
    SettingsJson TEXT NOT NULL,
    SchemaVersion INTEGER NOT NULL,
    UpdatedAtUtc TIMESTAMP NOT NULL
);

CREATE TABLE IF NOT EXISTS Backups (
    Id UUID PRIMARY KEY,
    BackupPath TEXT NOT NULL,
    CreatedAtUtc TIMESTAMP NOT NULL,
    Notes TEXT NULL
);

INSERT INTO SchemaVersion (Version, AppliedAtUtc)
SELECT 1, CURRENT_TIMESTAMP
WHERE NOT EXISTS (SELECT 1 FROM SchemaVersion);
