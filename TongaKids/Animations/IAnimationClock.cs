namespace TongaKids.Animations;

public interface IAnimationClock
{
    /// <summary>True when the platform asks for reduced motion.</summary>
    bool ReducedMotion { get; }

    /// <summary>
    /// Ticks with progress from 0 to 1 over the duration. Calling Start again
    /// cancels any run in progress.
    /// </summary>
    void Start(Action<double> onTick, TimeSpan duration, Action? onComplete = null);

    void Stop();

    /// <summary>Jumps to the end. Used when a child taps to skip.</summary>
    void SkipToEnd();
}
