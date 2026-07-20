using System.IO;
using System.Text.Json;

namespace CarryIQ.App;

public sealed record ShotEntryPreferences
{
    public Guid? LastClubId { get; init; }
}

public interface IShotEntryPreferencesStore
{
    Task<ShotEntryPreferences> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(ShotEntryPreferences preferences, CancellationToken cancellationToken);
}

public sealed class JsonShotEntryPreferencesStore : IShotEntryPreferencesStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly IApplicationPaths _applicationPaths;

    public JsonShotEntryPreferencesStore(IApplicationPaths applicationPaths)
    {
        _applicationPaths = applicationPaths;
    }

    public async Task<ShotEntryPreferences> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_applicationPaths.SettingsPath))
        {
            return new ShotEntryPreferences();
        }

        try
        {
            await using var stream = File.OpenRead(_applicationPaths.SettingsPath);
            var preferences = await JsonSerializer.DeserializeAsync<ShotEntryPreferences>(stream, SerializerOptions, cancellationToken);
            return preferences ?? new ShotEntryPreferences();
        }
        catch (JsonException)
        {
            return new ShotEntryPreferences();
        }
    }

    public async Task SaveAsync(ShotEntryPreferences preferences, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_applicationPaths.SettingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(_applicationPaths.SettingsPath);
        await JsonSerializer.SerializeAsync(stream, preferences, SerializerOptions, cancellationToken);
    }
}
