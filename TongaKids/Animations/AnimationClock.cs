namespace TongaKids.Animations;

/// <summary>
/// Drives animations at a capped frame rate. 30fps is deliberate: the spec
/// targets low-specification devices, where chasing 60fps drops frames and
/// looks worse than a steady 30.
/// </summary>
public sealed class AnimationClock : IAnimationClock
{
    private const int FramesPerSecond = 30;

    private IDispatcherTimer? _timer;
    private Action<double>? _onTick;
    private Action? _onComplete;
    private DateTime _startedAt;
    private TimeSpan _duration;

    public bool ReducedMotion { get; }

    public AnimationClock()
    {
        // Read once: this does not change while the app is running.
#if ANDROID
        try
        {
            var scale = Android.Provider.Settings.Global.GetFloat(
                Android.App.Application.Context.ContentResolver,
                Android.Provider.Settings.Global.AnimatorDurationScale, 1f);
            ReducedMotion = scale == 0f;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Clock] motion setting unavailable: {ex.Message}");
        }
#endif
    }

    public void Start(Action<double> onTick, TimeSpan duration, Action? onComplete = null)
    {
        Stop();

        _onTick = onTick;
        _onComplete = onComplete;
        _duration = duration;

        if (ReducedMotion || duration <= TimeSpan.Zero)
        {
            SkipToEnd();
            return;
        }

        _startedAt = DateTime.UtcNow;

        _timer = Application.Current?.Dispatcher.CreateTimer();
        if (_timer is null)
        {
            SkipToEnd();
            return;
        }

        _timer.Interval = TimeSpan.FromMilliseconds(1000.0 / FramesPerSecond);
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        var elapsed = DateTime.UtcNow - _startedAt;
        var progress = Math.Clamp(elapsed.TotalMilliseconds / _duration.TotalMilliseconds, 0, 1);

        _onTick?.Invoke(progress);

        if (progress >= 1)
        {
            var done = _onComplete;
            Stop();
            done?.Invoke();
        }
    }

    public void SkipToEnd()
    {
        var tick = _onTick;
        var done = _onComplete;
        Stop();
        tick?.Invoke(1.0);
        done?.Invoke();
    }

    public void Stop()
    {
        if (_timer is not null)
        {
            _timer.Tick -= OnTimerTick;
            _timer.Stop();
            _timer = null;
        }
    }
}
