namespace CarryIQ.Application;

public static class SessionRules
{
    public static IReadOnlyList<string> Validate(PracticeSession session)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(session.Name))
        {
            errors.Add("Session name is required.");
        }

        if (session.StartTime is TimeOnly startTime && session.EndTime is TimeOnly endTime && endTime < startTime)
        {
            errors.Add("End time must be after start time.");
        }

        return errors;
    }
}
