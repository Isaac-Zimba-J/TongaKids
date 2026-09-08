# TongaKids Read — Plan 1: Foundation and Phonics Core Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a running .NET MAUI Android app in which a child picks a profile, opens a Chitonga phonics lesson, hears a syllable, plays a matching game, and gets a star result that unlocks the next lesson.

**Architecture:** Single MAUI project, MVVM via CommunityToolkit.Mvvm, strictly one-way dependencies `Views → ViewModels → Services → Data`. All learning content lives in a versioned JSON pack seeded into SQLite on first launch. The three logic engines (PhonicsEngine, ProgressCalculator, MasteryEvaluator) are pure classes with no MAUI and no SQLite references.

**Tech Stack:** .NET 10, .NET MAUI 10, C#, XAML, `CommunityToolkit.Mvvm`, `sqlite-net-pcl` + `SQLitePCLRaw.bundle_green`, `Plugin.Maui.Audio`.

**Spec:** `docs/superpowers/specs/2026-09-07-tongakids-read-design.md`

**Covers spec phases:** 0–4. Delivers UC-01 (Learn Phonics) and UC-03 (Listen to Audio).
Plan 2 covers UC-02 (Stories). Plan 3 covers UC-04 to UC-07 plus hardening.

## Global Constraints

- Target framework for all verification: `net10.0-android`. Minimum Android API 21.
- Exactly three new NuGet packages: `CommunityToolkit.Mvvm`, `sqlite-net-pcl`, `Plugin.Maui.Audio`. Do not add a fourth without asking — the spec commits to low-specification devices. In particular do not add `SQLitePCLRaw.bundle_green`; see Task 1 Step 3.
- **No network call may exist on any code path a child can reach.** No analytics, no crash reporter, no runtime font or image fetch.
- All colours come from `Resources/Styles/Colors.xaml`. **Never write a hex literal in a page or control.**
- All text styles come from `Resources/Styles/Typography.xaml`. Minimum font weight 400.
- Minimum touch target 48×48dp. Phonics cards minimum 72×72dp.
- 8px baseline grid. Screen margins exactly 20px.
- Corner radii: 8 for inputs, 16 for primary buttons and cards, pill for chips and progress bars. **No sharp corners anywhere.**
- Mastery threshold is `80`, declared once in `MasteryEvaluator.MasteryThresholdPercent` and referenced everywhere else.
- A child must never see an error, a stack trace, an error code, or technical vocabulary. Missing audio is silence; missing image is a placeholder shape; a bad content row is skipped.
- Commit after every task. Never commit a build that does not compile.

## On testing in this plan

The project owner chose a single-project structure with no separate test project (spec §12).
Verification is therefore an **in-app self-check harness** built in Task 8: each engine
ships a set of assertion cases written *before* the engine itself, so the TDD rhythm is
preserved — write cases, watch them fail, implement, watch them pass.

Because running them requires launching the emulator, engine tasks (6, 7) batch their
verification into a single run at the end of Task 8 rather than one run per assertion.
Tasks 1–5 and 9–16 are verified by build plus on-device inspection against the mockup
screenshots in `TongaKids/Resources/Raw/stitch_tongakids_read_design_spec/*/screen.png`.

If a `TongaKids.Tests` project is later approved (spec §15, decision 3), the engine
assertion cases from Task 8 port across unchanged — they are plain input/output cases
with no MAUI dependency.

## File Structure

Files created by this plan, and what each owns.

**Configuration**
- `TongaKids/TongaKids.csproj` — package references, font and image build items
- `TongaKids/MauiProgram.cs` — DI registration, font registration

**Design system** (Task 1, 2)
- `Resources/Styles/Colors.xaml` — every DESIGN.md colour token, nothing else
- `Resources/Styles/Typography.xaml` — the DESIGN.md type scale as named styles
- `Resources/Styles/Styles.xaml` — component styles composed from the two above
- `Resources/Fonts/` — 4 Nunito Sans weights + Material Symbols Outlined
- `Controls/TactileButton.cs` — the "squishy" 3px bottom-border press control
- `Controls/StarRating.cs` — 5-star earned/unearned display

**Data** (Task 4, 5)
- `Models/Learner.cs`, `Level.cs`, `Lesson.cs`, `PhonicsItem.cs`, `LessonProgress.cs`, `QuizAttempt.cs`
- `Models/ContentPack.cs` — DTOs matching the JSON pack shape
- `Data/TongaKidsDatabase.cs` — single owner of the SQLite connection
- `Data/LearnerRepository.cs`, `Data/ContentRepository.cs`, `Data/ProgressRepository.cs`
- `Data/ContentSeeder.cs` — JSON → SQLite, content tables only
- `Resources/Raw/content/chitonga-content.json` — the content pack
- `tools/generate-audio-manifest/` — console script producing the recording list

**Engines** (Task 6, 7)
- `Services/IPhonicsEngine.cs` + `PhonicsEngine.cs` — quiz generation, minimal pairs
- `Services/IProgressCalculator.cs` + `ProgressCalculator.cs` — accuracy, stars, WPM
- `Services/IMasteryEvaluator.cs` + `MasteryEvaluator.cs` — the 80% rule

**Services** (Task 3, 13)
- `Services/ILearnerSession.cs` + `LearnerSession.cs` — who is currently learning
- `Services/IAudioService.cs` + `AudioService.cs` — clip playback, silent on miss
- `Services/SelfCheck/` — assertion harness (Task 8)

**Presentation** (Task 3, 9–16)
- `AppShell.xaml` — routes and TabBar
- `Views/` + `ViewModels/` — one pair per screen

---

### Task 1: Foundation — packages, fonts, design tokens

**Files:**
- Modify: `TongaKids/TongaKids.csproj`
- Modify: `TongaKids/MauiProgram.cs`
- Create: `TongaKids/Resources/Styles/Colors.xaml` (replaces template contents)
- Create: `TongaKids/Resources/Styles/Typography.xaml`
- Modify: `TongaKids/App.xaml`
- Create: `TongaKids/Resources/Fonts/` — 5 TTF files
- Create: `tools/fetch-fonts.sh`

**Interfaces:**
- Consumes: nothing (first task)
- Produces: XAML resource keys used by every later task. Colours are named exactly as the DESIGN.md token, PascalCased: `Primary`, `OnPrimary`, `PrimaryContainer`, `OnPrimaryContainer`, `Secondary`, `OnSecondary`, `SecondaryContainer`, `Tertiary`, `OnTertiary`, `TertiaryContainer`, `Error`, `OnError`, `ErrorContainer`, `Surface`, `SurfaceDim`, `SurfaceContainerLowest`, `SurfaceContainerLow`, `SurfaceContainer`, `SurfaceContainerHigh`, `SurfaceContainerHighest`, `OnSurface`, `OnSurfaceVariant`, `Outline`, `OutlineVariant`, `PrimaryFixed`, `PrimaryFixedDim`, `OnPrimaryFixed`, `OnPrimaryFixedVariant`, `SecondaryFixed`, `SecondaryFixedDim`, `TertiaryFixed`, `CardBorder`. Text styles: `DisplayLg`, `HeadlineLg`, `TitleLg`, `BodyLg`, `BodyMd`, `LabelLg`, `PhonicsFocus`. Font aliases: `NunitoRegular`, `NunitoMedium`, `NunitoBold`, `NunitoExtraBold`, `MaterialSymbols`.

- [ ] **Step 1: Fetch the fonts**

The mockups use Nunito Sans at weights 400/500/700/800 plus Material Symbols Outlined.
Google's URLs are versioned and rotate, so resolve them through the CSS API rather than
hardcoding. Create `tools/fetch-fonts.sh`:

```bash
#!/usr/bin/env bash
set -euo pipefail
DEST="$(cd "$(dirname "$0")/.." && pwd)/TongaKids/Resources/Fonts"
mkdir -p "$DEST"
UA="Mozilla/5.0"

fetch_weight() {
  local weight="$1" out="$2"
  local css url
  css=$(curl -sSfL -A "$UA" "https://fonts.googleapis.com/css2?family=Nunito+Sans:wght@${weight}")
  url=$(echo "$css" | grep -o 'https://fonts.gstatic.com/[^)]*' | head -1)
  [ -n "$url" ] || { echo "no URL for weight $weight" >&2; exit 1; }
  curl -sSfL -A "$UA" -o "$DEST/$out" "$url"
  echo "  $out"
}

echo "Nunito Sans:"
fetch_weight 400 NunitoSans-Regular.ttf
fetch_weight 500 NunitoSans-Medium.ttf
fetch_weight 700 NunitoSans-Bold.ttf
fetch_weight 800 NunitoSans-ExtraBold.ttf

echo "Material Symbols:"
css=$(curl -sSfL -A "$UA" "https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined")
url=$(echo "$css" | grep -o 'https://fonts.gstatic.com/[^)]*' | head -1)
curl -sSfL -A "$UA" -o "$DEST/MaterialSymbolsOutlined.ttf" "$url"
echo "  MaterialSymbolsOutlined.ttf"
```

Run:

```bash
chmod +x tools/fetch-fonts.sh && ./tools/fetch-fonts.sh
```

- [ ] **Step 2: Verify the fonts are real TrueType files**

Run: `file TongaKids/Resources/Fonts/*.ttf`
Expected: five lines, each saying `TrueType Font data` or `TrueType font data`.
If any file says `ASCII text` or `HTML`, the download returned an error page — stop and re-run Step 1.

- [ ] **Step 3: Add the three packages**

In `TongaKids/TongaKids.csproj`, inside the existing `<ItemGroup>` that already holds
`Microsoft.Maui.Controls`, add:

```xml
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.2" />
<PackageReference Include="sqlite-net-pcl" Version="1.11.285" />
<PackageReference Include="Plugin.Maui.Audio" Version="4.0.0" />
```

**Three packages, not four.** Older guidance (including an earlier draft of this plan)
says to add `SQLitePCLRaw.bundle_green` as the native provider. Do **not**. As of
`sqlite-net-pcl` 1.11.285 it already brings `SQLitePCLRaw.core` 3.0.3,
`provider.e_sqlite3` 3.0.3 and `SourceGear.sqlite3` 3.53.3. Adding `bundle_green` 2.1.11
on top drags the *older* 2.1.11 native libraries back in, which:

1. trips `NU1903` — `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 carries a known **high severity**
   advisory (GHSA-2m69-gcr7-jv3q), and
2. trips `XA4301` — two copies of `libe_sqlite3.so` land in the APK and one is discarded.

Verify with `dotnet list package --include-transitive` if the build ever warns about
SQLite again.

Versions above were checked against nuget.org rather than assumed; bump them only after
re-checking, since a newer `sqlite-net-pcl` may change what it brings with it.

- [ ] **Step 4: Stop the template shipping the sample bot image**

In `TongaKids/TongaKids.csproj`, delete this line — the file is unused and only inflates the APK:

```xml
<MauiImage Update="Resources\Images\dotnet_bot.png" Resize="True" BaseSize="300,185" />
```

Then delete the asset:

```bash
rm TongaKids/Resources/Images/dotnet_bot.png
```

- [ ] **Step 5: Write Colors.xaml**

Replace the entire contents of `TongaKids/Resources/Styles/Colors.xaml`. Every value is
copied verbatim from `DESIGN.md` frontmatter. `CardBorder` is the one addition — DESIGN.md
specifies it in prose ("1px border of #E0D8C3") but not in the token list.

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<ResourceDictionary xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">

    <!-- Primary — Warm Orange. Main actions, phonics highlights. -->
    <Color x:Key="Primary">#914c00</Color>
    <Color x:Key="OnPrimary">#ffffff</Color>
    <Color x:Key="PrimaryContainer">#ff8a00</Color>
    <Color x:Key="OnPrimaryContainer">#613100</Color>
    <Color x:Key="InversePrimary">#ffb77f</Color>
    <Color x:Key="PrimaryFixed">#ffdcc4</Color>
    <Color x:Key="PrimaryFixedDim">#ffb77f</Color>
    <Color x:Key="OnPrimaryFixed">#2f1500</Color>
    <Color x:Key="OnPrimaryFixedVariant">#6f3900</Color>

    <!-- Secondary — Sky Blue. Secondary navigation, information. -->
    <Color x:Key="Secondary">#006688</Color>
    <Color x:Key="OnSecondary">#ffffff</Color>
    <Color x:Key="SecondaryContainer">#58cafe</Color>
    <Color x:Key="OnSecondaryContainer">#005370</Color>
    <Color x:Key="SecondaryFixed">#c2e8ff</Color>
    <Color x:Key="SecondaryFixedDim">#75d1ff</Color>
    <Color x:Key="OnSecondaryFixed">#001e2b</Color>
    <Color x:Key="OnSecondaryFixedVariant">#004d67</Color>

    <!-- Tertiary — Leaf Green. Progress, success, nature. -->
    <Color x:Key="Tertiary">#006e1c</Color>
    <Color x:Key="OnTertiary">#ffffff</Color>
    <Color x:Key="TertiaryContainer">#5abd5c</Color>
    <Color x:Key="OnTertiaryContainer">#00480f</Color>
    <Color x:Key="TertiaryFixed">#94f990</Color>
    <Color x:Key="TertiaryFixedDim">#78dc77</Color>
    <Color x:Key="OnTertiaryFixed">#002204</Color>
    <Color x:Key="OnTertiaryFixedVariant">#005313</Color>

    <!-- Error -->
    <Color x:Key="Error">#ba1a1a</Color>
    <Color x:Key="OnError">#ffffff</Color>
    <Color x:Key="ErrorContainer">#ffdad6</Color>
    <Color x:Key="OnErrorContainer">#93000a</Color>

    <!-- Surfaces — Soft Cream, warmer than white to reduce eye strain. -->
    <Color x:Key="Surface">#fbf9f8</Color>
    <Color x:Key="SurfaceDim">#dcd9d9</Color>
    <Color x:Key="SurfaceBright">#fbf9f8</Color>
    <Color x:Key="SurfaceContainerLowest">#ffffff</Color>
    <Color x:Key="SurfaceContainerLow">#f6f3f2</Color>
    <Color x:Key="SurfaceContainer">#f0eded</Color>
    <Color x:Key="SurfaceContainerHigh">#eae8e7</Color>
    <Color x:Key="SurfaceContainerHighest">#e4e2e1</Color>
    <Color x:Key="SurfaceVariant">#e4e2e1</Color>
    <Color x:Key="SurfaceTint">#914c00</Color>
    <Color x:Key="OnSurface">#1b1c1c</Color>
    <Color x:Key="OnSurfaceVariant">#564334</Color>
    <Color x:Key="InverseSurface">#303030</Color>
    <Color x:Key="InverseOnSurface">#f3f0f0</Color>
    <Color x:Key="Background">#fbf9f8</Color>
    <Color x:Key="OnBackground">#1b1c1c</Color>

    <!-- Outlines -->
    <Color x:Key="Outline">#8a7362</Color>
    <Color x:Key="OutlineVariant">#ddc1ae</Color>

    <!-- Level 1 card border. DESIGN.md "Elevation & Depth", prose not token list. -->
    <Color x:Key="CardBorder">#e0d8c3</Color>

</ResourceDictionary>
```

- [ ] **Step 6: Write Typography.xaml**

Create `TongaKids/Resources/Styles/Typography.xaml`. Sizes and weights are the DESIGN.md
type scale. MAUI has no font-weight property for custom fonts — the weight is selected by
choosing the right registered font family alias, which is why four Nunito files are
needed rather than one.

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<ResourceDictionary xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">

    <!-- display-lg: 48/56, weight 800 -->
    <Style x:Key="DisplayLg" TargetType="Label">
        <Setter Property="FontFamily" Value="NunitoExtraBold" />
        <Setter Property="FontSize" Value="48" />
        <Setter Property="LineHeight" Value="1.17" />
        <Setter Property="TextColor" Value="{StaticResource OnSurface}" />
    </Style>

    <!-- headline-lg-mobile: 28/36, weight 700. Mobile is our only target. -->
    <Style x:Key="HeadlineLg" TargetType="Label">
        <Setter Property="FontFamily" Value="NunitoBold" />
        <Setter Property="FontSize" Value="28" />
        <Setter Property="LineHeight" Value="1.29" />
        <Setter Property="TextColor" Value="{StaticResource OnSurface}" />
    </Style>

    <!-- title-lg: 22/28, weight 700 -->
    <Style x:Key="TitleLg" TargetType="Label">
        <Setter Property="FontFamily" Value="NunitoBold" />
        <Setter Property="FontSize" Value="22" />
        <Setter Property="LineHeight" Value="1.27" />
        <Setter Property="TextColor" Value="{StaticResource OnSurface}" />
    </Style>

    <!-- body-lg: 18/26, weight 500 -->
    <Style x:Key="BodyLg" TargetType="Label">
        <Setter Property="FontFamily" Value="NunitoMedium" />
        <Setter Property="FontSize" Value="18" />
        <Setter Property="LineHeight" Value="1.44" />
        <Setter Property="TextColor" Value="{StaticResource OnSurface}" />
    </Style>

    <!-- body-md: 16/24, weight 400 -->
    <Style x:Key="BodyMd" TargetType="Label">
        <Setter Property="FontFamily" Value="NunitoRegular" />
        <Setter Property="FontSize" Value="16" />
        <Setter Property="LineHeight" Value="1.5" />
        <Setter Property="TextColor" Value="{StaticResource OnSurfaceVariant}" />
    </Style>

    <!-- label-lg: 14/20, weight 700, tracking 0.1 -->
    <Style x:Key="LabelLg" TargetType="Label">
        <Setter Property="FontFamily" Value="NunitoBold" />
        <Setter Property="FontSize" Value="14" />
        <Setter Property="CharacterSpacing" Value="0.1" />
        <Setter Property="TextColor" Value="{StaticResource OnSurface}" />
    </Style>

    <!-- phonics-focus: 64/80, weight 800. A single grapheme, centred. -->
    <Style x:Key="PhonicsFocus" TargetType="Label">
        <Setter Property="FontFamily" Value="NunitoExtraBold" />
        <Setter Property="FontSize" Value="64" />
        <Setter Property="HorizontalTextAlignment" Value="Center" />
        <Setter Property="VerticalTextAlignment" Value="Center" />
        <Setter Property="TextColor" Value="{StaticResource PrimaryContainer}" />
    </Style>

    <!-- Icon font. Set Text to the Material Symbols ligature name, e.g. "home". -->
    <Style x:Key="Icon" TargetType="Label">
        <Setter Property="FontFamily" Value="MaterialSymbols" />
        <Setter Property="FontSize" Value="24" />
        <Setter Property="TextColor" Value="{StaticResource OnSurface}" />
    </Style>

</ResourceDictionary>
```

- [ ] **Step 7: Register fonts and the new dictionary**

In `TongaKids/MauiProgram.cs`, replace the `ConfigureFonts` call:

```csharp
.ConfigureFonts(fonts =>
{
    fonts.AddFont("NunitoSans-Regular.ttf", "NunitoRegular");
    fonts.AddFont("NunitoSans-Medium.ttf", "NunitoMedium");
    fonts.AddFont("NunitoSans-Bold.ttf", "NunitoBold");
    fonts.AddFont("NunitoSans-ExtraBold.ttf", "NunitoExtraBold");
    fonts.AddFont("MaterialSymbolsOutlined.ttf", "MaterialSymbols");
})
```

The OpenSans fonts shipped by the template are no longer referenced. Delete them:

```bash
rm TongaKids/Resources/Fonts/OpenSans-Regular.ttf TongaKids/Resources/Fonts/OpenSans-Semibold.ttf
```

In `TongaKids/App.xaml`, add `Typography.xaml` to the merged dictionaries, after
`Colors.xaml` and before `Styles.xaml` — order matters because Typography references
colour keys:

```xml
<ResourceDictionary.MergedDictionaries>
    <ResourceDictionary Source="Resources/Styles/Colors.xaml" />
    <ResourceDictionary Source="Resources/Styles/Typography.xaml" />
    <ResourceDictionary Source="Resources/Styles/Styles.xaml" />
</ResourceDictionary.MergedDictionaries>
```

- [ ] **Step 8: Build**

Run: `dotnet build TongaKids/TongaKids.csproj -f net10.0-android`
Expected: `Build succeeded`.

If it fails with a missing resource key, the template `Styles.xaml` still references a
colour name that no longer exists (`Gray100`, `Magenta` and similar were in the template
palette). Task 2 rewrites that file; for now, replace any missing key reference with the
nearest key defined in Step 5 to get the build green.

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "Add design tokens, Nunito Sans fonts, and core packages

Transcribes the DESIGN.md colour and type scales into MAUI resource
dictionaries, fetches the five fonts the mockups use, and adds the three
runtime dependencies. Drops the template's OpenSans fonts and sample image.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 2: Design-system controls and component styles

**Files:**
- Create: `TongaKids/Controls/TactileButton.cs`
- Create: `TongaKids/Controls/StarRating.cs`
- Modify: `TongaKids/Resources/Styles/Styles.xaml` (replace template contents)

**Interfaces:**
- Consumes: colour and text style keys from Task 1.
- Produces:
  - `TongaKids.Controls.TactileButton : ContentView` — bindable properties `Text` (string), `BackgroundFill` (Color), `TextColor` (Color), `DepthColor` (Color), `Command` (ICommand), `CommandParameter` (object), `CornerRadius` (double, default 16). Enable/disable uses the inherited `VisualElement.IsEnabled`; the tap handler returns early when it is false.
  - `TongaKids.Controls.StarRating : ContentView` — bindable properties `Earned` (int), `Total` (int, default 5), `StarSize` (double, default 32).
  - Styles.xaml keys: `Card`, `CardTinted`, `ScreenPadding` (Thickness), `PillProgress`.

The tactile press effect from DESIGN.md ("Level 2: colour fill with a thick 3px bottom
border in a darker shade") is the single most repeated visual in the mockups — the orange
Play Game button, the green audio button, the Next button, the level Play chips. Building
it once here prevents it being re-hand-rolled on eight screens.

- [ ] **Step 1: Write TactileButton**

Create `TongaKids/Controls/TactileButton.cs`:

```csharp
using System.Windows.Input;

namespace TongaKids.Controls;

/// <summary>
/// A "squishy" button: a coloured face sitting on a darker plinth. Pressing it
/// translates the face down onto the plinth, which reads as physical depression.
/// DESIGN.md, "Elevation & Depth", Level 2.
/// </summary>
public class TactileButton : ContentView
{
    private const double DepthPixels = 4;

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(TactileButton), string.Empty);

    public static readonly BindableProperty BackgroundFillProperty =
        BindableProperty.Create(nameof(BackgroundFill), typeof(Color), typeof(TactileButton), Colors.Orange);

    public static readonly BindableProperty TextColorProperty =
        BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(TactileButton), Colors.White);

    public static readonly BindableProperty DepthColorProperty =
        BindableProperty.Create(nameof(DepthColor), typeof(Color), typeof(TactileButton), Colors.DarkOrange);

    public static readonly BindableProperty CornerRadiusProperty =
        BindableProperty.Create(nameof(CornerRadius), typeof(double), typeof(TactileButton), 16d);

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(TactileButton));

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(TactileButton));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public Color BackgroundFill
    {
        get => (Color)GetValue(BackgroundFillProperty);
        set => SetValue(BackgroundFillProperty, value);
    }

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public Color DepthColor
    {
        get => (Color)GetValue(DepthColorProperty);
        set => SetValue(DepthColorProperty, value);
    }

    public double CornerRadius
    {
        get => (double)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    private readonly Border _plinth;
    private readonly Border _face;
    private readonly Label _label;

    public TactileButton()
    {
        // 48dp is the DESIGN.md minimum hit area; the plinth adds the depth below it.
        MinimumHeightRequest = 48 + DepthPixels;

        _label = new Label
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            FontFamily = "NunitoBold",
            FontSize = 18
        };

        _face = new Border
        {
            Content = _label,
            Padding = new Thickness(24, 12),
            StrokeThickness = 0,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        _plinth = new Border
        {
            StrokeThickness = 0,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        // The plinth sits DepthPixels lower; the face covers all but that sliver.
        var grid = new Grid();
        grid.Add(_plinth);
        grid.Add(_face);
        _plinth.Margin = new Thickness(0, DepthPixels, 0, 0);
        _face.Margin = new Thickness(0, 0, 0, DepthPixels);

        Content = grid;

        var tap = new TapGestureRecognizer();
        tap.Tapped += OnTapped;
        GestureRecognizers.Add(tap);

        ApplyVisual();
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName is nameof(Text) or nameof(BackgroundFill) or nameof(TextColor)
            or nameof(DepthColor) or nameof(CornerRadius))
        {
            ApplyVisual();
        }
    }

    private void ApplyVisual()
    {
        _label.Text = Text;
        _label.TextColor = TextColor;
        _face.BackgroundColor = BackgroundFill;
        _plinth.BackgroundColor = DepthColor;
        _face.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(CornerRadius) };
        _plinth.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(CornerRadius) };
    }

    private async void OnTapped(object? sender, TappedEventArgs e)
    {
        if (!IsEnabled)
        {
            return;
        }

        // Press: drop the face onto the plinth. Release: spring back.
        await _face.TranslateToAsync(0, DepthPixels, 60, Easing.CubicOut);
        await _face.TranslateToAsync(0, 0, 90, Easing.CubicOut);

        if (Command?.CanExecute(CommandParameter) == true)
        {
            Command.Execute(CommandParameter);
        }
    }
}
```

- [ ] **Step 2: Write StarRating**

Create `TongaKids/Controls/StarRating.cs`. DESIGN.md: unearned stars are outline Sky Blue,
earned stars are filled Amber.

```csharp
namespace TongaKids.Controls;

/// <summary>
/// Five-star lesson result. DESIGN.md "Progress & Rewards": unearned stars are
/// outline Sky Blue, earned are filled Amber.
/// </summary>
public class StarRating : ContentView
{
    private static readonly Color EarnedColor = Color.FromArgb("#ff8a00");
    private static readonly Color UnearnedColor = Color.FromArgb("#58cafe");

    public static readonly BindableProperty EarnedProperty =
        BindableProperty.Create(nameof(Earned), typeof(int), typeof(StarRating), 0,
            propertyChanged: (b, _, _) => ((StarRating)b).Rebuild());

    public static readonly BindableProperty TotalProperty =
        BindableProperty.Create(nameof(Total), typeof(int), typeof(StarRating), 5,
            propertyChanged: (b, _, _) => ((StarRating)b).Rebuild());

    public static readonly BindableProperty StarSizeProperty =
        BindableProperty.Create(nameof(StarSize), typeof(double), typeof(StarRating), 32d,
            propertyChanged: (b, _, _) => ((StarRating)b).Rebuild());

    public int Earned
    {
        get => (int)GetValue(EarnedProperty);
        set => SetValue(EarnedProperty, value);
    }

    public int Total
    {
        get => (int)GetValue(TotalProperty);
        set => SetValue(TotalProperty, value);
    }

    public double StarSize
    {
        get => (double)GetValue(StarSizeProperty);
        set => SetValue(StarSizeProperty, value);
    }

    private readonly HorizontalStackLayout _row = new() { Spacing = 4 };

    public StarRating()
    {
        Content = _row;
        Rebuild();
    }

    private void Rebuild()
    {
        _row.Clear();
        for (var i = 0; i < Total; i++)
        {
            var isEarned = i < Earned;
            _row.Add(new Label
            {
                // Material Symbols ligatures: filled vs outlined star.
                Text = isEarned ? "star" : "star_outline",
                FontFamily = "MaterialSymbols",
                FontSize = StarSize,
                TextColor = isEarned ? EarnedColor : UnearnedColor,
                VerticalOptions = LayoutOptions.Center
            });
        }
    }
}
```

> Note: `StarRating` is the one place a hex literal is allowed, because the two star
> colours are stated in DESIGN.md prose rather than the token list and the control has no
> XAML in which to resolve `StaticResource`. Everywhere else, the global constraint holds.

- [ ] **Step 3: Replace Styles.xaml**

Replace the entire contents of `TongaKids/Resources/Styles/Styles.xaml`. The template
file references template colours that no longer exist, so it must go wholesale.

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<ResourceDictionary xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">

    <!-- 20px screen margin: the "safe zone" for small hands gripping a phone. -->
    <Thickness x:Key="ScreenPadding">20,20,20,20</Thickness>

    <!-- Level 1 surface: white, 1px darker-cream border, soft diffused shadow. -->
    <Style x:Key="Card" TargetType="Border">
        <Setter Property="BackgroundColor" Value="{StaticResource SurfaceContainerLowest}" />
        <Setter Property="Stroke" Value="{StaticResource CardBorder}" />
        <Setter Property="StrokeThickness" Value="1" />
        <Setter Property="Padding" Value="16" />
        <Setter Property="StrokeShape">
            <Setter.Value>
                <RoundRectangle CornerRadius="16" />
            </Setter.Value>
        </Setter>
        <Setter Property="Shadow">
            <Setter.Value>
                <Shadow Brush="{StaticResource OnSurface}" Offset="0,4" Radius="8" Opacity="0.08" />
            </Setter.Value>
        </Setter>
    </Style>

    <!-- Tinted variant for categorising sounds (e.g. vowels vs consonants). -->
    <Style x:Key="CardTinted" TargetType="Border" BasedOn="{StaticResource Card}">
        <Setter Property="BackgroundColor" Value="{StaticResource SurfaceContainerLow}" />
    </Style>

    <!-- Pill progress bar. -->
    <Style x:Key="PillProgress" TargetType="ProgressBar">
        <Setter Property="ProgressColor" Value="{StaticResource PrimaryContainer}" />
        <Setter Property="BackgroundColor" Value="{StaticResource SurfaceContainerHighest}" />
        <Setter Property="HeightRequest" Value="12" />
    </Style>

    <!-- Page default: Soft Cream ground everywhere. -->
    <Style TargetType="ContentPage" ApplyToDerivedTypes="True">
        <Setter Property="BackgroundColor" Value="{StaticResource Surface}" />
    </Style>

    <Style TargetType="Label">
        <Setter Property="FontFamily" Value="NunitoRegular" />
        <Setter Property="TextColor" Value="{StaticResource OnSurface}" />
    </Style>

</ResourceDictionary>
```

- [ ] **Step 4: Build**

Run: `dotnet build TongaKids/TongaKids.csproj -f net10.0-android`
Expected: `Build succeeded`. Any remaining error naming a colour key means a leftover
template reference — remove it.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "Add TactileButton, StarRating, and component styles

Implements the DESIGN.md depth model once as a reusable control rather than
per screen: a coloured face on a darker plinth that depresses on tap.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 3: Shell, navigation, and dependency injection

**Files:**
- Modify: `TongaKids/AppShell.xaml`, `TongaKids/AppShell.xaml.cs`
- Modify: `TongaKids/MauiProgram.cs`
- Modify: `TongaKids/App.xaml.cs`
- Delete: `TongaKids/MainPage.xaml`, `TongaKids/MainPage.xaml.cs`
- Create: `TongaKids/Views/SplashPage.xaml(.cs)`, `OnboardingPage`, `ProfileSelectionPage`, `HomePage`, `StoriesPage`, `ProgressPage`, `SettingsPage` — all placeholders at this stage
- Create: `TongaKids/Services/ILearnerSession.cs`, `TongaKids/Services/LearnerSession.cs`

**Interfaces:**
- Consumes: styles from Tasks 1–2.
- Produces:
  - Absolute routes: `//splash`, `//onboarding`, `//profiles`, `//main/home`, `//main/stories`, `//main/progress`, `//main/settings`
  - Relative routes registered in `AppShell.xaml.cs`: `levels`, `lesson`, `game`, `complete`
  - `ILearnerSession` with `Learner? Current { get; }`, `void SetCurrent(Learner learner)`, `void Clear()`
  - `MauiProgram.CreateMauiApp()` registering every service and page

- [ ] **Step 1: Create the placeholder pages**

Seven pages. Each is the same shape; here is `SplashPage`, and the other six are identical
apart from the class name, the `x:Class` value, and the label text.

`TongaKids/Views/SplashPage.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="TongaKids.Views.SplashPage"
             Shell.NavBarIsVisible="False">
    <Label Text="Splash" Style="{StaticResource HeadlineLg}"
           HorizontalOptions="Center" VerticalOptions="Center" />
</ContentPage>
```

`TongaKids/Views/SplashPage.xaml.cs`:

```csharp
namespace TongaKids.Views;

public partial class SplashPage : ContentPage
{
    public SplashPage()
    {
        InitializeComponent();
    }
}
```

Repeat for `OnboardingPage`, `ProfileSelectionPage`, `HomePage`, `StoriesPage`,
`ProgressPage`, `SettingsPage` — same two files each, changing `SplashPage` to the new
name in both the `x:Class` attribute and the class declaration, and changing the label
text to match. Leave `Shell.NavBarIsVisible="False"` on `SplashPage`, `OnboardingPage`
and `ProfileSelectionPage` only.

Then delete the template page:

```bash
rm TongaKids/MainPage.xaml TongaKids/MainPage.xaml.cs
```

- [ ] **Step 2: Write AppShell.xaml**

Splash, onboarding and profile selection sit outside the TabBar so a child never sees a
navigation bar before choosing a profile.

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<Shell x:Class="TongaKids.AppShell"
       xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
       xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
       xmlns:views="clr-namespace:TongaKids.Views"
       Shell.FlyoutBehavior="Disabled"
       Shell.TabBarBackgroundColor="{StaticResource SurfaceContainerLow}"
       Shell.TabBarForegroundColor="{StaticResource Tertiary}"
       Shell.TabBarUnselectedColor="{StaticResource OnSurfaceVariant}"
       Shell.TabBarTitleColor="{StaticResource Tertiary}">

    <ShellContent Route="splash"
                  ContentTemplate="{DataTemplate views:SplashPage}"
                  Shell.TabBarIsVisible="False" />

    <ShellContent Route="onboarding"
                  ContentTemplate="{DataTemplate views:OnboardingPage}"
                  Shell.TabBarIsVisible="False" />

    <ShellContent Route="profiles"
                  ContentTemplate="{DataTemplate views:ProfileSelectionPage}"
                  Shell.TabBarIsVisible="False" />

    <TabBar Route="main">
        <ShellContent Title="Home" Route="home"
                      ContentTemplate="{DataTemplate views:HomePage}" />
        <ShellContent Title="Stories" Route="stories"
                      ContentTemplate="{DataTemplate views:StoriesPage}" />
        <ShellContent Title="Progress" Route="progress"
                      ContentTemplate="{DataTemplate views:ProgressPage}" />
        <ShellContent Title="Settings" Route="settings"
                      ContentTemplate="{DataTemplate views:SettingsPage}" />
    </TabBar>

</Shell>
```

- [ ] **Step 3: Register the pushed routes**

`TongaKids/AppShell.xaml.cs`:

```csharp
using TongaKids.Views;

namespace TongaKids;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Pushed routes — reached from a page, not from the tab bar.
        Routing.RegisterRoute("levels", typeof(PhonicsLevelsPage));
        Routing.RegisterRoute("lesson", typeof(PhonicsLessonPage));
        Routing.RegisterRoute("game", typeof(MatchingGamePage));
        Routing.RegisterRoute("complete", typeof(LessonCompletePage));
    }
}
```

These four page types do not exist until Tasks 12–16. To keep the build green now,
create each as a placeholder using the exact pattern from Step 1 (`PhonicsLevelsPage`,
`PhonicsLessonPage`, `MatchingGamePage`, `LessonCompletePage`), each with
`Shell.NavBarIsVisible="False"`.

- [ ] **Step 4: Write LearnerSession**

`TongaKids/Services/ILearnerSession.cs`:

```csharp
using TongaKids.Models;

namespace TongaKids.Services;

/// <summary>Which learner is using the app right now. In-memory only.</summary>
public interface ILearnerSession
{
    Learner? Current { get; }

    void SetCurrent(Learner learner);

    void Clear();
}
```

`TongaKids/Services/LearnerSession.cs`:

```csharp
using TongaKids.Models;

namespace TongaKids.Services;

public sealed class LearnerSession : ILearnerSession
{
    public Learner? Current { get; private set; }

    public void SetCurrent(Learner learner) => Current = learner;

    public void Clear() => Current = null;
}
```

`Learner` is defined in Task 4. Create Task 4's `Models/Learner.cs` first if the build
needs it, or reorder so Task 4 runs before this step.

- [ ] **Step 5: Wire dependency injection**

Replace `TongaKids/MauiProgram.cs`:

```csharp
using Microsoft.Extensions.Logging;
using TongaKids.Services;
using TongaKids.Views;

namespace TongaKids;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("NunitoSans-Regular.ttf", "NunitoRegular");
                fonts.AddFont("NunitoSans-Medium.ttf", "NunitoMedium");
                fonts.AddFont("NunitoSans-Bold.ttf", "NunitoBold");
                fonts.AddFont("NunitoSans-ExtraBold.ttf", "NunitoExtraBold");
                fonts.AddFont("MaterialSymbolsOutlined.ttf", "MaterialSymbols");
            });

        // Session is a singleton: one learner at a time, app-wide.
        builder.Services.AddSingleton<ILearnerSession, LearnerSession>();

        // Pages are transient so each navigation gets fresh state.
        builder.Services.AddTransient<SplashPage>();
        builder.Services.AddTransient<OnboardingPage>();
        builder.Services.AddTransient<ProfileSelectionPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<StoriesPage>();
        builder.Services.AddTransient<ProgressPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<PhonicsLevelsPage>();
        builder.Services.AddTransient<PhonicsLessonPage>();
        builder.Services.AddTransient<MatchingGamePage>();
        builder.Services.AddTransient<LessonCompletePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
```

- [ ] **Step 6: Build and run on the emulator**

Run: `dotnet build TongaKids/TongaKids.csproj -f net10.0-android -t:Run`
Expected: the app launches showing "Splash". No tab bar is visible.

To confirm the TabBar works, temporarily change `App.xaml.cs` to navigate to `//main/home`
on start, verify four tabs appear with Home selected, then change it back.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "Add Shell routes, tab bar, and dependency injection

Splash, onboarding and profile selection sit outside the TabBar so a child
never sees navigation before choosing a profile. Registers all pages and
the learner session.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 4: Models, database, and repositories

**Files:**
- Create: `TongaKids/Models/Learner.cs`, `Level.cs`, `Lesson.cs`, `PhonicsItem.cs`, `LessonProgress.cs`, `QuizAttempt.cs`
- Create: `TongaKids/Data/TongaKidsDatabase.cs`
- Create: `TongaKids/Data/LearnerRepository.cs`, `ContentRepository.cs`, `ProgressRepository.cs`
- Modify: `TongaKids/MauiProgram.cs`

**Interfaces:**
- Consumes: nothing beyond packages from Task 1.
- Produces:
  - `ITongaKidsDatabase.GetConnectionAsync() → Task<SQLiteAsyncConnection>`
  - `ILearnerRepository`: `GetAllAsync() → Task<List<Learner>>`, `AddAsync(Learner) → Task<Learner>`, `TouchAsync(int learnerId) → Task`
  - `IContentRepository`: `GetLevelsAsync() → Task<List<Level>>`, `GetLessonsAsync(int levelId) → Task<List<Lesson>>`, `GetLessonAsync(int lessonId) → Task<Lesson?>`, `GetItemsAsync(int lessonId) → Task<List<PhonicsItem>>`, `GetAllItemsAsync() → Task<List<PhonicsItem>>`
  - `IProgressRepository`: `GetForLearnerAsync(int learnerId) → Task<List<LessonProgress>>`, `GetForLessonAsync(int learnerId, int lessonId) → Task<LessonProgress?>`, `SaveAsync(LessonProgress) → Task`, `AddAttemptAsync(QuizAttempt) → Task`

- [ ] **Step 1: Write the models**

Six files in `TongaKids/Models/`. Content models use assigned integer primary keys so the
JSON pack controls identity; learner-state models autoincrement.

```csharp
// Learner.cs
using SQLite;

namespace TongaKids.Models;

[Table("Learner")]
public class Learner
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Image resource name without extension, e.g. "avatar_chipo".</summary>
    public string AvatarKey { get; set; } = string.Empty;
    /// <summary>Colour resource key for the profile ring, e.g. "Tertiary".</summary>
    public string AccentColorKey { get; set; } = "Tertiary";
    public DateTime CreatedAt { get; set; }
    public DateTime LastActiveAt { get; set; }
}
```

```csharp
// Level.cs
using SQLite;

namespace TongaKids.Models;

[Table("Level")]
public class Level
{
    [PrimaryKey] public int Id { get; set; }
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    /// <summary>Material Symbols ligature, e.g. "music_note".</summary>
    public string IconGlyph { get; set; } = string.Empty;
    /// <summary>Level number that must be mastered first. 0 means always unlocked.</summary>
    public int RequiresLevelNumber { get; set; }
}
```

```csharp
// Lesson.cs
using SQLite;

namespace TongaKids.Models;

[Table("Lesson")]
public class Lesson
{
    [PrimaryKey] public int Id { get; set; }
    [Indexed] public int LevelId { get; set; }
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
}
```

```csharp
// PhonicsItem.cs
using SQLite;

namespace TongaKids.Models;

[Table("PhonicsItem")]
public class PhonicsItem
{
    [PrimaryKey] public int Id { get; set; }
    [Indexed] public int LessonId { get; set; }
    /// <summary>The letter or syllable shown on the card, e.g. "BA".</summary>
    public string Grapheme { get; set; } = string.Empty;
    /// <summary>Audio filename without extension, e.g. "syl_ba".</summary>
    public string AudioKey { get; set; } = string.Empty;
    public string ExampleWord { get; set; } = string.Empty;
    /// <summary>English gloss, parent-facing support text.</summary>
    public string Gloss { get; set; } = string.Empty;
    public string ImageKey { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
```

```csharp
// LessonProgress.cs
using SQLite;

namespace TongaKids.Models;

[Table("LessonProgress")]
public class LessonProgress
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int LearnerId { get; set; }
    [Indexed] public int LessonId { get; set; }
    public int StarsEarned { get; set; }
    public int BestScorePercent { get; set; }
    public bool IsMastered { get; set; }
    public DateTime? CompletedAt { get; set; }
}
```

```csharp
// QuizAttempt.cs
using SQLite;

namespace TongaKids.Models;

/// <summary>
/// One completed quiz. These raw rows are what the ProgressCalculator
/// aggregates — without them, reported accuracy would have nothing behind it.
/// </summary>
[Table("QuizAttempt")]
public class QuizAttempt
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int LearnerId { get; set; }
    [Indexed] public int LessonId { get; set; }
    public int CorrectCount { get; set; }
    public int TotalCount { get; set; }
    public long DurationMs { get; set; }
    public DateTime AttemptedAt { get; set; }
}
```

- [ ] **Step 2: Write the database**

`TongaKids/Data/TongaKidsDatabase.cs`. Single owner of the connection, created once,
WAL enabled for concurrent read while writing.

```csharp
using SQLite;
using TongaKids.Models;

namespace TongaKids.Data;

public interface ITongaKidsDatabase
{
    Task<SQLiteAsyncConnection> GetConnectionAsync();
}

public sealed class TongaKidsDatabase : ITongaKidsDatabase
{
    public const string FileName = "tongakids.db3";

    private const SQLiteOpenFlags Flags =
        SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;

    private SQLiteAsyncConnection? _connection;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
        {
            return _connection;
        }

        await _gate.WaitAsync();
        try
        {
            if (_connection is not null)
            {
                return _connection;
            }

            var path = Path.Combine(FileSystem.AppDataDirectory, FileName);
            var connection = new SQLiteAsyncConnection(path, Flags);

            // WAL keeps reads fast while progress is being written.
            await connection.ExecuteAsync("PRAGMA journal_mode=WAL;");

            await connection.CreateTableAsync<Learner>();
            await connection.CreateTableAsync<Level>();
            await connection.CreateTableAsync<Lesson>();
            await connection.CreateTableAsync<PhonicsItem>();
            await connection.CreateTableAsync<LessonProgress>();
            await connection.CreateTableAsync<QuizAttempt>();

            _connection = connection;
            return _connection;
        }
        finally
        {
            _gate.Release();
        }
    }
}
```

- [ ] **Step 3: Write the repositories**

`TongaKids/Data/LearnerRepository.cs`:

```csharp
using TongaKids.Models;

namespace TongaKids.Data;

public interface ILearnerRepository
{
    Task<List<Learner>> GetAllAsync();
    Task<Learner> AddAsync(Learner learner);
    Task TouchAsync(int learnerId);
}

public sealed class LearnerRepository(ITongaKidsDatabase database) : ILearnerRepository
{
    public async Task<List<Learner>> GetAllAsync()
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<Learner>().OrderBy(l => l.Id).ToListAsync();
    }

    public async Task<Learner> AddAsync(Learner learner)
    {
        var db = await database.GetConnectionAsync();
        learner.CreatedAt = DateTime.UtcNow;
        learner.LastActiveAt = DateTime.UtcNow;
        await db.InsertAsync(learner);
        return learner;
    }

    public async Task TouchAsync(int learnerId)
    {
        var db = await database.GetConnectionAsync();
        await db.ExecuteAsync(
            "UPDATE Learner SET LastActiveAt = ? WHERE Id = ?", DateTime.UtcNow, learnerId);
    }
}
```

`TongaKids/Data/ContentRepository.cs`:

```csharp
using TongaKids.Models;

namespace TongaKids.Data;

public interface IContentRepository
{
    Task<List<Level>> GetLevelsAsync();
    Task<List<Lesson>> GetLessonsAsync(int levelId);
    Task<Lesson?> GetLessonAsync(int lessonId);
    Task<List<PhonicsItem>> GetItemsAsync(int lessonId);
    Task<List<PhonicsItem>> GetAllItemsAsync();
}

public sealed class ContentRepository(ITongaKidsDatabase database) : IContentRepository
{
    public async Task<List<Level>> GetLevelsAsync()
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<Level>().OrderBy(l => l.Number).ToListAsync();
    }

    public async Task<List<Lesson>> GetLessonsAsync(int levelId)
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<Lesson>()
            .Where(l => l.LevelId == levelId)
            .OrderBy(l => l.Number)
            .ToListAsync();
    }

    public async Task<Lesson?> GetLessonAsync(int lessonId)
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<Lesson>().Where(l => l.Id == lessonId).FirstOrDefaultAsync();
    }

    public async Task<List<PhonicsItem>> GetItemsAsync(int lessonId)
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<PhonicsItem>()
            .Where(i => i.LessonId == lessonId)
            .OrderBy(i => i.SortOrder)
            .ToListAsync();
    }

    public async Task<List<PhonicsItem>> GetAllItemsAsync()
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<PhonicsItem>().ToListAsync();
    }
}
```

`TongaKids/Data/ProgressRepository.cs`:

```csharp
using TongaKids.Models;

namespace TongaKids.Data;

public interface IProgressRepository
{
    Task<List<LessonProgress>> GetForLearnerAsync(int learnerId);
    Task<LessonProgress?> GetForLessonAsync(int learnerId, int lessonId);
    Task SaveAsync(LessonProgress progress);
    Task AddAttemptAsync(QuizAttempt attempt);
}

public sealed class ProgressRepository(ITongaKidsDatabase database) : IProgressRepository
{
    public async Task<List<LessonProgress>> GetForLearnerAsync(int learnerId)
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<LessonProgress>().Where(p => p.LearnerId == learnerId).ToListAsync();
    }

    public async Task<LessonProgress?> GetForLessonAsync(int learnerId, int lessonId)
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<LessonProgress>()
            .Where(p => p.LearnerId == learnerId && p.LessonId == lessonId)
            .FirstOrDefaultAsync();
    }

    public async Task SaveAsync(LessonProgress progress)
    {
        var db = await database.GetConnectionAsync();
        if (progress.Id == 0)
        {
            await db.InsertAsync(progress);
        }
        else
        {
            await db.UpdateAsync(progress);
        }
    }

    public async Task AddAttemptAsync(QuizAttempt attempt)
    {
        var db = await database.GetConnectionAsync();
        await db.InsertAsync(attempt);
    }
}
```

- [ ] **Step 4: Register in DI**

In `MauiProgram.cs`, before the page registrations:

```csharp
builder.Services.AddSingleton<ITongaKidsDatabase, TongaKidsDatabase>();
builder.Services.AddSingleton<ILearnerRepository, LearnerRepository>();
builder.Services.AddSingleton<IContentRepository, ContentRepository>();
builder.Services.AddSingleton<IProgressRepository, ProgressRepository>();
```

Add `using TongaKids.Data;` at the top.

- [ ] **Step 5: Build**

Run: `dotnet build TongaKids/TongaKids.csproj -f net10.0-android`
Expected: `Build succeeded`.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "Add SQLite models, database, and repositories

Splits content tables (assigned ids, seeded) from learner-state tables
(autoincrement, written at runtime) so reseeding content on an app update
can never touch a child's progress.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 5: Content pack, seeder, and audio manifest tool

**Files:**
- Create: `TongaKids/Resources/Raw/content/chitonga-content.json`
- Create: `TongaKids/Models/ContentPack.cs`
- Create: `TongaKids/Data/ContentSeeder.cs`
- Create: `tools/generate-audio-manifest.py`
- Create: `docs/audio-recording-list.md` (generated)
- Modify: `TongaKids/MauiProgram.cs`, `TongaKids/App.xaml.cs`

**Interfaces:**
- Consumes: `ITongaKidsDatabase`, models from Task 4.
- Produces: `IContentSeeder.SeedIfNeededAsync() → Task<bool>` (true if it seeded).

**Content ownership:** the schema, seeder and tool are built here. The Chitonga words and
glosses are authored by the project owner. Fields left empty below are deliberate — the UI
renders an example row only when `exampleWord` is non-empty, so the app is correct and
runnable with content half-written. **Do not invent Chitonga vocabulary to fill them.**
The two real words present (`balu` = ball, and the `BA`/`DA`/`MA`/`PA` syllable set) come
from the approved mockups.

- [ ] **Step 1: Write the content pack**

Create `TongaKids/Resources/Raw/content/chitonga-content.json`:

```json
{
  "version": 1,
  "levels": [
    {
      "id": 1,
      "number": 1,
      "title": "Level 1: Vowels",
      "subtitle": "Learn the sounds of A, E, I, O, U",
      "iconGlyph": "psychology",
      "requiresLevelNumber": 0,
      "lessons": [
        {
          "id": 101,
          "number": 1,
          "title": "Lesson 1: The Five Vowels",
          "items": [
            { "id": 1001, "grapheme": "A", "audioKey": "vowel_a", "exampleWord": "", "gloss": "", "imageKey": "", "sortOrder": 1 },
            { "id": 1002, "grapheme": "E", "audioKey": "vowel_e", "exampleWord": "", "gloss": "", "imageKey": "", "sortOrder": 2 },
            { "id": 1003, "grapheme": "I", "audioKey": "vowel_i", "exampleWord": "", "gloss": "", "imageKey": "", "sortOrder": 3 },
            { "id": 1004, "grapheme": "O", "audioKey": "vowel_o", "exampleWord": "", "gloss": "", "imageKey": "", "sortOrder": 4 },
            { "id": 1005, "grapheme": "U", "audioKey": "vowel_u", "exampleWord": "", "gloss": "", "imageKey": "", "sortOrder": 5 }
          ]
        }
      ]
    },
    {
      "id": 2,
      "number": 2,
      "title": "Level 2: Simple Syllables",
      "subtitle": "Combining letters to make basic sounds.",
      "iconGlyph": "music_note",
      "requiresLevelNumber": 1,
      "lessons": [
        {
          "id": 201,
          "number": 1,
          "title": "Lesson 1: Ba and Da",
          "items": [
            { "id": 2001, "grapheme": "BA", "audioKey": "syl_ba", "exampleWord": "Balu", "gloss": "ball", "imageKey": "word_ball", "sortOrder": 1 },
            { "id": 2002, "grapheme": "DA", "audioKey": "syl_da", "exampleWord": "", "gloss": "", "imageKey": "", "sortOrder": 2 }
          ]
        },
        {
          "id": 202,
          "number": 2,
          "title": "Lesson 2: Syllables",
          "items": [
            { "id": 2003, "grapheme": "MA", "audioKey": "syl_ma", "exampleWord": "", "gloss": "", "imageKey": "", "sortOrder": 1 },
            { "id": 2004, "grapheme": "PA", "audioKey": "syl_pa", "exampleWord": "", "gloss": "", "imageKey": "", "sortOrder": 2 }
          ]
        }
      ]
    },
    {
      "id": 3,
      "number": 3,
      "title": "Level 3: Word Building",
      "subtitle": "Mastering short words and structures.",
      "iconGlyph": "extension",
      "requiresLevelNumber": 2,
      "lessons": []
    },
    {
      "id": 4,
      "number": 4,
      "title": "Level 4: Sentence Reading",
      "subtitle": "Reading your first Tonga sentences.",
      "iconGlyph": "menu_book",
      "requiresLevelNumber": 3,
      "lessons": []
    }
  ]
}
```

Levels 3 and 4 ship with no lessons on purpose: the mockup shows them locked behind
Level 2, so a child never reaches them, and the owner adds lessons as content is written.

- [ ] **Step 2: Write the DTOs**

`TongaKids/Models/ContentPack.cs`. These mirror the JSON exactly and exist only for
deserialisation — the SQLite entities in Task 4 stay free of serialisation concerns.

```csharp
using System.Text.Json.Serialization;

namespace TongaKids.Models;

public sealed class ContentPack
{
    [JsonPropertyName("version")] public int Version { get; set; }
    [JsonPropertyName("levels")] public List<ContentLevel> Levels { get; set; } = [];
}

public sealed class ContentLevel
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("number")] public int Number { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
    [JsonPropertyName("subtitle")] public string Subtitle { get; set; } = string.Empty;
    [JsonPropertyName("iconGlyph")] public string IconGlyph { get; set; } = string.Empty;
    [JsonPropertyName("requiresLevelNumber")] public int RequiresLevelNumber { get; set; }
    [JsonPropertyName("lessons")] public List<ContentLesson> Lessons { get; set; } = [];
}

public sealed class ContentLesson
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("number")] public int Number { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
    [JsonPropertyName("items")] public List<ContentItem> Items { get; set; } = [];
}

public sealed class ContentItem
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("grapheme")] public string Grapheme { get; set; } = string.Empty;
    [JsonPropertyName("audioKey")] public string AudioKey { get; set; } = string.Empty;
    [JsonPropertyName("exampleWord")] public string ExampleWord { get; set; } = string.Empty;
    [JsonPropertyName("gloss")] public string Gloss { get; set; } = string.Empty;
    [JsonPropertyName("imageKey")] public string ImageKey { get; set; } = string.Empty;
    [JsonPropertyName("sortOrder")] public int SortOrder { get; set; }
}
```

- [ ] **Step 3: Write the seeder**

`TongaKids/Data/ContentSeeder.cs`. Touches content tables only. A malformed row is
skipped rather than aborting the seed, per the spec's error-handling rule.

```csharp
using System.Text.Json;
using TongaKids.Models;

namespace TongaKids.Data;

public interface IContentSeeder
{
    Task<bool> SeedIfNeededAsync();
}

public sealed class ContentSeeder(ITongaKidsDatabase database) : IContentSeeder
{
    private const string AssetPath = "content/chitonga-content.json";
    private const string VersionKey = "content_version";

    public async Task<bool> SeedIfNeededAsync()
    {
        ContentPack? pack;
        try
        {
            await using var stream = await FileSystem.OpenAppPackageFileAsync(AssetPath);
            pack = await JsonSerializer.DeserializeAsync<ContentPack>(stream);
        }
        catch (Exception ex)
        {
            // No content pack means an empty library, not a crash.
            System.Diagnostics.Debug.WriteLine($"[ContentSeeder] could not read pack: {ex}");
            return false;
        }

        if (pack is null)
        {
            return false;
        }

        var installed = Preferences.Default.Get(VersionKey, 0);
        if (installed >= pack.Version)
        {
            return false;
        }

        var db = await database.GetConnectionAsync();

        // Content tables only. Learner progress is never touched by seeding.
        await db.DeleteAllAsync<PhonicsItem>();
        await db.DeleteAllAsync<Lesson>();
        await db.DeleteAllAsync<Level>();

        foreach (var level in pack.Levels)
        {
            if (level.Id <= 0 || string.IsNullOrWhiteSpace(level.Title))
            {
                System.Diagnostics.Debug.WriteLine($"[ContentSeeder] skipped level {level.Id}");
                continue;
            }

            await db.InsertAsync(new Level
            {
                Id = level.Id,
                Number = level.Number,
                Title = level.Title,
                Subtitle = level.Subtitle,
                IconGlyph = level.IconGlyph,
                RequiresLevelNumber = level.RequiresLevelNumber
            });

            foreach (var lesson in level.Lessons)
            {
                if (lesson.Id <= 0)
                {
                    continue;
                }

                await db.InsertAsync(new Lesson
                {
                    Id = lesson.Id,
                    LevelId = level.Id,
                    Number = lesson.Number,
                    Title = lesson.Title
                });

                foreach (var item in lesson.Items)
                {
                    if (item.Id <= 0 || string.IsNullOrWhiteSpace(item.Grapheme))
                    {
                        continue;
                    }

                    await db.InsertAsync(new PhonicsItem
                    {
                        Id = item.Id,
                        LessonId = lesson.Id,
                        Grapheme = item.Grapheme,
                        AudioKey = item.AudioKey,
                        ExampleWord = item.ExampleWord,
                        Gloss = item.Gloss,
                        ImageKey = item.ImageKey,
                        SortOrder = item.SortOrder
                    });
                }
            }
        }

        Preferences.Default.Set(VersionKey, pack.Version);
        return true;
    }
}
```

- [ ] **Step 4: Run the seeder at startup**

In `MauiProgram.cs` add `builder.Services.AddSingleton<IContentSeeder, ContentSeeder>();`.

In `TongaKids/App.xaml.cs`, seed before the first navigation:

```csharp
using TongaKids.Data;

namespace TongaKids;

public partial class App : Application
{
    private readonly IContentSeeder _seeder;

    public App(IContentSeeder seeder)
    {
        InitializeComponent();
        _seeder = seeder;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }

    protected override async void OnStart()
    {
        base.OnStart();
        try
        {
            await _seeder.SeedIfNeededAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App] seeding failed: {ex}");
        }
    }
}
```

Register the app itself for injection by adding to `MauiProgram.cs`:
`builder.Services.AddSingleton<App>();`

- [ ] **Step 5: Write the audio manifest tool**

Create `tools/generate-audio-manifest.py`:

```python
#!/usr/bin/env python3
"""Generate the Chitonga audio recording list from the content pack.

Every audioKey in the content pack is exactly one clip to record. Re-running
after content edits reports which clips are new and which are now orphaned.
"""
import json
import os
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
PACK = ROOT / "TongaKids/Resources/Raw/content/chitonga-content.json"
AUDIO_DIR = ROOT / "TongaKids/Resources/Raw/audio"
OUT = ROOT / "docs/audio-recording-list.md"

pack = json.loads(PACK.read_text(encoding="utf-8"))

clips = []
for level in pack["levels"]:
    for lesson in level["lessons"]:
        for item in lesson["items"]:
            key = item.get("audioKey", "").strip()
            if key:
                clips.append((key, item["grapheme"], level["title"], lesson["title"]))

existing = {p.stem for p in AUDIO_DIR.glob("*.m4a")} if AUDIO_DIR.is_dir() else set()
needed = {c[0] for c in clips}

lines = [
    "# Chitonga audio recording list",
    "",
    "Generated by `tools/generate-audio-manifest.py`. Do not edit by hand.",
    "",
    "**Format:** mono, 44.1 kHz, `.m4a`. Save into `TongaKids/Resources/Raw/audio/`",
    "using exactly the filename in the first column.",
    "",
    f"**Status:** {len(needed - existing)} to record, {len(needed & existing)} done.",
    "",
    "| Filename | Say this | Level | Lesson | Recorded |",
    "|---|---|---|---|---|",
]
for key, grapheme, level_title, lesson_title in clips:
    mark = "yes" if key in existing else " "
    lines.append(f"| `{key}.m4a` | **{grapheme}** | {level_title} | {lesson_title} | {mark} |")

orphans = sorted(existing - needed)
if orphans:
    lines += ["", "## Orphaned clips", "",
              "These files exist but no content references them:", ""]
    lines += [f"- `{o}.m4a`" for o in orphans]

OUT.parent.mkdir(parents=True, exist_ok=True)
OUT.write_text("\n".join(lines) + "\n", encoding="utf-8")
print(f"Wrote {OUT.relative_to(ROOT)}: {len(clips)} clips, "
      f"{len(needed - existing)} still to record.")
```

Run:

```bash
mkdir -p TongaKids/Resources/Raw/audio
python3 tools/generate-audio-manifest.py
```

Expected: `Wrote docs/audio-recording-list.md: 9 clips, 9 still to record.`

- [ ] **Step 6: Verify seeding on the emulator**

Run: `dotnet build TongaKids/TongaKids.csproj -f net10.0-android -t:Run`

Temporarily add to `App.OnStart` after the seed call, to prove rows landed:

```csharp
var repo = Handler!.MauiContext!.Services.GetRequiredService<IContentRepository>();
var levels = await repo.GetLevelsAsync();
System.Diagnostics.Debug.WriteLine($"[App] seeded {levels.Count} levels");
```

Expected in the debug output: `[App] seeded 4 levels`. Remove the temporary lines once seen.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "Add content pack, seeder, and audio manifest tool

Content is a versioned JSON pack seeded into SQLite on first launch,
touching content tables only. Empty exampleWord fields are deliberate:
the owner authors the Chitonga, and the UI degrades gracefully until then.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 6: PhonicsEngine — minimal-pair quiz generation

**Files:**
- Create: `TongaKids/Services/IPhonicsEngine.cs`, `TongaKids/Services/PhonicsEngine.cs`
- Modify: `TongaKids/MauiProgram.cs`

**Interfaces:**
- Consumes: `PhonicsItem` (Task 4).
- Produces:
  - `record PhonicsQuestion(PhonicsItem Target, IReadOnlyList<PhonicsItem> Options)`
  - `IPhonicsEngine.BuildQuiz(IReadOnlyList<PhonicsItem> lessonItems, IReadOnlyList<PhonicsItem> pool, int optionCount, int seed) → IReadOnlyList<PhonicsQuestion>`
  - `PhonicsEngine.MinimalPairScore(string a, string b) → int` (internal, exercised by Task 8)

This is where Chitonga's transparent orthography is used concretely rather than merely
cited. Distractors are **minimal pairs** of the target — same length, differing in exactly
one position (BA against DA, MA, PA). A child must discriminate the actual sound rather
than guess from word shape.

- [ ] **Step 1: Write the interface**

`TongaKids/Services/IPhonicsEngine.cs`:

```csharp
using TongaKids.Models;

namespace TongaKids.Services;

/// <summary>One quiz question: the item being tested, and the cards to show.</summary>
public sealed record PhonicsQuestion(PhonicsItem Target, IReadOnlyList<PhonicsItem> Options);

public interface IPhonicsEngine
{
    /// <summary>
    /// Builds one question per lesson item. Distractors are drawn from the pool,
    /// preferring minimal pairs of the target.
    /// </summary>
    /// <param name="seed">Fixed seed makes a quiz reproducible, which is what
    /// lets the self-check assert on the result.</param>
    IReadOnlyList<PhonicsQuestion> BuildQuiz(
        IReadOnlyList<PhonicsItem> lessonItems,
        IReadOnlyList<PhonicsItem> pool,
        int optionCount,
        int seed);
}
```

- [ ] **Step 2: Write the implementation**

`TongaKids/Services/PhonicsEngine.cs`:

```csharp
using TongaKids.Models;

namespace TongaKids.Services;

public sealed class PhonicsEngine : IPhonicsEngine
{
    public IReadOnlyList<PhonicsQuestion> BuildQuiz(
        IReadOnlyList<PhonicsItem> lessonItems,
        IReadOnlyList<PhonicsItem> pool,
        int optionCount,
        int seed)
    {
        if (lessonItems.Count == 0 || optionCount < 2)
        {
            return [];
        }

        var rng = new Random(seed);
        var questions = new List<PhonicsQuestion>(lessonItems.Count);

        foreach (var target in lessonItems)
        {
            var distractors = pool
                .Where(p => p.Id != target.Id)
                .OrderByDescending(p => MinimalPairScore(target.Grapheme, p.Grapheme))
                .ThenBy(_ => rng.Next())
                .Take(optionCount - 1)
                .ToList();

            var options = distractors
                .Append(target)
                .OrderBy(_ => rng.Next())
                .ToList();

            questions.Add(new PhonicsQuestion(target, options));
        }

        return questions;
    }

    /// <summary>
    /// 2 = a true minimal pair (same length, one differing position).
    /// 1 = same length but further apart. 0 = different length.
    /// </summary>
    internal static int MinimalPairScore(string a, string b)
    {
        if (a.Length != b.Length || a.Length == 0)
        {
            return 0;
        }

        var differences = 0;
        for (var i = 0; i < a.Length; i++)
        {
            if (!char.ToUpperInvariant(a[i]).Equals(char.ToUpperInvariant(b[i])))
            {
                differences++;
            }
        }

        return differences == 1 ? 2 : 1;
    }
}
```

- [ ] **Step 3: Register and build**

Add to `MauiProgram.cs`: `builder.Services.AddSingleton<IPhonicsEngine, PhonicsEngine>();`

Run: `dotnet build TongaKids/TongaKids.csproj -f net10.0-android`
Expected: `Build succeeded`.

Assertions for this engine are written in Task 8 and run there.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "Add PhonicsEngine with minimal-pair distractor selection

Distractors differ from the target in exactly one position, so the child
must discriminate the sound rather than the word shape. This is the
transparent-orthography claim made concrete.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 7: ProgressCalculator and MasteryEvaluator

**Files:**
- Create: `TongaKids/Services/IProgressCalculator.cs`, `ProgressCalculator.cs`
- Create: `TongaKids/Services/IMasteryEvaluator.cs`, `MasteryEvaluator.cs`
- Modify: `TongaKids/MauiProgram.cs`

**Interfaces:**
- Produces:
  - `IProgressCalculator.AccuracyPercent(int correct, int total) → int`
  - `IProgressCalculator.StarsFor(int accuracyPercent) → int`
  - `IProgressCalculator.WordsPerMinute(int wordsRead, long durationMs) → double`
  - `enum MasteryOutcome { Mastered, NeedsRemediation }`
  - `IMasteryEvaluator.Evaluate(int accuracyPercent) → MasteryOutcome`
  - `MasteryEvaluator.MasteryThresholdPercent` (const int = 80)

- [ ] **Step 1: Write ProgressCalculator**

`TongaKids/Services/IProgressCalculator.cs`:

```csharp
namespace TongaKids.Services;

public interface IProgressCalculator
{
    /// <summary>Rounded percentage. Zero questions scores 0, never divides by zero.</summary>
    int AccuracyPercent(int correct, int total);

    /// <summary>DESIGN.md five-star layout: 95/85/80/60 thresholds.</summary>
    int StarsFor(int accuracyPercent);

    /// <summary>Reading fluency. Zero or negative duration returns 0.</summary>
    double WordsPerMinute(int wordsRead, long durationMs);
}
```

`TongaKids/Services/ProgressCalculator.cs`:

```csharp
namespace TongaKids.Services;

public sealed class ProgressCalculator : IProgressCalculator
{
    public int AccuracyPercent(int correct, int total)
    {
        if (total <= 0)
        {
            return 0;
        }

        var clamped = Math.Clamp(correct, 0, total);
        return (int)Math.Round(clamped * 100.0 / total, MidpointRounding.AwayFromZero);
    }

    public int StarsFor(int accuracyPercent) => accuracyPercent switch
    {
        >= 95 => 5,
        >= 85 => 4,
        >= 80 => 3,
        >= 60 => 2,
        _ => 1
    };

    public double WordsPerMinute(int wordsRead, long durationMs)
    {
        if (durationMs <= 0 || wordsRead <= 0)
        {
            return 0;
        }

        return wordsRead / (durationMs / 60000.0);
    }
}
```

- [ ] **Step 2: Write MasteryEvaluator**

`TongaKids/Services/IMasteryEvaluator.cs`:

```csharp
namespace TongaKids.Services;

public enum MasteryOutcome
{
    Mastered,
    NeedsRemediation
}

public interface IMasteryEvaluator
{
    MasteryOutcome Evaluate(int accuracyPercent);
}
```

`TongaKids/Services/MasteryEvaluator.cs`:

```csharp
namespace TongaKids.Services;

/// <summary>
/// The guard condition from the project report's state diagram (Figure 9):
/// at or above the threshold the learner advances; below it they return to
/// Active Learning for remediation. No penalty, no lost progress.
/// </summary>
public sealed class MasteryEvaluator : IMasteryEvaluator
{
    /// <summary>The single definition of the mastery rule. Never inline this number.</summary>
    public const int MasteryThresholdPercent = 80;

    public MasteryOutcome Evaluate(int accuracyPercent) =>
        accuracyPercent >= MasteryThresholdPercent
            ? MasteryOutcome.Mastered
            : MasteryOutcome.NeedsRemediation;
}
```

- [ ] **Step 3: Register and build**

Add to `MauiProgram.cs`:

```csharp
builder.Services.AddSingleton<IProgressCalculator, ProgressCalculator>();
builder.Services.AddSingleton<IMasteryEvaluator, MasteryEvaluator>();
```

Run: `dotnet build TongaKids/TongaKids.csproj -f net10.0-android`
Expected: `Build succeeded`.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "Add ProgressCalculator and MasteryEvaluator

Accuracy, the DESIGN.md five-star mapping, and words-per-minute. The 80%
mastery threshold from the report's state diagram is declared exactly once.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 8: Self-check harness — verification for all three engines

**Files:**
- Create: `TongaKids/Services/SelfCheck/SelfCheckResult.cs`, `ISelfCheck.cs`
- Create: `TongaKids/Services/SelfCheck/PhonicsEngineSelfCheck.cs`
- Create: `TongaKids/Services/SelfCheck/ProgressSelfCheck.cs`
- Create: `TongaKids/Views/SelfCheckPage.xaml(.cs)`
- Modify: `TongaKids/MauiProgram.cs`, `TongaKids/AppShell.xaml.cs`

**Interfaces:**
- Consumes: `IPhonicsEngine`, `IProgressCalculator`, `IMasteryEvaluator`.
- Produces:
  - `record SelfCheckResult(string Name, bool Passed, string Detail)`
  - `ISelfCheck` with `string Area { get; }` and `IReadOnlyList<SelfCheckResult> Run()`
  - Route `selfcheck` (DEBUG builds only)

This is the answer to "how did you verify the 80% rule?" — cases written before the
behaviour, run on the device, pass or fail visibly. The cases are plain input/output with
no MAUI dependency, so they port unchanged into a real test project if one is approved.

- [ ] **Step 1: Write the harness types**

`TongaKids/Services/SelfCheck/SelfCheckResult.cs`:

```csharp
namespace TongaKids.Services.SelfCheck;

public sealed record SelfCheckResult(string Name, bool Passed, string Detail);

public interface ISelfCheck
{
    string Area { get; }

    IReadOnlyList<SelfCheckResult> Run();
}
```

- [ ] **Step 2: Write the assertion cases and watch them fail**

`TongaKids/Services/SelfCheck/ProgressSelfCheck.cs`:

```csharp
namespace TongaKids.Services.SelfCheck;

public sealed class ProgressSelfCheck(
    IProgressCalculator calculator,
    IMasteryEvaluator evaluator) : ISelfCheck
{
    public string Area => "Progress and mastery";

    public IReadOnlyList<SelfCheckResult> Run()
    {
        var results = new List<SelfCheckResult>();

        void Check(string name, object expected, object actual) =>
            results.Add(new SelfCheckResult(
                name,
                Equals(expected, actual),
                $"expected {expected}, got {actual}"));

        // Accuracy
        Check("8 of 10 is 80%", 80, calculator.AccuracyPercent(8, 10));
        Check("0 of 0 is 0% (no divide by zero)", 0, calculator.AccuracyPercent(0, 0));
        Check("3 of 7 rounds to 43%", 43, calculator.AccuracyPercent(3, 7));
        Check("correct above total is clamped", 100, calculator.AccuracyPercent(99, 10));

        // Stars — DESIGN.md thresholds
        Check("100% earns 5 stars", 5, calculator.StarsFor(100));
        Check("95% earns 5 stars", 5, calculator.StarsFor(95));
        Check("94% earns 4 stars", 4, calculator.StarsFor(94));
        Check("85% earns 4 stars", 4, calculator.StarsFor(85));
        Check("80% earns 3 stars", 3, calculator.StarsFor(80));
        Check("60% earns 2 stars", 2, calculator.StarsFor(60));
        Check("59% earns 1 star", 1, calculator.StarsFor(59));

        // Words per minute
        Check("60 words in 60s is 60 wpm", 60d, calculator.WordsPerMinute(60, 60_000));
        Check("30 words in 30s is 60 wpm", 60d, calculator.WordsPerMinute(30, 30_000));
        Check("zero duration is 0 wpm", 0d, calculator.WordsPerMinute(50, 0));

        // The report's guard condition
        Check("80% is mastered", MasteryOutcome.Mastered, evaluator.Evaluate(80));
        Check("79% needs remediation", MasteryOutcome.NeedsRemediation, evaluator.Evaluate(79));
        Check("100% is mastered", MasteryOutcome.Mastered, evaluator.Evaluate(100));
        Check("0% needs remediation", MasteryOutcome.NeedsRemediation, evaluator.Evaluate(0));

        return results;
    }
}
```

`TongaKids/Services/SelfCheck/PhonicsEngineSelfCheck.cs`:

```csharp
using TongaKids.Models;

namespace TongaKids.Services.SelfCheck;

public sealed class PhonicsEngineSelfCheck(IPhonicsEngine engine) : ISelfCheck
{
    public string Area => "Phonics engine";

    private static PhonicsItem Item(int id, string grapheme) =>
        new() { Id = id, Grapheme = grapheme, LessonId = 1, SortOrder = id };

    public IReadOnlyList<SelfCheckResult> Run()
    {
        var results = new List<SelfCheckResult>();

        void Check(string name, object expected, object actual) =>
            results.Add(new SelfCheckResult(
                name,
                Equals(expected, actual),
                $"expected {expected}, got {actual}"));

        // Minimal-pair scoring
        Check("BA vs DA is a minimal pair", 2, PhonicsEngine.MinimalPairScore("BA", "DA"));
        Check("BA vs BE is a minimal pair", 2, PhonicsEngine.MinimalPairScore("BA", "BE"));
        Check("BA vs DE differs in two places", 1, PhonicsEngine.MinimalPairScore("BA", "DE"));
        Check("BA vs BAA has a different length", 0, PhonicsEngine.MinimalPairScore("BA", "BAA"));
        Check("BA vs BA is identical, not minimal", 1, PhonicsEngine.MinimalPairScore("BA", "BA"));

        // Quiz construction
        var target = Item(1, "BA");
        var lesson = new List<PhonicsItem> { target };
        var pool = new List<PhonicsItem>
        {
            target, Item(2, "DA"), Item(3, "MA"), Item(4, "PA"), Item(5, "TAMBO")
        };

        var quiz = engine.BuildQuiz(lesson, pool, optionCount: 4, seed: 42);

        Check("one question per lesson item", 1, quiz.Count);
        Check("four options are offered", 4, quiz[0].Options.Count);
        Check("the target is among the options", true, quiz[0].Options.Any(o => o.Id == target.Id));
        Check("no option appears twice", 4, quiz[0].Options.Select(o => o.Id).Distinct().Count());
        Check("the long word is not chosen over minimal pairs", false,
            quiz[0].Options.Any(o => o.Grapheme == "TAMBO"));

        // Reproducibility — the same seed must give the same quiz.
        var again = engine.BuildQuiz(lesson, pool, optionCount: 4, seed: 42);
        Check("the same seed gives the same order", true,
            quiz[0].Options.Select(o => o.Id).SequenceEqual(again[0].Options.Select(o => o.Id)));

        // Degenerate input must not throw.
        Check("an empty lesson gives no questions", 0,
            engine.BuildQuiz([], pool, 4, 1).Count);
        Check("a pool smaller than optionCount still works", 2,
            engine.BuildQuiz(lesson, [target, Item(2, "DA")], 4, 1)[0].Options.Count);

        return results;
    }
}
```

- [ ] **Step 3: Write the self-check page**

`TongaKids/Views/SelfCheckPage.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="TongaKids.Views.SelfCheckPage"
             Title="Engine self-check">
    <ScrollView>
        <VerticalStackLayout Padding="{StaticResource ScreenPadding}" Spacing="12">
            <Label x:Name="SummaryLabel" Style="{StaticResource TitleLg}" />
            <VerticalStackLayout x:Name="ResultsLayout" Spacing="8" />
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

`TongaKids/Views/SelfCheckPage.xaml.cs`:

```csharp
using TongaKids.Services.SelfCheck;

namespace TongaKids.Views;

public partial class SelfCheckPage : ContentPage
{
    public SelfCheckPage(IEnumerable<ISelfCheck> checks)
    {
        InitializeComponent();

        var passed = 0;
        var total = 0;

        foreach (var check in checks)
        {
            ResultsLayout.Add(new Label
            {
                Text = check.Area,
                Style = (Style)Resources["LabelLg"] ?? Application.Current!.Resources["LabelLg"] as Style
            });

            foreach (var result in check.Run())
            {
                total++;
                if (result.Passed)
                {
                    passed++;
                }

                ResultsLayout.Add(new Label
                {
                    Text = $"{(result.Passed ? "PASS" : "FAIL")}  {result.Name}"
                           + (result.Passed ? string.Empty : $"  ({result.Detail})"),
                    FontFamily = "NunitoRegular",
                    FontSize = 13,
                    TextColor = result.Passed
                        ? Color.FromArgb("#006e1c")
                        : Color.FromArgb("#ba1a1a")
                });
            }
        }

        SummaryLabel.Text = $"{passed} of {total} checks passed";
    }
}
```

- [ ] **Step 4: Register, DEBUG only**

In `MauiProgram.cs`:

```csharp
#if DEBUG
builder.Services.AddSingleton<ISelfCheck, ProgressSelfCheck>();
builder.Services.AddSingleton<ISelfCheck, PhonicsEngineSelfCheck>();
builder.Services.AddTransient<SelfCheckPage>();
#endif
```

Add `using TongaKids.Services.SelfCheck;`. In `AppShell.xaml.cs`:

```csharp
#if DEBUG
Routing.RegisterRoute("selfcheck", typeof(SelfCheckPage));
#endif
```

- [ ] **Step 5: Run the self-check**

Temporarily navigate to it on startup by adding to `App.OnStart` after seeding:

```csharp
await Shell.Current.GoToAsync("selfcheck");
```

Run: `dotnet build TongaKids/TongaKids.csproj -f net10.0-android -t:Run`

Expected: **`28 of 28 checks passed`**, every line green.

Any FAIL line names the case and shows expected versus actual — fix the engine, not the
assertion, unless the assertion misreads DESIGN.md. Remove the temporary navigation once
the run is green; the page stays reachable from Settings in Plan 3.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "Add in-app self-check harness for the three engines

28 assertion cases covering accuracy rounding, the DESIGN.md star
thresholds, words-per-minute, the 80% mastery guard, and minimal-pair
quiz construction including reproducibility and degenerate input.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 9: Splash and Onboarding

**Files:**
- Modify: `TongaKids/Views/SplashPage.xaml(.cs)`, `OnboardingPage.xaml(.cs)`
- Create: `TongaKids/ViewModels/OnboardingViewModel.cs`
- Modify: `TongaKids/MauiProgram.cs`

**Interfaces:**
- Consumes: styles (Tasks 1–2), routes (Task 3).
- Produces: navigation contract — Splash decides the next screen; onboarding completion is
  stored in `Preferences` under key `onboarding_complete` (bool).

Reference: `Resources/Raw/stitch_tongakids_read_design_spec/splash_screen/screen.png` and
`onboarding_learn_sounds/screen.png`.

- [ ] **Step 1: Write SplashPage**

Warm-orange gradient to cream, the baobab illustration, wordmark, tagline, and a
progress pill. It waits briefly, then routes onward.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="TongaKids.Views.SplashPage"
             Shell.NavBarIsVisible="False">
    <ContentPage.Background>
        <LinearGradientBrush StartPoint="0,0" EndPoint="0,1">
            <GradientStop Color="{StaticResource PrimaryContainer}" Offset="0.0" />
            <GradientStop Color="{StaticResource PrimaryFixed}" Offset="0.55" />
            <GradientStop Color="{StaticResource Surface}" Offset="1.0" />
        </LinearGradientBrush>
    </ContentPage.Background>

    <VerticalStackLayout Padding="{StaticResource ScreenPadding}"
                         Spacing="16" VerticalOptions="Center">

        <Image Source="illus_splash_baobab.png"
               HeightRequest="280" Aspect="AspectFit" />

        <Label Text="TongaKids Read" Style="{StaticResource DisplayLg}"
               TextColor="{StaticResource Primary}"
               HorizontalTextAlignment="Center" />

        <HorizontalStackLayout HorizontalOptions="Center" Spacing="10">
            <Label Text="Learn" Style="{StaticResource TitleLg}" />
            <Label Text="•" Style="{StaticResource TitleLg}"
                   TextColor="{StaticResource PrimaryContainer}" />
            <Label Text="Read" Style="{StaticResource TitleLg}" />
            <Label Text="•" Style="{StaticResource TitleLg}"
                   TextColor="{StaticResource PrimaryContainer}" />
            <Label Text="Grow" Style="{StaticResource TitleLg}" />
        </HorizontalStackLayout>

        <ProgressBar x:Name="LoadingBar" Progress="0.4"
                     Style="{StaticResource PillProgress}"
                     WidthRequest="240" HorizontalOptions="Center" Margin="0,40,0,0" />

        <Label Text="LOADING ADVENTURES..." Style="{StaticResource LabelLg}"
               TextColor="{StaticResource OnSurfaceVariant}"
               CharacterSpacing="2" HorizontalTextAlignment="Center" />
    </VerticalStackLayout>
</ContentPage>
```

```csharp
namespace TongaKids.Views;

public partial class SplashPage : ContentPage
{
    public SplashPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadingBar.ProgressTo(1.0, 1200, Easing.CubicInOut);

        var seenOnboarding = Preferences.Default.Get("onboarding_complete", false);
        await Shell.Current.GoToAsync(seenOnboarding ? "//profiles" : "//onboarding");
    }
}
```

- [ ] **Step 2: Write the onboarding view model**

`TongaKids/ViewModels/OnboardingViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TongaKids.ViewModels;

public sealed record OnboardingSlide(string ImageKey, string Title, string Body);

public sealed partial class OnboardingViewModel : ObservableObject
{
    public IReadOnlyList<OnboardingSlide> Slides { get; } =
    [
        new("illus_onboarding_blocks", "Learn Chitonga Sounds",
            "Master letters and syllables through fun activities."),
        new("illus_lion_reading", "Read Together",
            "Enjoy illustrated folktales with audio narration."),
        new("badge_master_reader", "Earn Your Stars",
            "Track your progress and collect rewards as you grow.")
    ];

    [ObservableProperty]
    private int _position;

    public bool IsLastSlide => Position >= Slides.Count - 1;

    public string NextLabel => IsLastSlide ? "Start" : "Next";

    partial void OnPositionChanged(int value)
    {
        OnPropertyChanged(nameof(IsLastSlide));
        OnPropertyChanged(nameof(NextLabel));
    }

    [RelayCommand]
    private async Task NextAsync()
    {
        if (!IsLastSlide)
        {
            Position++;
            return;
        }

        await FinishAsync();
    }

    [RelayCommand]
    private async Task SkipAsync() => await FinishAsync();

    private static async Task FinishAsync()
    {
        Preferences.Default.Set("onboarding_complete", true);
        await Shell.Current.GoToAsync("//profiles");
    }
}
```

- [ ] **Step 3: Write OnboardingPage**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:controls="clr-namespace:TongaKids.Controls"
             xmlns:vm="clr-namespace:TongaKids.ViewModels"
             x:Class="TongaKids.Views.OnboardingPage"
             x:DataType="vm:OnboardingViewModel"
             Shell.NavBarIsVisible="False">

    <Grid RowDefinitions="*,Auto" Padding="{StaticResource ScreenPadding}">

        <CarouselView Grid.Row="0"
                      ItemsSource="{Binding Slides}"
                      Position="{Binding Position, Mode=TwoWay}"
                      IndicatorView="Indicator"
                      Loop="False">
            <CarouselView.ItemTemplate>
                <DataTemplate x:DataType="vm:OnboardingSlide">
                    <VerticalStackLayout Spacing="24" VerticalOptions="Center">
                        <Border BackgroundColor="{StaticResource PrimaryFixed}"
                                StrokeThickness="0" HeightRequest="320">
                            <Border.StrokeShape>
                                <RoundRectangle CornerRadius="24" />
                            </Border.StrokeShape>
                            <Image Source="{Binding ImageKey, StringFormat='{0}.png'}"
                                   Aspect="AspectFit" Margin="24" />
                        </Border>
                        <Label Text="{Binding Title}" Style="{StaticResource HeadlineLg}"
                               TextColor="{StaticResource Primary}"
                               HorizontalTextAlignment="Center" />
                        <Label Text="{Binding Body}" Style="{StaticResource BodyLg}"
                               HorizontalTextAlignment="Center" />
                    </VerticalStackLayout>
                </DataTemplate>
            </CarouselView.ItemTemplate>
        </CarouselView>

        <VerticalStackLayout Grid.Row="1" Spacing="24">
            <IndicatorView x:Name="Indicator"
                           HorizontalOptions="Center"
                           IndicatorColor="{StaticResource SurfaceContainerHighest}"
                           SelectedIndicatorColor="{StaticResource Primary}"
                           IndicatorSize="10" />

            <Grid ColumnDefinitions="Auto,*,Auto" ColumnSpacing="16">
                <Label Grid.Column="0" Text="Skip" Style="{StaticResource TitleLg}"
                       TextColor="{StaticResource Secondary}"
                       VerticalOptions="Center">
                    <Label.GestureRecognizers>
                        <TapGestureRecognizer Command="{Binding SkipCommand}" />
                    </Label.GestureRecognizers>
                </Label>

                <controls:TactileButton Grid.Column="2"
                                        Text="{Binding NextLabel}"
                                        BackgroundFill="{StaticResource PrimaryContainer}"
                                        DepthColor="{StaticResource OnPrimaryContainer}"
                                        TextColor="{StaticResource OnPrimary}"
                                        Command="{Binding NextCommand}"
                                        WidthRequest="180" />
            </Grid>
        </VerticalStackLayout>
    </Grid>
</ContentPage>
```

```csharp
using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class OnboardingPage : ContentPage
{
    public OnboardingPage(OnboardingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
```

- [ ] **Step 4: Register and run**

Add `builder.Services.AddTransient<OnboardingViewModel>();` to `MauiProgram.cs`
(with `using TongaKids.ViewModels;`).

Run: `dotnet build TongaKids/TongaKids.csproj -f net10.0-android -t:Run`

Expected: splash shows the baobab and fills its progress bar, then onboarding appears.
Swiping advances the dots. "Next" on slide 3 reads "Start" and navigates to profiles.
Relaunching the app skips onboarding and goes straight to profiles.

Compare side by side with `splash_screen/screen.png` and `onboarding_learn_sounds/screen.png`.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "Add splash and onboarding screens

Splash routes to onboarding on first launch and to profile selection
thereafter. Onboarding is a three-slide carousel, skippable.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 10: Profile selection and Add Learner

**Files:**
- Modify: `TongaKids/Views/ProfileSelectionPage.xaml(.cs)`
- Create: `TongaKids/ViewModels/ProfileSelectionViewModel.cs`
- Modify: `TongaKids/MauiProgram.cs`

**Interfaces:**
- Consumes: `ILearnerRepository`, `ILearnerSession`.
- Produces: on selection, sets `ILearnerSession.Current` and navigates to `//main/home`.

Reference: `profile_selection/screen.png`. No login anywhere — a child taps their own face
(spec §3.2). Adding a learner asks only for a first name; the avatar and accent colour are
assigned by cycling the three shipped avatars.

> **Deviation from spec §7:** the spec lists "Add Learner" as its own screen. This plan
> implements it as a single `DisplayPromptAsync` for the name. A dedicated screen would
> only add an avatar picker, which is not needed to demonstrate multi-child self-paced
> learning. If the owner wants avatar choice, it becomes its own task rather than growing
> this one.

- [ ] **Step 1: Write the view model**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Data;
using TongaKids.Models;
using TongaKids.Services;

namespace TongaKids.ViewModels;

public sealed partial class ProfileSelectionViewModel(
    ILearnerRepository learners,
    ILearnerSession session) : ObservableObject
{
    /// <summary>Avatar art shipped with the app, cycled for new learners.</summary>
    private static readonly string[] AvatarKeys =
        ["avatar_chipo", "avatar_mwaka", "avatar_twaambo"];

    private static readonly string[] AccentKeys =
        ["Tertiary", "SecondaryContainer", "PrimaryContainer"];

    public ObservableCollection<Learner> Learners { get; } = [];

    [ObservableProperty]
    private bool _isBusy;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            Learners.Clear();
            foreach (var learner in await learners.GetAllAsync())
            {
                Learners.Add(learner);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Profiles] load failed: {ex}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SelectAsync(Learner? learner)
    {
        if (learner is null)
        {
            return;
        }

        session.SetCurrent(learner);
        await learners.TouchAsync(learner.Id);
        await Shell.Current.GoToAsync("//main/home");
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var page = Application.Current?.Windows[0].Page;
        if (page is null)
        {
            return;
        }

        var name = await page.DisplayPromptAsync(
            "Add a learner", "What is your name?", "Save", "Cancel", maxLength: 20);

        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var index = Learners.Count;
        var created = await learners.AddAsync(new Learner
        {
            Name = name.Trim(),
            AvatarKey = AvatarKeys[index % AvatarKeys.Length],
            AccentColorKey = AccentKeys[index % AccentKeys.Length]
        });

        Learners.Add(created);
    }
}
```

- [ ] **Step 2: Write the page**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:models="clr-namespace:TongaKids.Models"
             xmlns:vm="clr-namespace:TongaKids.ViewModels"
             x:Class="TongaKids.Views.ProfileSelectionPage"
             x:DataType="vm:ProfileSelectionViewModel"
             Shell.NavBarIsVisible="False">

    <ScrollView>
        <VerticalStackLayout Padding="{StaticResource ScreenPadding}" Spacing="24">

            <Label Text="TongaKids Read" Style="{StaticResource TitleLg}"
                   TextColor="{StaticResource Primary}" HorizontalTextAlignment="Center" />

            <VerticalStackLayout Spacing="4">
                <Label Text="Who is learning today?" Style="{StaticResource HeadlineLg}"
                       HorizontalTextAlignment="Center" />
                <Label Text="Choose your profile to start reading!"
                       Style="{StaticResource BodyMd}" HorizontalTextAlignment="Center" />
            </VerticalStackLayout>

            <CollectionView ItemsSource="{Binding Learners}" SelectionMode="None">
                <CollectionView.ItemsLayout>
                    <LinearItemsLayout Orientation="Vertical" ItemSpacing="20" />
                </CollectionView.ItemsLayout>
                <CollectionView.ItemTemplate>
                    <DataTemplate x:DataType="models:Learner">
                        <VerticalStackLayout Spacing="8" HorizontalOptions="Center">
                            <VerticalStackLayout.GestureRecognizers>
                                <TapGestureRecognizer
                                    Command="{Binding Source={RelativeSource AncestorType={x:Type vm:ProfileSelectionViewModel}}, Path=SelectCommand}"
                                    CommandParameter="{Binding .}" />
                            </VerticalStackLayout.GestureRecognizers>

                            <!-- 120dp face: far above the 48dp minimum, easy for small hands. -->
                            <Border HeightRequest="120" WidthRequest="120"
                                    StrokeThickness="4"
                                    Stroke="{StaticResource Tertiary}"
                                    BackgroundColor="{StaticResource PrimaryFixed}">
                                <Border.StrokeShape>
                                    <RoundRectangle CornerRadius="60" />
                                </Border.StrokeShape>
                                <Image Source="{Binding AvatarKey, StringFormat='{0}.png'}"
                                       Aspect="AspectFill" />
                            </Border>

                            <Label Text="{Binding Name}" Style="{StaticResource TitleLg}"
                                   HorizontalTextAlignment="Center" />
                        </VerticalStackLayout>
                    </DataTemplate>
                </CollectionView.ItemTemplate>
            </CollectionView>

            <Border Style="{StaticResource Card}" Padding="16">
                <Border.GestureRecognizers>
                    <TapGestureRecognizer Command="{Binding AddCommand}" />
                </Border.GestureRecognizers>
                <HorizontalStackLayout Spacing="16" HorizontalOptions="Center">
                    <Label Text="add" Style="{StaticResource Icon}" FontSize="32"
                           TextColor="{StaticResource Primary}" VerticalOptions="Center" />
                    <Label Text="Add New Learner" Style="{StaticResource TitleLg}"
                           VerticalOptions="Center" />
                </HorizontalStackLayout>
            </Border>

        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

```csharp
using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class ProfileSelectionPage : ContentPage
{
    private readonly ProfileSelectionViewModel _viewModel;

    public ProfileSelectionPage(ProfileSelectionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
```

- [ ] **Step 3: Register, run, verify**

Add `builder.Services.AddTransient<ProfileSelectionViewModel>();`.

Run: `dotnet build TongaKids/TongaKids.csproj -f net10.0-android -t:Run`

Expected: on a fresh install the list is empty. Tap "Add New Learner", enter "Chipo",
and a circular avatar appears. Tapping it navigates to Home with the tab bar visible.
Force-close and relaunch: Chipo is still listed (proving SQLite persistence).

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "Add profile selection and learner creation

A child taps their own face; there is no login on any path a child
reaches. Adding a learner asks only for a first name.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 11: Home dashboard

**Files:**
- Modify: `TongaKids/Views/HomePage.xaml(.cs)`
- Create: `TongaKids/ViewModels/HomeViewModel.cs`
- Modify: `TongaKids/MauiProgram.cs`

**Interfaces:**
- Consumes: `ILearnerSession`, `IContentRepository`, `IProgressRepository`, `IProgressCalculator`.
- Produces: navigation to the `levels` route from the daily-mission card.

Reference: `home_dashboard/screen.png`.

- [ ] **Step 1: Write the view model**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Data;
using TongaKids.Services;

namespace TongaKids.ViewModels;

public sealed partial class HomeViewModel(
    ILearnerSession session,
    IContentRepository content,
    IProgressRepository progress,
    IProgressCalculator calculator) : ObservableObject
{
    [ObservableProperty] private string _greeting = "Welcome back!";
    [ObservableProperty] private string _avatarKey = "avatar_chipo";
    [ObservableProperty] private string _goalSummary = "Let's begin!";
    [ObservableProperty] private double _goalProgress;
    [ObservableProperty] private string _missionSubtitle = "Start your first lesson";

    [RelayCommand]
    public async Task LoadAsync()
    {
        var learner = session.Current;
        if (learner is null)
        {
            // No profile chosen — send the child back rather than showing an error.
            await Shell.Current.GoToAsync("//profiles");
            return;
        }

        Greeting = $"Welcome back, {learner.Name}!";
        AvatarKey = learner.AvatarKey;

        try
        {
            var levels = await content.GetLevelsAsync();
            var lessonCount = 0;
            foreach (var level in levels)
            {
                lessonCount += (await content.GetLessonsAsync(level.Id)).Count;
            }

            var done = (await progress.GetForLearnerAsync(learner.Id)).Count(p => p.IsMastered);

            GoalProgress = lessonCount == 0 ? 0 : (double)done / lessonCount;
            GoalSummary = $"{calculator.AccuracyPercent(done, lessonCount)}% of today's goal";
            MissionSubtitle = done == 0
                ? "Master the letter 'A' sounds"
                : $"{done} of {lessonCount} lessons mastered";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Home] load failed: {ex}");
        }
    }

    [RelayCommand]
    private static async Task OpenPhonicsAsync() => await Shell.Current.GoToAsync("levels");
}
```

- [ ] **Step 2: Write the page**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="clr-namespace:TongaKids.ViewModels"
             x:Class="TongaKids.Views.HomePage"
             x:DataType="vm:HomeViewModel"
             Shell.NavBarIsVisible="False">

    <ScrollView>
        <VerticalStackLayout Padding="{StaticResource ScreenPadding}" Spacing="24">

            <!-- Header -->
            <Grid ColumnDefinitions="Auto,*" ColumnSpacing="16">
                <Border Grid.Column="0" HeightRequest="56" WidthRequest="56"
                        StrokeThickness="0">
                    <Border.StrokeShape>
                        <RoundRectangle CornerRadius="28" />
                    </Border.StrokeShape>
                    <Image Source="{Binding AvatarKey, StringFormat='{0}.png'}"
                           Aspect="AspectFill" />
                </Border>
                <VerticalStackLayout Grid.Column="1" VerticalOptions="Center">
                    <Label Text="{Binding Greeting}" Style="{StaticResource HeadlineLg}"
                           TextColor="{StaticResource Primary}" />
                    <Label Text="{Binding GoalSummary}" Style="{StaticResource LabelLg}" />
                </VerticalStackLayout>
            </Grid>

            <!-- Daily mission: the entry point to phonics -->
            <Border BackgroundColor="{StaticResource PrimaryContainer}"
                    StrokeThickness="0" Padding="20">
                <Border.StrokeShape>
                    <RoundRectangle CornerRadius="16" />
                </Border.StrokeShape>
                <Border.GestureRecognizers>
                    <TapGestureRecognizer Command="{Binding OpenPhonicsCommand}" />
                </Border.GestureRecognizers>
                <VerticalStackLayout Spacing="8">
                    <Border BackgroundColor="{StaticResource SurfaceContainerLowest}"
                            Padding="12,4" HorizontalOptions="Start" StrokeThickness="0">
                        <Border.StrokeShape>
                            <RoundRectangle CornerRadius="24" />
                        </Border.StrokeShape>
                        <Label Text="DAILY MISSION" Style="{StaticResource LabelLg}"
                               TextColor="{StaticResource Primary}" />
                    </Border>
                    <Label Text="Learn Phonics" Style="{StaticResource DisplayLg}"
                           TextColor="{StaticResource OnPrimary}" />
                    <Label Text="{Binding MissionSubtitle}" Style="{StaticResource BodyLg}"
                           TextColor="{StaticResource OnPrimary}" />
                </VerticalStackLayout>
            </Border>

            <!-- Story Library and Reading Challenges -->
            <Grid ColumnDefinitions="*,*" ColumnSpacing="16" HeightRequest="200">
                <Border Grid.Column="0" BackgroundColor="{StaticResource SecondaryContainer}"
                        StrokeThickness="0" Padding="16">
                    <Border.StrokeShape>
                        <RoundRectangle CornerRadius="16" />
                    </Border.StrokeShape>
                    <VerticalStackLayout VerticalOptions="End" Spacing="4">
                        <Label Text="menu_book" Style="{StaticResource Icon}" FontSize="36"
                               TextColor="{StaticResource OnSecondaryContainer}"
                               VerticalOptions="Start" />
                        <Label Text="Story Library" Style="{StaticResource TitleLg}"
                               TextColor="{StaticResource OnSecondaryContainer}" />
                        <Label Text="Coming soon" Style="{StaticResource BodyMd}"
                               TextColor="{StaticResource OnSecondaryContainer}" />
                    </VerticalStackLayout>
                </Border>

                <Border Grid.Column="1" BackgroundColor="{StaticResource TertiaryContainer}"
                        StrokeThickness="0" Padding="16">
                    <Border.StrokeShape>
                        <RoundRectangle CornerRadius="16" />
                    </Border.StrokeShape>
                    <VerticalStackLayout VerticalOptions="End" Spacing="4">
                        <Label Text="trophy" Style="{StaticResource Icon}" FontSize="36"
                               TextColor="{StaticResource OnTertiaryContainer}" />
                        <Label Text="Reading Challenges" Style="{StaticResource TitleLg}"
                               TextColor="{StaticResource OnTertiaryContainer}" />
                        <Label Text="Earn stars!" Style="{StaticResource BodyMd}"
                               TextColor="{StaticResource OnTertiaryContainer}" />
                    </VerticalStackLayout>
                </Border>
            </Grid>

            <!-- My Progress -->
            <Border Style="{StaticResource Card}">
                <VerticalStackLayout Spacing="12">
                    <Label Text="My Progress" Style="{StaticResource TitleLg}" />
                    <Label Text="See how much you've learned!" Style="{StaticResource BodyMd}" />
                    <ProgressBar Progress="{Binding GoalProgress}"
                                 Style="{StaticResource PillProgress}" />
                </VerticalStackLayout>
            </Border>

        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

```csharp
using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _viewModel;

    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
```

> "Story Library" and "Reading Challenges" are inert cards here. Story Library becomes
> live in Plan 2. Do not wire them to placeholder pages — a card that opens an empty
> screen is worse than one that plainly says "Coming soon".

- [ ] **Step 3: Register, run, verify**

Add `builder.Services.AddTransient<HomeViewModel>();`.

Run: `dotnet build TongaKids/TongaKids.csproj -f net10.0-android -t:Run`

Expected: after choosing Chipo, the header greets "Welcome back, Chipo!" with the avatar,
the orange mission card fills the width, and the two coloured tiles sit below.
Tapping the mission card opens the (still placeholder) levels page. Compare with
`home_dashboard/screen.png`.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "Add home dashboard

Greets the current learner, shows real mastery progress from the database,
and routes into phonics from the daily-mission card.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 12: Phonics levels

**Files:**
- Modify: `TongaKids/Views/PhonicsLevelsPage.xaml(.cs)`
- Create: `TongaKids/ViewModels/PhonicsLevelsViewModel.cs`, `TongaKids/ViewModels/LevelCardModel.cs`
- Modify: `TongaKids/MauiProgram.cs`

**Interfaces:**
- Consumes: `IContentRepository`, `IProgressRepository`, `ILearnerSession`, `MasteryEvaluator`.
- Produces: `LevelCardModel` with `Number`, `Title`, `Subtitle`, `IconGlyph`, `IsLocked`, `IsCurrent`, `IsComplete`, `ProgressFraction`, `ProgressLabel`, `Stars`, `ActionLabel`, `FirstLessonId`; navigation to `lesson?lessonId=N`.

Reference: `learn_phonics_levels/screen.png`. Three visual states — complete (stars +
Review), current (orange border, progress bar, PLAY), locked (dimmed with a padlock).

- [ ] **Step 1: Write the card model and view model**

`TongaKids/ViewModels/LevelCardModel.cs`:

```csharp
namespace TongaKids.ViewModels;

/// <summary>A level as the levels screen needs it: content joined to this learner's progress.</summary>
public sealed class LevelCardModel
{
    public int Number { get; init; }
    public int FirstLessonId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string IconGlyph { get; init; } = string.Empty;
    public bool IsLocked { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsComplete { get; init; }
    public double ProgressFraction { get; init; }
    public int Stars { get; init; }

    public bool IsUnlocked => !IsLocked;
    public bool HasLessons => FirstLessonId > 0;
    public string ProgressLabel => $"Progress: {ProgressFraction * 100:0}%";
    public string ActionLabel => IsComplete ? "Review" : "PLAY";
    public string LockedLabel => $"Complete Level {Number - 1} to unlock";
}
```

`TongaKids/ViewModels/PhonicsLevelsViewModel.cs`:

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Data;
using TongaKids.Services;

namespace TongaKids.ViewModels;

public sealed partial class PhonicsLevelsViewModel(
    IContentRepository content,
    IProgressRepository progress,
    ILearnerSession session) : ObservableObject
{
    public ObservableCollection<LevelCardModel> Levels { get; } = [];

    [ObservableProperty] private string _greeting = "Mwapoloka! (Hello!)";

    [RelayCommand]
    public async Task LoadAsync()
    {
        var learner = session.Current;
        if (learner is null)
        {
            await Shell.Current.GoToAsync("//profiles");
            return;
        }

        Levels.Clear();

        var levels = await content.GetLevelsAsync();
        var learnerProgress = await progress.GetForLearnerAsync(learner.Id);

        // A level is unlocked when its prerequisite level is fully mastered.
        var masteredByLevel = new Dictionary<int, bool>();

        foreach (var level in levels)
        {
            var lessons = await content.GetLessonsAsync(level.Id);
            var mastered = lessons.Count > 0 && lessons.All(l =>
                learnerProgress.Any(p => p.LessonId == l.Id && p.IsMastered));
            masteredByLevel[level.Number] = mastered;

            var completedCount = lessons.Count(l =>
                learnerProgress.Any(p => p.LessonId == l.Id && p.IsMastered));

            var isLocked = level.RequiresLevelNumber > 0
                           && !masteredByLevel.GetValueOrDefault(level.RequiresLevelNumber);

            var stars = lessons.Count == 0
                ? 0
                : learnerProgress
                    .Where(p => lessons.Any(l => l.Id == p.LessonId))
                    .Sum(p => p.StarsEarned);

            Levels.Add(new LevelCardModel
            {
                Number = level.Number,
                Title = level.Title,
                Subtitle = level.Subtitle,
                IconGlyph = level.IconGlyph,
                FirstLessonId = lessons.FirstOrDefault()?.Id ?? 0,
                IsLocked = isLocked,
                IsComplete = mastered,
                IsCurrent = !isLocked && !mastered,
                ProgressFraction = lessons.Count == 0 ? 0 : (double)completedCount / lessons.Count,
                Stars = Math.Min(stars, 3)
            });
        }
    }

    [RelayCommand]
    private static async Task OpenAsync(LevelCardModel? level)
    {
        // A locked or empty level is simply not tappable. No message, no error.
        if (level is null || level.IsLocked || !level.HasLessons)
        {
            return;
        }

        await Shell.Current.GoToAsync($"lesson?lessonId={level.FirstLessonId}");
    }
}
```

- [ ] **Step 2: Write the page**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:controls="clr-namespace:TongaKids.Controls"
             xmlns:vm="clr-namespace:TongaKids.ViewModels"
             x:Class="TongaKids.Views.PhonicsLevelsPage"
             x:DataType="vm:PhonicsLevelsViewModel"
             Title="Phonics Levels">

    <ScrollView>
        <VerticalStackLayout Padding="{StaticResource ScreenPadding}" Spacing="24">

            <VerticalStackLayout Spacing="4">
                <Label Text="{Binding Greeting}" Style="{StaticResource TitleLg}"
                       HorizontalTextAlignment="Center" />
                <Label Text="Let's continue your phonics journey today."
                       Style="{StaticResource BodyMd}" HorizontalTextAlignment="Center" />
            </VerticalStackLayout>

            <CollectionView ItemsSource="{Binding Levels}" SelectionMode="None">
                <CollectionView.ItemsLayout>
                    <LinearItemsLayout Orientation="Vertical" ItemSpacing="16" />
                </CollectionView.ItemsLayout>
                <CollectionView.ItemTemplate>
                    <DataTemplate x:DataType="vm:LevelCardModel">
                        <Border Style="{StaticResource Card}"
                                Opacity="{Binding IsLocked, Converter={StaticResource LockedOpacity}}">
                            <Border.GestureRecognizers>
                                <TapGestureRecognizer
                                    Command="{Binding Source={RelativeSource AncestorType={x:Type vm:PhonicsLevelsViewModel}}, Path=OpenCommand}"
                                    CommandParameter="{Binding .}" />
                            </Border.GestureRecognizers>

                            <VerticalStackLayout Spacing="12">
                                <Grid ColumnDefinitions="Auto,*,Auto">
                                    <Border Grid.Column="0" WidthRequest="56" HeightRequest="56"
                                            BackgroundColor="{StaticResource PrimaryFixed}"
                                            StrokeThickness="0">
                                        <Border.StrokeShape>
                                            <RoundRectangle CornerRadius="16" />
                                        </Border.StrokeShape>
                                        <Label Text="{Binding IconGlyph}"
                                               Style="{StaticResource Icon}" FontSize="28"
                                               HorizontalOptions="Center" VerticalOptions="Center" />
                                    </Border>

                                    <Label Grid.Column="2" Text="lock"
                                           Style="{StaticResource Icon}" FontSize="32"
                                           IsVisible="{Binding IsLocked}"
                                           TextColor="{StaticResource Outline}" />
                                </Grid>

                                <Label Text="{Binding Title}" Style="{StaticResource TitleLg}" />
                                <Label Text="{Binding Subtitle}" Style="{StaticResource BodyMd}" />

                                <ProgressBar Progress="{Binding ProgressFraction}"
                                             Style="{StaticResource PillProgress}"
                                             IsVisible="{Binding IsCurrent}" />

                                <Grid ColumnDefinitions="*,Auto" IsVisible="{Binding IsUnlocked}">
                                    <Label Grid.Column="0" Text="{Binding ProgressLabel}"
                                           Style="{StaticResource LabelLg}" VerticalOptions="Center" />
                                    <controls:TactileButton Grid.Column="1"
                                        Text="{Binding ActionLabel}"
                                        BackgroundFill="{StaticResource PrimaryContainer}"
                                        DepthColor="{StaticResource OnPrimaryContainer}"
                                        TextColor="{StaticResource OnPrimary}"
                                        CornerRadius="24"
                                        IsVisible="{Binding HasLessons}"
                                        Command="{Binding Source={RelativeSource AncestorType={x:Type vm:PhonicsLevelsViewModel}}, Path=OpenCommand}"
                                        CommandParameter="{Binding .}" />
                                </Grid>

                                <Label Text="{Binding LockedLabel}" Style="{StaticResource LabelLg}"
                                       TextColor="{StaticResource Outline}"
                                       IsVisible="{Binding IsLocked}" />
                            </VerticalStackLayout>
                        </Border>
                    </DataTemplate>
                </CollectionView.ItemTemplate>
            </CollectionView>

        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

- [ ] **Step 3: Add the opacity converter**

Create `TongaKids/Converters/LockedOpacityConverter.cs`:

```csharp
using System.Globalization;

namespace TongaKids.Converters;

/// <summary>Locked levels render dimmed, per the mockup.</summary>
public sealed class LockedOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? 0.45 : 1.0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
```

Register it in `App.xaml` inside `<ResourceDictionary>` (after the merged dictionaries),
adding `xmlns:converters="clr-namespace:TongaKids.Converters"` to the `Application` tag:

```xml
<converters:LockedOpacityConverter x:Key="LockedOpacity" />
```

- [ ] **Step 4: Code-behind, register, run**

```csharp
using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class PhonicsLevelsPage : ContentPage
{
    private readonly PhonicsLevelsViewModel _viewModel;

    public PhonicsLevelsPage(PhonicsLevelsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
```

Add `builder.Services.AddTransient<PhonicsLevelsViewModel>();`.

Run: `dotnet build TongaKids/TongaKids.csproj -f net10.0-android -t:Run`

Expected on a **fresh install**, where nothing is mastered yet:

- Level 1 — unlocked, PLAY button, progress 0%. Its `requiresLevelNumber` is 0.
- Levels 2, 3 and 4 — dimmed with a padlock and "Complete Level N to unlock",
  because each requires the level before it and none is mastered.

Tapping Level 1's PLAY opens the (still placeholder) lesson page; tapping a locked card
does nothing at all — no message, no error. Level 2 becoming available is verified in
Task 16 Step 3, once mastery can actually be earned. Compare with
`learn_phonics_levels/screen.png`, which shows the same three card states.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "Add phonics levels screen

Joins level content to this learner's progress to produce the three card
states from the mockup: complete, current, and locked behind a prerequisite.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 13: AudioService

**Files:**
- Create: `TongaKids/Services/IAudioService.cs`, `TongaKids/Services/AudioService.cs`
- Modify: `TongaKids/MauiProgram.cs`

**Interfaces:**
- Produces: `IAudioService.PlayAsync(string audioKey) → Task<bool>` (false when the clip is
  absent), `IAudioService.StopAsync() → Task`.

Per the spec: **a missing clip is silence, never an error.** The app must be fully usable
before a single word of Chitonga has been recorded.

- [ ] **Step 1: Write the service**

```csharp
namespace TongaKids.Services;

public interface IAudioService
{
    /// <summary>Plays a clip by key. Returns false if the clip does not exist.</summary>
    Task<bool> PlayAsync(string audioKey);

    Task StopAsync();
}
```

```csharp
using Plugin.Maui.Audio;

namespace TongaKids.Services;

public sealed class AudioService(IAudioManager audioManager) : IAudioService
{
    private IAudioPlayer? _player;

    public async Task<bool> PlayAsync(string audioKey)
    {
        if (string.IsNullOrWhiteSpace(audioKey))
        {
            return false;
        }

        await StopAsync();

        try
        {
            // Raw assets are addressed by their path under Resources/Raw.
            await using var stream = await FileSystem.OpenAppPackageFileAsync($"audio/{audioKey}.m4a");

            // The stream must outlive this call, so copy it into memory first.
            var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            buffer.Position = 0;

            _player = audioManager.CreatePlayer(buffer);
            _player.Play();
            return true;
        }
        catch (FileNotFoundException)
        {
            // Not yet recorded. Silence is the designed behaviour.
            System.Diagnostics.Debug.WriteLine($"[Audio] no clip for '{audioKey}'");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Audio] '{audioKey}' failed: {ex}");
            return false;
        }
    }

    public Task StopAsync()
    {
        try
        {
            if (_player is not null)
            {
                if (_player.IsPlaying)
                {
                    _player.Stop();
                }

                _player.Dispose();
                _player = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Audio] stop failed: {ex}");
        }

        return Task.CompletedTask;
    }
}
```

- [ ] **Step 2: Register**

In `MauiProgram.cs`, add `.UseMauiApp<App>()`-chained registration for the plugin and the
service:

```csharp
builder.Services.AddSingleton(AudioManager.Current);
builder.Services.AddSingleton<IAudioService, AudioService>();
```

Add `using Plugin.Maui.Audio;`.

- [ ] **Step 3: Verify the silent path**

Run: `dotnet build TongaKids/TongaKids.csproj -f net10.0-android`
Expected: `Build succeeded`.

With no audio recorded yet, playback is exercised in Task 14. The behaviour to confirm
there is: tapping the speaker logs `[Audio] no clip for 'syl_ba'` and the UI still
animates. **No dialog, no crash.**

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "Add AudioService with silent fallback for unrecorded clips

A missing clip logs and returns false rather than throwing, so the app is
fully usable before any Chitonga has been recorded.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 14: Phonics lesson

**Files:**
- Modify: `TongaKids/Views/PhonicsLessonPage.xaml(.cs)`
- Create: `TongaKids/ViewModels/PhonicsLessonViewModel.cs`
- Modify: `TongaKids/MauiProgram.cs`

**Interfaces:**
- Consumes: `IContentRepository`, `IAudioService`, query parameter `lessonId`.
- Produces: navigation to `game?lessonId=N`.

Reference: `phonics_lesson_ba/screen.png`. The grapheme card uses the `PhonicsFocus`
style (64px/800) and is at least 72×72dp per DESIGN.md.

- [ ] **Step 1: Write the view model**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Data;
using TongaKids.Models;
using TongaKids.Services;

namespace TongaKids.ViewModels;

[QueryProperty(nameof(LessonId), "lessonId")]
public sealed partial class PhonicsLessonViewModel(
    IContentRepository content,
    IAudioService audio) : ObservableObject
{
    [ObservableProperty] private int _lessonId;
    [ObservableProperty] private string _lessonTitle = string.Empty;
    [ObservableProperty] private string _lessonSubtitle = string.Empty;
    [ObservableProperty] private string _grapheme = string.Empty;
    [ObservableProperty] private double _lessonProgress;
    [ObservableProperty] private bool _hasExamples;

    public ObservableCollection<PhonicsItem> Examples { get; } = [];

    private List<PhonicsItem> _items = [];
    private int _index;

    partial void OnLessonIdChanged(int value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (LessonId <= 0)
        {
            return;
        }

        try
        {
            var lesson = await content.GetLessonAsync(LessonId);
            _items = await content.GetItemsAsync(LessonId);
            _index = 0;

            LessonTitle = lesson?.Title ?? "Lesson";
            ShowCurrent();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Lesson] load failed: {ex}");
        }
    }

    private void ShowCurrent()
    {
        Examples.Clear();

        if (_items.Count == 0)
        {
            Grapheme = string.Empty;
            LessonSubtitle = string.Empty;
            HasExamples = false;
            return;
        }

        var item = _items[_index];
        Grapheme = item.Grapheme;
        LessonSubtitle = $"Sound {_index + 1} of {_items.Count}";
        LessonProgress = (double)(_index + 1) / _items.Count;

        // Only show the example row once the owner has authored a word for it.
        if (!string.IsNullOrWhiteSpace(item.ExampleWord))
        {
            Examples.Add(item);
        }

        HasExamples = Examples.Count > 0;
    }

    [RelayCommand]
    private async Task PlaySoundAsync()
    {
        if (_items.Count > 0)
        {
            await audio.PlayAsync(_items[_index].AudioKey);
        }
    }

    [RelayCommand]
    private void Next()
    {
        if (_index < _items.Count - 1)
        {
            _index++;
            ShowCurrent();
        }
    }

    [RelayCommand]
    private void Previous()
    {
        if (_index > 0)
        {
            _index--;
            ShowCurrent();
        }
    }

    [RelayCommand]
    private async Task PlayGameAsync() => await Shell.Current.GoToAsync($"game?lessonId={LessonId}");
}
```

- [ ] **Step 2: Write the page**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:controls="clr-namespace:TongaKids.Controls"
             xmlns:models="clr-namespace:TongaKids.Models"
             xmlns:vm="clr-namespace:TongaKids.ViewModels"
             x:Class="TongaKids.Views.PhonicsLessonPage"
             x:DataType="vm:PhonicsLessonViewModel"
             Title="{Binding LessonTitle}">

    <Grid RowDefinitions="Auto,*,Auto" Padding="{StaticResource ScreenPadding}" RowSpacing="16">

        <ProgressBar Grid.Row="0" Progress="{Binding LessonProgress}"
                     ProgressColor="{StaticResource Tertiary}"
                     Style="{StaticResource PillProgress}" />

        <ScrollView Grid.Row="1">
            <VerticalStackLayout Spacing="24">

                <!-- Grapheme card. 240dp tall, far above the 72dp minimum. -->
                <Border Style="{StaticResource Card}" HeightRequest="240" Padding="24">
                    <Label Text="{Binding Grapheme}" Style="{StaticResource PhonicsFocus}"
                           HorizontalOptions="Center" VerticalOptions="Center" />
                </Border>

                <!-- Tap to hear -->
                <VerticalStackLayout Spacing="8" HorizontalOptions="Center">
                    <Border WidthRequest="96" HeightRequest="96"
                            BackgroundColor="{StaticResource TertiaryContainer}"
                            StrokeThickness="0">
                        <Border.StrokeShape>
                            <RoundRectangle CornerRadius="48" />
                        </Border.StrokeShape>
                        <Border.GestureRecognizers>
                            <TapGestureRecognizer Command="{Binding PlaySoundCommand}" />
                        </Border.GestureRecognizers>
                        <Label Text="volume_up" Style="{StaticResource Icon}" FontSize="44"
                               TextColor="{StaticResource OnTertiaryContainer}"
                               HorizontalOptions="Center" VerticalOptions="Center" />
                    </Border>
                    <Label Text="TAP TO HEAR" Style="{StaticResource LabelLg}"
                           CharacterSpacing="2" HorizontalTextAlignment="Center" />
                </VerticalStackLayout>

                <!-- Example words, shown only when authored -->
                <VerticalStackLayout IsVisible="{Binding HasExamples}" Spacing="12"
                                     BindableLayout.ItemsSource="{Binding Examples}">
                    <BindableLayout.ItemTemplate>
                        <DataTemplate x:DataType="models:PhonicsItem">
                            <Border Style="{StaticResource Card}" Padding="12">
                                <Grid ColumnDefinitions="Auto,*" ColumnSpacing="16">
                                    <Border Grid.Column="0" WidthRequest="64" HeightRequest="64"
                                            BackgroundColor="{StaticResource PrimaryFixed}"
                                            StrokeThickness="0">
                                        <Border.StrokeShape>
                                            <RoundRectangle CornerRadius="12" />
                                        </Border.StrokeShape>
                                        <Image Source="{Binding ImageKey, StringFormat='{0}.png'}"
                                               Aspect="AspectFill" />
                                    </Border>
                                    <VerticalStackLayout Grid.Column="1" VerticalOptions="Center">
                                        <Label Text="{Binding ExampleWord}"
                                               Style="{StaticResource TitleLg}"
                                               TextColor="{StaticResource Secondary}" />
                                        <Label Text="{Binding Gloss}" Style="{StaticResource BodyMd}" />
                                    </VerticalStackLayout>
                                </Grid>
                            </Border>
                        </DataTemplate>
                    </BindableLayout.ItemTemplate>
                </VerticalStackLayout>

            </VerticalStackLayout>
        </ScrollView>

        <VerticalStackLayout Grid.Row="2" Spacing="12">
            <controls:TactileButton Text="Play Game"
                                    BackgroundFill="{StaticResource PrimaryContainer}"
                                    DepthColor="{StaticResource OnPrimaryContainer}"
                                    TextColor="{StaticResource OnPrimary}"
                                    Command="{Binding PlayGameCommand}" />
            <Grid ColumnDefinitions="*,*" ColumnSpacing="12">
                <controls:TactileButton Grid.Column="0" Text="Previous"
                                        BackgroundFill="{StaticResource SurfaceContainerLowest}"
                                        DepthColor="{StaticResource OutlineVariant}"
                                        TextColor="{StaticResource Secondary}"
                                        Command="{Binding PreviousCommand}" />
                <controls:TactileButton Grid.Column="1" Text="Next"
                                        BackgroundFill="{StaticResource Secondary}"
                                        DepthColor="{StaticResource OnSecondaryFixedVariant}"
                                        TextColor="{StaticResource OnSecondary}"
                                        Command="{Binding NextCommand}" />
            </Grid>
        </VerticalStackLayout>
    </Grid>
</ContentPage>
```

```csharp
using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class PhonicsLessonPage : ContentPage
{
    public PhonicsLessonPage(PhonicsLessonViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
```

- [ ] **Step 3: Register and run**

Add `builder.Services.AddTransient<PhonicsLessonViewModel>();`.

Run: `dotnet build TongaKids/TongaKids.csproj -f net10.0-android -t:Run`

Expected: Level 1 opens showing a large "A" on a white card. Next steps through E, I, O, U
and the green progress bar advances. Tapping the speaker logs `[Audio] no clip for 'vowel_a'`
with **no dialog and no crash**. Level 2 Lesson 1 shows "BA" with the Balu example row and
the ball image; "DA" shows no example row because none is authored. "Play Game" opens the
placeholder game page. Compare with `phonics_lesson_ba/screen.png`.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "Add phonics lesson screen

Steps through a lesson's graphemes with tap-to-hear playback. The example
row renders only when the owner has authored a word, so the screen is
correct with content half-written.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 15: Matching game

**Files:**
- Modify: `TongaKids/Views/MatchingGamePage.xaml(.cs)`
- Create: `TongaKids/ViewModels/MatchingGameViewModel.cs`, `TongaKids/ViewModels/ChoiceCardModel.cs`
- Modify: `TongaKids/MauiProgram.cs`

**Interfaces:**
- Consumes: `IContentRepository`, `IPhonicsEngine`, `IAudioService`, query parameter `lessonId`.
- Produces: navigation to `complete?lessonId=N&correct=C&total=T&durationMs=D`.

Reference: `matching_game_practice/screen.png`. Four cards, one selected at a time, then
Submit. This is the Quiz state of the report's state machine.

- [ ] **Step 1: Write the card model and view model**

`TongaKids/ViewModels/ChoiceCardModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using TongaKids.Models;

namespace TongaKids.ViewModels;

public sealed partial class ChoiceCardModel(PhonicsItem item) : ObservableObject
{
    public PhonicsItem Item { get; } = item;

    public string Grapheme => Item.Grapheme;

    [ObservableProperty] private bool _isSelected;
}
```

`TongaKids/ViewModels/MatchingGameViewModel.cs`:

```csharp
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Data;
using TongaKids.Services;

namespace TongaKids.ViewModels;

[QueryProperty(nameof(LessonId), "lessonId")]
public sealed partial class MatchingGameViewModel(
    IContentRepository content,
    IPhonicsEngine engine,
    IAudioService audio) : ObservableObject
{
    private const int OptionCount = 4;

    [ObservableProperty] private int _lessonId;
    [ObservableProperty] private string _prompt = string.Empty;
    [ObservableProperty] private bool _canSubmit;

    public ObservableCollection<ChoiceCardModel> Choices { get; } = [];

    private IReadOnlyList<PhonicsQuestion> _questions = [];
    private int _questionIndex;
    private int _correctCount;
    private readonly Stopwatch _timer = new();

    partial void OnLessonIdChanged(int value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (LessonId <= 0)
        {
            return;
        }

        try
        {
            var items = await content.GetItemsAsync(LessonId);
            var pool = await content.GetAllItemsAsync();

            // A fresh seed each session so the order is not memorised.
            _questions = engine.BuildQuiz(items, pool, OptionCount, Environment.TickCount);
            _questionIndex = 0;
            _correctCount = 0;
            _timer.Restart();

            ShowQuestion();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Game] load failed: {ex}");
        }
    }

    private void ShowQuestion()
    {
        Choices.Clear();
        CanSubmit = false;

        if (_questionIndex >= _questions.Count)
        {
            return;
        }

        var question = _questions[_questionIndex];
        Prompt = $"Which one says '{question.Target.Grapheme}'?";

        foreach (var option in question.Options)
        {
            Choices.Add(new ChoiceCardModel(option));
        }
    }

    [RelayCommand]
    private void Select(ChoiceCardModel? card)
    {
        if (card is null)
        {
            return;
        }

        foreach (var choice in Choices)
        {
            choice.IsSelected = ReferenceEquals(choice, card);
        }

        CanSubmit = true;
    }

    [RelayCommand]
    private async Task PlayPromptAsync()
    {
        if (_questionIndex < _questions.Count)
        {
            await audio.PlayAsync(_questions[_questionIndex].Target.AudioKey);
        }
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (!CanSubmit || _questionIndex >= _questions.Count)
        {
            return;
        }

        var selected = Choices.FirstOrDefault(c => c.IsSelected);
        if (selected is null)
        {
            return;
        }

        if (selected.Item.Id == _questions[_questionIndex].Target.Id)
        {
            _correctCount++;
        }

        _questionIndex++;

        if (_questionIndex < _questions.Count)
        {
            ShowQuestion();
            return;
        }

        _timer.Stop();
        await Shell.Current.GoToAsync(
            $"complete?lessonId={LessonId}&correct={_correctCount}" +
            $"&total={_questions.Count}&durationMs={_timer.ElapsedMilliseconds}");
    }
}
```

Add `using TongaKids.Models;` for `PhonicsItem` if the compiler asks.

- [ ] **Step 2: Write the page**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:controls="clr-namespace:TongaKids.Controls"
             xmlns:vm="clr-namespace:TongaKids.ViewModels"
             x:Class="TongaKids.Views.MatchingGamePage"
             x:DataType="vm:MatchingGameViewModel"
             Title="TongaKids Read">

    <Grid RowDefinitions="Auto,*,Auto" Padding="{StaticResource ScreenPadding}" RowSpacing="16">

        <VerticalStackLayout Grid.Row="0" Spacing="8">
            <Border Style="{StaticResource Card}">
                <Grid ColumnDefinitions="*,Auto" ColumnSpacing="12">
                    <Label Grid.Column="0" Text="{Binding Prompt}"
                           Style="{StaticResource TitleLg}" VerticalOptions="Center" />
                    <Border Grid.Column="1" WidthRequest="56" HeightRequest="56"
                            BackgroundColor="{StaticResource Secondary}" StrokeThickness="0">
                        <Border.StrokeShape>
                            <RoundRectangle CornerRadius="28" />
                        </Border.StrokeShape>
                        <Border.GestureRecognizers>
                            <TapGestureRecognizer Command="{Binding PlayPromptCommand}" />
                        </Border.GestureRecognizers>
                        <Label Text="volume_up" Style="{StaticResource Icon}" FontSize="28"
                               TextColor="{StaticResource OnSecondary}"
                               HorizontalOptions="Center" VerticalOptions="Center" />
                    </Border>
                </Grid>
            </Border>
            <Label Text="Tap the card that matches the sound."
                   Style="{StaticResource BodyMd}" HorizontalTextAlignment="Center" />
        </VerticalStackLayout>

        <CollectionView Grid.Row="1" ItemsSource="{Binding Choices}" SelectionMode="None">
            <CollectionView.ItemsLayout>
                <GridItemsLayout Orientation="Vertical" Span="2"
                                 HorizontalItemSpacing="16" VerticalItemSpacing="16" />
            </CollectionView.ItemsLayout>
            <CollectionView.ItemTemplate>
                <DataTemplate x:DataType="vm:ChoiceCardModel">
                    <!-- 150dp square: DESIGN.md requires 72dp minimum for phonics cards. -->
                    <Border HeightRequest="150"
                            BackgroundColor="{StaticResource SurfaceContainerLowest}"
                            Stroke="{Binding IsSelected, Converter={StaticResource SelectionStroke}}"
                            StrokeThickness="{Binding IsSelected, Converter={StaticResource SelectionThickness}}">
                        <Border.StrokeShape>
                            <RoundRectangle CornerRadius="16" />
                        </Border.StrokeShape>
                        <Border.GestureRecognizers>
                            <TapGestureRecognizer
                                Command="{Binding Source={RelativeSource AncestorType={x:Type vm:MatchingGameViewModel}}, Path=SelectCommand}"
                                CommandParameter="{Binding .}" />
                        </Border.GestureRecognizers>
                        <Label Text="{Binding Grapheme}" Style="{StaticResource PhonicsFocus}"
                               FontSize="52" TextColor="{StaticResource Primary}"
                               HorizontalOptions="Center" VerticalOptions="Center" />
                    </Border>
                </DataTemplate>
            </CollectionView.ItemTemplate>
        </CollectionView>

        <controls:TactileButton Grid.Row="2" Text="Submit"
                                BackgroundFill="{StaticResource Primary}"
                                DepthColor="{StaticResource OnPrimaryFixed}"
                                TextColor="{StaticResource OnPrimary}"
                                CornerRadius="28"
                                IsEnabled="{Binding CanSubmit}"
                                Command="{Binding SubmitCommand}" />
    </Grid>
</ContentPage>
```

- [ ] **Step 3: Add the selection converters**

Create `TongaKids/Converters/SelectionConverters.cs`:

```csharp
using System.Globalization;

namespace TongaKids.Converters;

/// <summary>Selected cards take a heavy Leaf Green border, per the mockup.</summary>
public sealed class SelectionStrokeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Color.FromArgb("#006e1c") : Color.FromArgb("#e0d8c3");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class SelectionThicknessConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? 4.0 : 1.0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
```

Register both in `App.xaml` alongside `LockedOpacity`:

```xml
<converters:SelectionStrokeConverter x:Key="SelectionStroke" />
<converters:SelectionThicknessConverter x:Key="SelectionThickness" />
```

- [ ] **Step 4: Code-behind, register, run**

```csharp
using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class MatchingGamePage : ContentPage
{
    public MatchingGamePage(MatchingGameViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
```

Add `builder.Services.AddTransient<MatchingGameViewModel>();`.

Run: `dotnet build TongaKids/TongaKids.csproj -f net10.0-android -t:Run`

Expected: from Level 2 Lesson 1, "Which one says 'BA'?" with four cards in a 2×2 grid.
The distractors are minimal pairs (DA, MA, PA) — **never a long word like TAMBO**, which
is the visible proof the engine works. Tapping a card gives it a thick green border and
enables Submit. Submitting advances to the next question, then to the completion page.
Compare with `matching_game_practice/screen.png`.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "Add matching game

Four-card sound discrimination driven by the PhonicsEngine. Distractors are
minimal pairs of the target, so the child must hear the difference rather
than recognise the shape.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 16: Lesson complete and progress persistence

**Files:**
- Modify: `TongaKids/Views/LessonCompletePage.xaml(.cs)`
- Create: `TongaKids/ViewModels/LessonCompleteViewModel.cs`
- Modify: `TongaKids/MauiProgram.cs`

**Interfaces:**
- Consumes: `IProgressCalculator`, `IMasteryEvaluator`, `IProgressRepository`,
  `ILearnerSession`; query parameters `lessonId`, `correct`, `total`, `durationMs`.
- Produces: persisted `QuizAttempt` and `LessonProgress` rows — the data every later
  progress screen reads.

This closes the report's state machine: Quiz → Evaluation → Mastered, or back to Active
Learning for remediation.

- [ ] **Step 1: Write the view model**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Data;
using TongaKids.Models;
using TongaKids.Services;

namespace TongaKids.ViewModels;

[QueryProperty(nameof(LessonId), "lessonId")]
[QueryProperty(nameof(Correct), "correct")]
[QueryProperty(nameof(Total), "total")]
[QueryProperty(nameof(DurationMs), "durationMs")]
public sealed partial class LessonCompleteViewModel(
    IProgressCalculator calculator,
    IMasteryEvaluator evaluator,
    IProgressRepository progress,
    ILearnerSession session) : ObservableObject
{
    [ObservableProperty] private int _lessonId;
    [ObservableProperty] private int _correct;
    [ObservableProperty] private int _total;
    [ObservableProperty] private long _durationMs;

    [ObservableProperty] private int _stars;
    [ObservableProperty] private int _scorePercent;
    [ObservableProperty] private string _headline = string.Empty;
    [ObservableProperty] private string _message = string.Empty;
    [ObservableProperty] private bool _isMastered;

    private bool _saved;

    partial void OnTotalChanged(int value) => _ = EvaluateAndSaveAsync();

    private async Task EvaluateAndSaveAsync()
    {
        if (_saved || Total <= 0)
        {
            return;
        }

        _saved = true;

        ScorePercent = calculator.AccuracyPercent(Correct, Total);
        Stars = calculator.StarsFor(ScorePercent);
        IsMastered = evaluator.Evaluate(ScorePercent) == MasteryOutcome.Mastered;

        // Encouraging in both directions. A child who scored 40% is not told they failed.
        Headline = IsMastered ? "Well done!" : "Good try!";
        Message = IsMastered
            ? $"You got {Correct} of {Total} right."
            : "Let's practise these sounds once more.";

        var learner = session.Current;
        if (learner is null)
        {
            return;
        }

        try
        {
            await progress.AddAttemptAsync(new QuizAttempt
            {
                LearnerId = learner.Id,
                LessonId = LessonId,
                CorrectCount = Correct,
                TotalCount = Total,
                DurationMs = DurationMs,
                AttemptedAt = DateTime.UtcNow
            });

            var existing = await progress.GetForLessonAsync(learner.Id, LessonId)
                           ?? new LessonProgress { LearnerId = learner.Id, LessonId = LessonId };

            // Keep the learner's best result; a worse retry never takes anything away.
            if (ScorePercent >= existing.BestScorePercent)
            {
                existing.BestScorePercent = ScorePercent;
                existing.StarsEarned = Stars;
            }

            existing.IsMastered = existing.IsMastered || IsMastered;
            existing.CompletedAt = DateTime.UtcNow;

            await progress.SaveAsync(existing);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Complete] save failed: {ex}");
        }
    }

    [RelayCommand]
    private async Task ContinueAsync()
    {
        // Mastered moves on; otherwise back into the lesson for remediation.
        await Shell.Current.GoToAsync(IsMastered ? "//main/home" : $"lesson?lessonId={LessonId}");
    }
}
```

- [ ] **Step 2: Write the page**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:controls="clr-namespace:TongaKids.Controls"
             xmlns:vm="clr-namespace:TongaKids.ViewModels"
             x:Class="TongaKids.Views.LessonCompletePage"
             x:DataType="vm:LessonCompleteViewModel"
             Shell.NavBarIsVisible="False">

    <VerticalStackLayout Padding="{StaticResource ScreenPadding}" Spacing="24"
                         VerticalOptions="Center">

        <Label Text="{Binding Headline}" Style="{StaticResource DisplayLg}"
               TextColor="{StaticResource Primary}" HorizontalTextAlignment="Center" />

        <controls:StarRating Earned="{Binding Stars}" Total="5" StarSize="48"
                             HorizontalOptions="Center" />

        <Label Text="{Binding Message}" Style="{StaticResource BodyLg}"
               HorizontalTextAlignment="Center" />

        <Border Style="{StaticResource Card}" HorizontalOptions="Center" Padding="24,12">
            <Label Text="{Binding ScorePercent, StringFormat='{0}%'}"
                   Style="{StaticResource HeadlineLg}"
                   TextColor="{StaticResource Tertiary}" />
        </Border>

        <controls:TactileButton Text="Continue"
                                BackgroundFill="{StaticResource PrimaryContainer}"
                                DepthColor="{StaticResource OnPrimaryContainer}"
                                TextColor="{StaticResource OnPrimary}"
                                Command="{Binding ContinueCommand}" />
    </VerticalStackLayout>
</ContentPage>
```

```csharp
using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class LessonCompletePage : ContentPage
{
    public LessonCompletePage(LessonCompleteViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
```

- [ ] **Step 3: Register and verify the whole path end to end**

Add `builder.Services.AddTransient<LessonCompleteViewModel>();`.

Run: `dotnet build TongaKids/TongaKids.csproj -f net10.0-android -t:Run`

Walk the full child path and confirm each of these:

1. Splash → profile → Home → Learn Phonics → Level 1 → lesson → Play Game.
2. Answer **every question correctly**. Result shows "Well done!", 5 stars, 100%.
3. Continue returns to Home, and the progress bar has moved.
4. Return to Phonics Levels: **Level 1 now shows as complete and Level 2 is unlocked.**
   This is the mastery rule visibly working.
5. Play Level 2 and answer **most questions wrong**. Result shows "Good try!", a low
   star count, and Continue returns to the *lesson*, not Home — the remediation branch.
6. Force-close and relaunch: Level 1 is still complete. Progress survived.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "Add lesson completion with progress persistence

Closes the report's state machine: score at or above 80% marks the lesson
mastered and unlocks the next level; below it returns to the lesson for
remediation with no penalty and no lost progress.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Definition of done for Plan 1

- [ ] A child can complete the full path: splash → profile → home → level → lesson → game → result.
- [ ] Mastering a level unlocks the next; scoring below 80% returns to the lesson.
- [ ] Progress survives an app restart.
- [ ] The self-check page reports **28 of 28 checks passed**.
- [ ] No dialog, stack trace, or error code is reachable from any child-facing screen.
- [ ] The app is fully usable with **zero audio clips recorded**.
- [ ] `docs/audio-recording-list.md` lists every clip the owner needs to record.
- [ ] No hex literal appears in any page (the two documented control exceptions aside).
- [ ] `grep -rn "80" TongaKids/ViewModels/` returns no inlined mastery threshold.

## Handover to Plan 2

Plan 2 (Story Library and Reader) builds on: `ITongaKidsDatabase`, `IContentSeeder` (extend
with `stories` in the JSON pack), `IAudioService`, `ILearnerSession`, `IProgressCalculator`
(`WordsPerMinute` is written and verified here but not yet called by any screen — the Story
Reader is its first consumer), the `StoriesPage` tab stub, and the `Card` / `TactileButton`
/ `StarRating` design-system pieces.
