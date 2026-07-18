namespace CarryIQ.Domain;

public enum ClubType
{
    Driver,
    FairwayWood,
    Hybrid,
    UtilityIron,
    Iron,
    PitchingWedge,
    GapWedge,
    SandWedge,
    LobWedge,
    Putter,
    Other,
}

public enum SessionType
{
    DrivingRange,
    IndoorNet,
    OutdoorNet,
    GolfCourse,
    Simulator,
    Fitting,
    Other,
}

public enum SurfaceType
{
    Grass,
    Mat,
    IndoorMat,
    Unknown,
}

public enum StrikeQuality
{
    Unknown,
    Poor,
    BelowAverage,
    Average,
    Good,
    Excellent,
}

public enum ShotShape
{
    Unknown,
    Straight,
    Push,
    Pull,
    Fade,
    Draw,
    Slice,
    Hook,
    PushFade,
    PushDraw,
    PullFade,
    PullDraw,
}

public enum SwingType
{
    Full,
    ThreeQuarter,
    Half,
    Quarter,
    Pitch,
    Chip,
    Punch,
    Other,
}

public enum ShotSourceKind
{
    Manual,
    GenericCsv,
    SwingLogicSlx,
    Unknown,
}
