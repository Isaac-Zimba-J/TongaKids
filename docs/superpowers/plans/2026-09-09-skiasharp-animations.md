# TongaKids Read — Plan 3: SkiaSharp Animations

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a small number of high-payoff animations with SkiaSharp — celebration, answer feedback, and progress — without compromising the app's commitment to low-specification devices.

**Architecture:** SkiaSharp draws into a `SKCanvasView` layered over existing MAUI content. Each animation is a self-contained drawable driven by a single elapsed-time value, so it can be started, stopped, and skipped. No animation is on the critical path: every screen works identically with animations disabled.

**Tech Stack:** `SkiaSharp.Views.Maui.Controls` 4.151.2 on top of the Plan 1 stack.

**Spec:** `docs/superpowers/specs/2026-09-07-tongakids-read-design.md`

**Depends on:** Plan 1 complete (Tasks 1–16). Independent of Plan 2.

---

## Read this before starting

### This is polish, and it is not the priority

The report needs UC-02 (stories), UC-04 (progress) and UC-05 to UC-07 (parental
gateway) to be demonstrable. None of them exists yet; they are Plan 2. **Plan 2 comes
first.** An examiner asking "show me the parental gateway" will not be consoled by a
star animation.

Do this plan only when Plan 2 is done, or when a specific animation is worth more than
the feature it displaces. If the deadline is tight, Task 1 alone (the celebration) buys
most of the emotional payoff for the least work.

### The cost this adds

The spec commits to low-specification devices and a small APK (§11, §14). SkiaSharp is
not free:

- It ships a native library per ABI. On a single-ABI arm64 release build expect roughly
  **+3 to 5 MB**, more if you build all ABIs.
- It takes the package count from three to four, which the Plan 1 global constraints
  explicitly guard against.

That trade is defensible for the celebration animation, which is the single moment a
child most wants to feel rewarded. It is much harder to defend for decorative motion.
**Do not add SkiaSharp for ambient decoration.**

### Rules for every animation here

- **60fps is not the target; not dropping frames is.** Cap invalidation to ~30fps on a
  timer rather than redrawing as fast as possible.
- **Every animation is skippable.** A tap during a celebration jumps to the end state.
  Children repeat lessons; an unskippable two-second flourish becomes an obstacle by the
  tenth run.
- **Nothing blocks navigation.** Animations never gate a Continue button.
- **Stop on disappear.** Every timer is cancelled in `OnDisappearing`, or it keeps
  waking the CPU on a page nobody is looking at — the fastest way to drain a phone.
- **Respect reduced motion.** Read the setting once and fall straight to the end state.
- Colours come from `Colors.xaml` as always. Pass them into the drawable; never hardcode.

---

## Global Constraints

All Plan 1 global constraints carry over. In addition:

- Exactly one new package: `SkiaSharp.Views.Maui.Controls`. Do not add
  `SkiaSharp.Extended.UI.Maui` (Lottie) as well — it pulls a second native dependency for
  a feature this plan does not need.
- No animation may allocate inside its draw loop. Build paths and paints once, reuse them.
- Every `SKCanvasView` sits in a `Grid` cell above existing content with
  `InputTransparent="True"` unless it is itself interactive.

---

### Task 1: Animation infrastructure and the lesson-complete celebration

**Files:**
- Modify: `TongaKids/TongaKids.csproj`, `TongaKids/MauiProgram.cs`
- Create: `TongaKids/Animations/IAnimationClock.cs`, `AnimationClock.cs`
- Create: `TongaKids/Animations/StarBurstDrawable.cs`
- Create: `TongaKids/Controls/CelebrationView.cs`
- Modify: `TongaKids/Views/LessonCompletePage.xaml(.cs)`

**Interfaces:**
- Produces:
  - `IAnimationClock` with `bool ReducedMotion { get; }`, `void Start(Action<double> onTick, TimeSpan duration, Action? onComplete = null)`, `void Stop()`, `void SkipToEnd()`
  - `StarBurstDrawable` with `void Draw(SKCanvas canvas, SKRect bounds, double progress, int starCount)`
  - `CelebrationView : ContentView` with `int StarCount`, `void Play()`, `void Skip()`

- [ ] **Step 1: Add the package**

In `TongaKids/TongaKids.csproj`, alongside the existing three:

```xml
<PackageReference Include="SkiaSharp.Views.Maui.Controls" Version="4.151.2" />
```

In `MauiProgram.cs`, chain the initialiser after `.UseMauiApp<App>()`:

```csharp
.UseSkiaSharp()
```

Add `using SkiaSharp.Views.Maui.Controls.Hosting;`.

Run: `dotnet build TongaKids/TongaKids.csproj -f net10.0-android -p:RuntimeIdentifier=android-arm64 -v minimal`
Expected: `Build succeeded`.

- [ ] **Step 2: Write the clock**

One timer implementation, shared by every animation, so frame rate and the reduced-motion
rule are decided in exactly one place.

`TongaKids/Animations/IAnimationClock.cs`:

```csharp
namespace TongaKids.Animations;

public interface IAnimationClock
{
    /// <summary>True when the platform asks for reduced motion.</summary>
    bool ReducedMotion { get; }

    /// <summary>
    /// Ticks <paramref name="onTick"/> with progress from 0 to 1 over the duration.
    /// Calling Start again cancels any run in progress.
    /// </summary>
    void Start(Action<double> onTick, TimeSpan duration, Action? onComplete = null);

    void Stop();

    /// <summary>Jumps straight to progress 1 and completes. Used when a child taps to skip.</summary>
    void SkipToEnd();
}
```

`TongaKids/Animations/AnimationClock.cs`:

```csharp
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
        ReducedMotion = false;
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
```

Register in `MauiProgram.cs`. Transient, because each page needs its own:

```csharp
builder.Services.AddTransient<IAnimationClock, AnimationClock>();
```

- [ ] **Step 3: Write the star burst drawable**

Pure drawing, no timer and no MAUI types beyond colours, so it can be reasoned about and
later verified independently.

`TongaKids/Animations/StarBurstDrawable.cs`:

```csharp
using SkiaSharp;

namespace TongaKids.Animations;

/// <summary>
/// Stars fly outward from the centre, spinning and fading. One drawable instance
/// per view; paths and paints are built once and reused every frame, because
/// allocating inside a draw loop is what makes Skia stutter on cheap hardware.
/// </summary>
public sealed class StarBurstDrawable
{
    private const int MaxStars = 24;

    private readonly SKPath _star = BuildStar();
    private readonly SKPaint _paint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Fill
    };

    private readonly (float Angle, float Distance, float Spin, float Scale)[] _seeds;

    public StarBurstDrawable(int seed = 7)
    {
        var rng = new Random(seed);
        _seeds = new (float, float, float, float)[MaxStars];
        for (var i = 0; i < MaxStars; i++)
        {
            _seeds[i] = (
                Angle: (float)(rng.NextDouble() * Math.PI * 2),
                Distance: 0.35f + (float)rng.NextDouble() * 0.55f,
                Spin: (float)(rng.NextDouble() * 720 - 360),
                Scale: 0.5f + (float)rng.NextDouble() * 0.8f);
        }
    }

    /// <param name="progress">0 to 1.</param>
    /// <param name="starCount">How many stars the child earned; more stars, more burst.</param>
    public void Draw(SKCanvas canvas, SKRect bounds, double progress, int starCount,
        SKColor primary, SKColor secondary)
    {
        canvas.Clear(SKColors.Transparent);

        if (progress <= 0)
        {
            return;
        }

        var count = Math.Clamp(starCount * 4, 4, MaxStars);
        var centre = new SKPoint(bounds.MidX, bounds.MidY);
        var reach = Math.Min(bounds.Width, bounds.Height) * 0.5f;

        // Ease out: fast burst, slow drift.
        var eased = (float)(1 - Math.Pow(1 - progress, 3));
        var fade = (float)Math.Clamp(1.0 - Math.Max(0, progress - 0.6) / 0.4, 0, 1);

        for (var i = 0; i < count; i++)
        {
            var (angle, distance, spin, scale) = _seeds[i];

            var x = centre.X + (float)Math.Cos(angle) * reach * distance * eased;
            var y = centre.Y + (float)Math.Sin(angle) * reach * distance * eased;

            _paint.Color = (i % 2 == 0 ? primary : secondary)
                .WithAlpha((byte)(255 * fade));

            canvas.Save();
            canvas.Translate(x, y);
            canvas.RotateDegrees(spin * eased);
            canvas.Scale(scale * (0.6f + 0.4f * eased) * reach * 0.06f);
            canvas.DrawPath(_star, _paint);
            canvas.Restore();
        }
    }

    /// <summary>A five-point star on a 1x1 canvas centred at the origin.</summary>
    private static SKPath BuildStar()
    {
        var path = new SKPath();
        const int points = 5;
        const float outer = 1f;
        const float inner = 0.42f;

        for (var i = 0; i < points * 2; i++)
        {
            var radius = i % 2 == 0 ? outer : inner;
            var angle = Math.PI / points * i - Math.PI / 2;
            var x = (float)(Math.Cos(angle) * radius);
            var y = (float)(Math.Sin(angle) * radius);

            if (i == 0)
            {
                path.MoveTo(x, y);
            }
            else
            {
                path.LineTo(x, y);
            }
        }

        path.Close();
        return path;
    }
}
```

- [ ] **Step 4: Write the celebration view**

`TongaKids/Controls/CelebrationView.cs`:

```csharp
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using TongaKids.Animations;

namespace TongaKids.Controls;

/// <summary>
/// A transparent overlay that bursts stars once. Tapping skips to the end.
/// Sits above page content and never blocks it.
/// </summary>
public class CelebrationView : ContentView
{
    public static readonly BindableProperty StarCountProperty =
        BindableProperty.Create(nameof(StarCount), typeof(int), typeof(CelebrationView), 3);

    public int StarCount
    {
        get => (int)GetValue(StarCountProperty);
        set => SetValue(StarCountProperty, value);
    }

    private readonly SKCanvasView _canvas = new();
    private readonly StarBurstDrawable _drawable = new();
    private readonly IAnimationClock _clock;

    private double _progress;
    private SKColor _primary = SKColors.Orange;
    private SKColor _secondary = SKColors.Gold;

    public CelebrationView(IAnimationClock clock)
    {
        _clock = clock;

        InputTransparent = true;
        Content = _canvas;
        _canvas.PaintSurface += OnPaintSurface;

        ResolveColors();
    }

    private void ResolveColors()
    {
        if (Application.Current?.Resources.TryGetValue("PrimaryContainer", out var p) == true
            && p is Color primary)
        {
            _primary = primary.ToSKColor();
        }

        if (Application.Current?.Resources.TryGetValue("TertiaryContainer", out var s) == true
            && s is Color secondary)
        {
            _secondary = secondary.ToSKColor();
        }
    }

    public void Play()
    {
        _clock.Start(
            progress =>
            {
                _progress = progress;
                _canvas.InvalidateSurface();
            },
            TimeSpan.FromMilliseconds(1400));
    }

    public void Skip() => _clock.SkipToEnd();

    public void Stop() => _clock.Stop();

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        _drawable.Draw(
            e.Surface.Canvas,
            new SKRect(0, 0, e.Info.Width, e.Info.Height),
            _progress,
            StarCount,
            _primary,
            _secondary);
    }
}
```

- [ ] **Step 5: Put it on the lesson-complete page**

In `LessonCompletePage.xaml`, wrap the existing `VerticalStackLayout` in a `Grid` and add
the overlay as a second child so it paints on top:

```xml
<Grid>
    <VerticalStackLayout Padding="{StaticResource ScreenPadding}" Spacing="24"
                         VerticalOptions="Center">
        <!-- existing content unchanged -->
    </VerticalStackLayout>

    <ContentView x:Name="CelebrationHost" InputTransparent="True" />
</Grid>
```

In `LessonCompletePage.xaml.cs`:

```csharp
using TongaKids.Animations;
using TongaKids.Controls;
using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class LessonCompletePage : ContentPage
{
    private readonly LessonCompleteViewModel _viewModel;
    private readonly CelebrationView _celebration;

    public LessonCompletePage(LessonCompleteViewModel viewModel, IAnimationClock clock)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        _celebration = new CelebrationView(clock);
        CelebrationHost.Content = _celebration;

        // Tapping anywhere skips the flourish. A child repeating a lesson should
        // never have to wait through it again.
        var skip = new TapGestureRecognizer();
        skip.Tapped += (_, _) => _celebration.Skip();
        GestureRecognizers.Add(skip);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Only celebrate mastery. A "Good try!" result gets a calm screen.
        if (_viewModel.IsMastered)
        {
            _celebration.StarCount = _viewModel.Stars;
            _celebration.Play();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Always stop: a timer left running keeps waking the CPU.
        _celebration.Stop();
    }
}
```

- [ ] **Step 6: Verify on device**

Run: `dotnet build TongaKids/TongaKids.csproj -f net10.0-android -p:RuntimeIdentifier=android-arm64 -t:Run -v minimal`

Confirm each:

1. Answer a lesson's quiz correctly. Stars burst outward from the centre, spin, and fade
   over about 1.4 seconds.
2. Tap during the burst: it jumps immediately to the end. Nothing is left half-drawn.
3. Score below 80%. The result screen shows **no** burst — "Good try!" is calm.
4. Press Continue while the burst is running. Navigation is immediate.
5. In Android developer options set "Animator duration scale" to Off, then re-run a
   mastered lesson. No burst, no delay, and no crash.
6. Watch `adb logcat` while leaving the page. No timer messages continue afterwards.

- [ ] **Step 7: Check what it cost**

```bash
ls -la TongaKids/bin/Debug/net10.0-android/android-arm64/*-Signed.apk
```

Compare against the size recorded before this task. Note the delta in the commit message.
If it exceeds about 6 MB, say so rather than letting it pass silently — the spec's
low-specification commitment is a promise to real users on real devices.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "Add SkiaSharp celebration animation on lesson mastery

A shared 30fps clock honours the platform's reduced-motion setting and
stops on disappear. The burst plays only on mastery, is skippable by tap,
and never gates navigation.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 2: Answer feedback in the matching game

**Files:**
- Create: `TongaKids/Animations/AnswerFeedbackDrawable.cs`
- Modify: `TongaKids/Controls/` (new `AnswerFeedbackView.cs`)
- Modify: `TongaKids/Views/MatchingGamePage.xaml(.cs)`, `TongaKids/ViewModels/MatchingGameViewModel.cs`

**Interfaces:**
- Consumes: `IAnimationClock`.
- Produces: `MatchingGameViewModel.LastAnswerWasCorrect` (bool?) and an
  `AnswerSubmitted` event the page subscribes to.

Right now Submit moves straight to the next question with no feedback at all. A child
learns far more from a half-second of "yes, that one" than from a score at the end. This
is the animation with the clearest pedagogical justification in the plan.

- [ ] **Step 1: Surface the outcome from the view model**

In `MatchingGameViewModel`, add:

```csharp
/// <summary>Raised after each submission so the view can show feedback.</summary>
public event EventHandler<bool>? AnswerSubmitted;
```

In `SubmitAsync`, replace the scoring block so the outcome is announced before advancing:

```csharp
var wasCorrect = selected.Item.Id == _questions[_questionIndex].Target.Id;
if (wasCorrect)
{
    _correctCount++;
}

AnswerSubmitted?.Invoke(this, wasCorrect);

// Let the feedback land before moving on.
await Task.Delay(600);

_questionIndex++;
```

- [ ] **Step 2: Write the feedback drawable**

`TongaKids/Animations/AnswerFeedbackDrawable.cs`. A tick or a cross drawn with a stroke
that draws itself on, centred and fading out.

```csharp
using SkiaSharp;

namespace TongaKids.Animations;

/// <summary>
/// A tick or a cross that strokes itself on then fades. Deliberately quiet:
/// a wrong answer must read as information, never as punishment.
/// </summary>
public sealed class AnswerFeedbackDrawable
{
    private readonly SKPaint _paint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeCap = SKStrokeCap.Round,
        StrokeJoin = SKStrokeJoin.Round
    };

    public void Draw(SKCanvas canvas, SKRect bounds, double progress,
        bool isCorrect, SKColor correctColor, SKColor wrongColor)
    {
        canvas.Clear(SKColors.Transparent);

        if (progress <= 0)
        {
            return;
        }

        var size = Math.Min(bounds.Width, bounds.Height) * 0.28f;
        var cx = bounds.MidX;
        var cy = bounds.MidY;

        var fade = (float)Math.Clamp(1.0 - Math.Max(0, progress - 0.65) / 0.35, 0, 1);
        var draw = (float)Math.Clamp(progress / 0.45, 0, 1);

        _paint.Color = (isCorrect ? correctColor : wrongColor).WithAlpha((byte)(255 * fade));
        _paint.StrokeWidth = size * 0.16f;

        using var path = new SKPath();

        if (isCorrect)
        {
            // Tick: down-left stroke, then up-right, revealed by draw progress.
            var p0 = new SKPoint(cx - size * 0.6f, cy);
            var p1 = new SKPoint(cx - size * 0.15f, cy + size * 0.45f);
            var p2 = new SKPoint(cx + size * 0.65f, cy - size * 0.5f);

            path.MoveTo(p0);
            if (draw <= 0.5f)
            {
                path.LineTo(Lerp(p0, p1, draw / 0.5f));
            }
            else
            {
                path.LineTo(p1);
                path.LineTo(Lerp(p1, p2, (draw - 0.5f) / 0.5f));
            }
        }
        else
        {
            // Cross: both strokes grow together.
            var r = size * 0.5f;
            path.MoveTo(cx - r, cy - r);
            path.LineTo(cx - r + 2 * r * draw, cy - r + 2 * r * draw);
            path.MoveTo(cx + r, cy - r);
            path.LineTo(cx + r - 2 * r * draw, cy - r + 2 * r * draw);
        }

        canvas.DrawPath(path, _paint);
    }

    private static SKPoint Lerp(SKPoint a, SKPoint b, float t) =>
        new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
}
```

- [ ] **Step 3: Write the feedback view**

`TongaKids/Controls/AnswerFeedbackView.cs`. Same shape as `CelebrationView`: an
`SKCanvasView` inside a `ContentView`, an `IAnimationClock`, `InputTransparent = true`.
Resolve `Tertiary` for correct and `Error` for wrong from application resources exactly as
`CelebrationView.ResolveColors` does, and expose:

```csharp
public void Show(bool isCorrect)
{
    _isCorrect = isCorrect;
    _clock.Start(
        progress =>
        {
            _progress = progress;
            _canvas.InvalidateSurface();
        },
        TimeSpan.FromMilliseconds(600));
}

public void Stop() => _clock.Stop();
```

- [ ] **Step 4: Host it on the game page**

Wrap the game's root `Grid` content and add the overlay as the last child, then in
`MatchingGamePage.xaml.cs`:

```csharp
public MatchingGamePage(MatchingGameViewModel viewModel, IAnimationClock clock)
{
    InitializeComponent();
    BindingContext = _viewModel = viewModel;

    _feedback = new AnswerFeedbackView(clock);
    FeedbackHost.Content = _feedback;

    _viewModel.AnswerSubmitted += OnAnswerSubmitted;
}

private void OnAnswerSubmitted(object? sender, bool wasCorrect) =>
    _feedback.Show(wasCorrect);

protected override void OnDisappearing()
{
    base.OnDisappearing();
    _viewModel.AnswerSubmitted -= OnAnswerSubmitted;
    _feedback.Stop();
}
```

Unsubscribing matters: the page is transient, and a live handler on a retained view model
would keep it alive.

- [ ] **Step 5: Verify on device**

1. Answer correctly. A green tick strokes on over the cards and fades; the next question
   follows.
2. Answer wrongly. A red cross, same timing. **No sound, no shake, no "wrong" text** — it
   informs without scolding.
3. Complete a full quiz. The pacing feels deliberate rather than sluggish; if it drags,
   reduce the delay in Step 1 rather than the animation duration.
4. Leave the page mid-feedback. No crash, no lingering timer.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "Add answer feedback to the matching game

A tick or cross strokes on for 600ms after each submission. Previously the
game advanced with no feedback at all, which is the moment a child learns
most. The wrong-answer treatment is deliberately quiet.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 3: Animated progress ring on the levels screen

**Files:**
- Create: `TongaKids/Animations/ProgressRingDrawable.cs`
- Create: `TongaKids/Controls/ProgressRing.cs`
- Modify: `TongaKids/Views/PhonicsLevelsPage.xaml`

**Interfaces:**
- Produces: `ProgressRing : ContentView` with `double Progress` (0–1), `double RingSize`,
  and animation on progress change.

DESIGN.md's "Progress & Rewards" calls for a ring. The levels screen currently uses a flat
`ProgressBar`. This replaces it on the *current* level card only — completed and locked
cards do not need one.

- [ ] **Step 1: Write the drawable**

```csharp
using SkiaSharp;

namespace TongaKids.Animations;

/// <summary>A rounded progress arc that sweeps from the top clockwise.</summary>
public sealed class ProgressRingDrawable
{
    private readonly SKPaint _track = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeCap = SKStrokeCap.Round
    };

    private readonly SKPaint _fill = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeCap = SKStrokeCap.Round
    };

    public void Draw(SKCanvas canvas, SKRect bounds, double progress,
        SKColor trackColor, SKColor fillColor)
    {
        canvas.Clear(SKColors.Transparent);

        var stroke = Math.Min(bounds.Width, bounds.Height) * 0.12f;
        var inset = stroke / 2 + 1;
        var rect = new SKRect(
            bounds.Left + inset, bounds.Top + inset,
            bounds.Right - inset, bounds.Bottom - inset);

        _track.StrokeWidth = stroke;
        _track.Color = trackColor;
        canvas.DrawOval(rect, _track);

        if (progress <= 0)
        {
            return;
        }

        _fill.StrokeWidth = stroke;
        _fill.Color = fillColor;

        using var path = new SKPath();
        path.AddArc(rect, -90, (float)(360 * Math.Clamp(progress, 0, 1)));
        canvas.DrawPath(path, _fill);
    }
}
```

- [ ] **Step 2: Write the control**

`ProgressRing : ContentView` holding an `SKCanvasView` and an `IAnimationClock`. On
`Progress` change, animate from the previous value to the new one over 500ms, invalidating
each tick. Resolve `SurfaceContainerHighest` for the track and `Tertiary` for the fill.

Because `ProgressRing` is created from XAML it cannot take `IAnimationClock` by
constructor injection; instantiate `new AnimationClock()` inside it and note why in a
comment.

- [ ] **Step 3: Use it on the current level card**

In `PhonicsLevelsPage.xaml`, replace the `ProgressBar` inside the item template with:

```xml
<controls:ProgressRing Progress="{Binding ProgressFraction}"
                       RingSize="56"
                       IsVisible="{Binding IsCurrent}"
                       HorizontalOptions="End" />
```

- [ ] **Step 4: Verify**

1. The current level shows a ring that sweeps to its percentage on appearing.
2. Complete a lesson and return: the ring animates from the old value to the new one, not
   from zero.
3. Completed and locked cards show no ring.
4. Scroll the list quickly. No stutter; rings off-screen are not redrawn.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "Add animated progress ring to the current level card

Implements the DESIGN.md progress ring, replacing the flat bar on the
current level only. Animates from the previous value so returning from a
completed lesson reads as advancement.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Definition of done

- [ ] Mastering a lesson bursts stars; scoring below 80% does not.
- [ ] Every animation can be skipped by tapping, and none gates navigation.
- [ ] Turning off animator duration scale disables all three cleanly.
- [ ] Leaving any animated page stops its timer — verified in logcat.
- [ ] APK growth from SkiaSharp is measured and recorded in the commit message.
- [ ] The app remains fully usable with animations doing nothing at all.

## Explicitly not in this plan

- **Lottie.** `SkiaSharp.Extended.UI.Maui` pulls a second native dependency, and hand-drawn
  Skia covers everything here at a fraction of the size.
- **Animated page transitions.** Shell's defaults are fine and replacing them is a large
  surface for little gain.
- **Particle effects on the splash screen.** Decoration on a screen shown for 1.2 seconds
  is the worst possible return on APK size.
- **Animating the phonics grapheme.** The letter is the thing being learned; moving it
  competes with reading it.
