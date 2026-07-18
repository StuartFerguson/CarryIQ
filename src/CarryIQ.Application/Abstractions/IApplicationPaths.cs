namespace CarryIQ.Application;

/// <summary>
/// Provides the application data locations used by CarryIQ.
/// </summary>
public interface IApplicationPaths
{
    string DataDirectory { get; }

    string DatabasePath { get; }

    string SettingsPath { get; }

    string LogsDirectory { get; }

    string BackupsDirectory { get; }
}
