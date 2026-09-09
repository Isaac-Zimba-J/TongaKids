namespace TongaKids.Services;

/// <summary>One clip a guardian can record, and whether it exists yet.</summary>
public sealed record ClipToRecord(
    string AudioKey,
    string Say,
    string Context,
    bool IsRecorded);

public interface IRecordingService
{
    /// <summary>Every clip the content references, in the order a person would record them.</summary>
    Task<List<ClipToRecord>> GetClipsAsync();

    bool IsRecording { get; }

    /// <summary>Asks for the microphone. False if the person declined.</summary>
    Task<bool> EnsurePermissionAsync();

    /// <summary>Longest take allowed, after which recording stops on its own.</summary>
    TimeSpan MaxDuration { get; }

    /// <summary>The key currently being recorded, or null.</summary>
    string? CurrentKey { get; }

    Task<bool> StartAsync(string audioKey);

    /// <summary>Stops and throws the take away, leaving any saved clip untouched.</summary>
    Task CancelAsync();

    /// <summary>Stops and saves. False if nothing usable was captured.</summary>
    Task<bool> StopAsync();

    /// <summary>Removes a recording so it can be redone.</summary>
    Task DeleteAsync(string audioKey);
}
