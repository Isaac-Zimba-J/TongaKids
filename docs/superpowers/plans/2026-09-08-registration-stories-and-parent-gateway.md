# TongaKids Read — Plan 2: Registration, Stories, Progress and Parental Gateway

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make every remaining use case in the project report demonstrable — guardian registration, the folktale library and reader, learner progress, and the parental gateway with consent and monitoring.

**Architecture:** Continues Plan 1 unchanged. Same single MAUI project, same `Views → ViewModels → Services → Data` direction, same design tokens. This plan adds one new service (`IAuthService`), extends the content pack with stories, and adds nine screens.

**Tech Stack:** As Plan 1 (`CommunityToolkit.Mvvm`, `sqlite-net-pcl`, `Plugin.Maui.Audio`). No new NuGet packages.

**Spec:** `docs/superpowers/specs/2026-09-07-tongakids-read-design.md`

**Depends on:** Plan 1 (`docs/superpowers/plans/2026-09-08-foundation-and-phonics-core.md`) complete through Task 16.

**Covers:** UC-02, UC-04, UC-05, UC-06, UC-07, plus the registration requirement of report §3.4.2.

## Scope discipline — read before starting

The owner has a fixed deadline and chose **breadth over depth** (spec §3.2 decision log).
Every use case must be reachable and working; none needs to be deep.

Concretely, this means:

- The story library ships with **two** short stories, not fifty. The reader is real; the
  catalogue is small.
- The parent dashboard shows **real numbers** computed from real rows — accuracy, words
  per minute, weakest lesson. It does not need charts.
- **Do not** add features this plan does not list. If a task looks thin, that is deliberate.

A demo where all seven use cases work simply beats one where two are polished and five
are missing.

## Global Constraints

All Plan 1 global constraints carry over unchanged and are not repeated here. In addition:

- Guardian credentials are stored **hashed** (PBKDF2, per-account salt). Never plaintext,
  even in a demo — a marker who opens the database must find a hash.
- The child's path still contains **no login**. Registration is a one-time guardian step
  before profile selection; a learner never sees a credential field.
- The Parental Gateway must be genuinely closed: no route into parent screens without
  passing the PIN challenge in the same session.

---

### Task 17: Guardian account and AuthService

**Files:**
- Create: `TongaKids/Models/GuardianAccount.cs`
- Create: `TongaKids/Services/IAuthService.cs`, `TongaKids/Services/AuthService.cs`
- Modify: `TongaKids/Data/TongaKidsDatabase.cs`, `TongaKids/MauiProgram.cs`

**Interfaces:**
- Consumes: `ITongaKidsDatabase` (Plan 1 Task 4).
- Produces:
  - `IAuthService.IsRegisteredAsync() → Task<bool>`
  - `IAuthService.RegisterAsync(string name, string contact, string password, string securityAnswer) → Task<bool>`
  - `IAuthService.SignInAsync(string contact, string password) → Task<bool>`
  - `IAuthService.ResetPasswordAsync(string contact, string securityAnswer, string newPassword) → Task<bool>`
  - `IAuthService.CurrentAccount { get; }`
  - `AuthService.SecurityQuestion` (const string)

- [ ] **Step 1: Write the model**

`TongaKids/Models/GuardianAccount.cs`:

```csharp
using SQLite;

namespace TongaKids.Models;

/// <summary>
/// The parent or guardian who set the app up. One per device.
/// Report §3.4.2 requires registration; the child's path has none (spec §3.2).
/// </summary>
[Table("GuardianAccount")]
public class GuardianAccount
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Email address or phone number, whichever the guardian gave.</summary>
    [Indexed] public string Contact { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public string SecurityAnswerHash { get; set; } = string.Empty;
    public string SecurityAnswerSalt { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

Register the table in `TongaKidsDatabase.GetConnectionAsync`, alongside the existing
`CreateTableAsync` calls:

```csharp
await connection.CreateTableAsync<GuardianAccount>();
```

- [ ] **Step 2: Write the service**

`TongaKids/Services/IAuthService.cs`:

```csharp
using TongaKids.Models;

namespace TongaKids.Services;

public interface IAuthService
{
    GuardianAccount? CurrentAccount { get; }

    Task<bool> IsRegisteredAsync();

    Task<bool> RegisterAsync(string name, string contact, string password, string securityAnswer);

    Task<bool> SignInAsync(string contact, string password);

    Task<bool> ResetPasswordAsync(string contact, string securityAnswer, string newPassword);
}
```

`TongaKids/Services/AuthService.cs`:

```csharp
using System.Security.Cryptography;
using System.Text;
using TongaKids.Data;
using TongaKids.Models;

namespace TongaKids.Services;

/// <summary>
/// Local-only guardian authentication. There is no auth server because there is
/// no backend in scope; the flow is real, its remote counterpart is not built.
/// </summary>
public sealed class AuthService(ITongaKidsDatabase database) : IAuthService
{
    /// <summary>
    /// Email recovery needs a server to send mail, so recovery is a locally
    /// verifiable challenge instead.
    /// </summary>
    public const string SecurityQuestion = "What is the name of your home village?";

    private const int Iterations = 100_000;
    private const int SaltBytes = 16;
    private const int HashBytes = 32;

    public GuardianAccount? CurrentAccount { get; private set; }

    public async Task<bool> IsRegisteredAsync() => await GetAccountAsync() is not null;

    public async Task<bool> RegisterAsync(
        string name, string contact, string password, string securityAnswer)
    {
        if (string.IsNullOrWhiteSpace(contact) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        var db = await database.GetConnectionAsync();

        var (passwordHash, passwordSalt) = Hash(password);
        var (answerHash, answerSalt) = Hash(Normalise(securityAnswer));

        var account = new GuardianAccount
        {
            Name = name.Trim(),
            Contact = contact.Trim(),
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            SecurityAnswerHash = answerHash,
            SecurityAnswerSalt = answerSalt,
            CreatedAt = DateTime.UtcNow
        };

        await db.InsertAsync(account);
        CurrentAccount = account;
        return true;
    }

    public async Task<bool> SignInAsync(string contact, string password)
    {
        var account = await GetAccountAsync();
        if (account is null || !Verify(password, account.PasswordHash, account.PasswordSalt))
        {
            return false;
        }

        CurrentAccount = account;
        return true;
    }

    public async Task<bool> ResetPasswordAsync(
        string contact, string securityAnswer, string newPassword)
    {
        var account = await GetAccountAsync();
        if (account is null || string.IsNullOrWhiteSpace(newPassword))
        {
            return false;
        }

        if (!Verify(Normalise(securityAnswer), account.SecurityAnswerHash, account.SecurityAnswerSalt))
        {
            return false;
        }

        var (hash, salt) = Hash(newPassword);
        account.PasswordHash = hash;
        account.PasswordSalt = salt;

        var db = await database.GetConnectionAsync();
        await db.UpdateAsync(account);
        return true;
    }

    private async Task<GuardianAccount?> GetAccountAsync()
    {
        try
        {
            var db = await database.GetConnectionAsync();
            return await db.Table<GuardianAccount>().FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Auth] lookup failed: {ex}");
            return null;
        }
    }

    /// <summary>Case and whitespace insensitive, so recovery is not a memory test.</summary>
    private static string Normalise(string value) => (value ?? string.Empty).Trim().ToLowerInvariant();

    private static (string Hash, string Salt) Hash(string value)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(value), salt, Iterations, HashAlgorithmName.SHA256, HashBytes);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    private static bool Verify(string value, string expectedHash, string saltBase64)
    {
        try
        {
            var salt = Convert.FromBase64String(saltBase64);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(value), salt, Iterations, HashAlgorithmName.SHA256, HashBytes);

            // Constant-time comparison: never leak how much of the hash matched.
            return CryptographicOperations.FixedTimeEquals(
                hash, Convert.FromBase64String(expectedHash));
        }
        catch
        {
            return false;
        }
    }
}
```

- [ ] **Step 3: Add self-check cases and run them**

Create `TongaKids/Services/SelfCheck/AuthSelfCheck.cs`. These run against a real database,
so they are async; the harness interface is sync, so expose a sync wrapper that blocks —
acceptable in a DEBUG-only diagnostic page.

```csharp
namespace TongaKids.Services.SelfCheck;

public sealed class AuthSelfCheck(IAuthService auth) : ISelfCheck
{
    public string Area => "Guardian authentication";

    public IReadOnlyList<SelfCheckResult> Run()
    {
        var results = new List<SelfCheckResult>();

        void Check(string name, object expected, object actual) =>
            results.Add(new SelfCheckResult(
                name, Equals(expected, actual), $"expected {expected}, got {actual}"));

        // These assume registration already happened on this device.
        var registered = auth.IsRegisteredAsync().GetAwaiter().GetResult();
        Check("an account exists after registration", true, registered);

        if (registered)
        {
            Check("a wrong password is rejected", false,
                auth.SignInAsync("any", "definitely-not-the-password").GetAwaiter().GetResult());

            Check("a wrong security answer is rejected", false,
                auth.ResetPasswordAsync("any", "not-the-answer", "newpass123")
                    .GetAwaiter().GetResult());
        }

        return results;
    }
}
```

Register in `MauiProgram.cs`:

```csharp
builder.Services.AddSingleton<IAuthService, AuthService>();
#if DEBUG
builder.Services.AddSingleton<ISelfCheck, AuthSelfCheck>();
#endif
```

- [ ] **Step 4: Build**

Run: `dotnet build TongaKids/TongaKids.csproj -f net10.0-android`
Expected: `Build succeeded`. The auth checks are exercised on the device in Task 18.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "Add guardian account and local authentication service

PBKDF2-hashed credentials with per-account salts and constant-time
comparison. Recovery uses a security question because there is no backend
to send mail from.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 18: Registration and sign-in screens

**Files:**
- Create: `TongaKids/Views/RegisterPage.xaml(.cs)`, `TongaKids/Views/SignInPage.xaml(.cs)`
- Create: `TongaKids/ViewModels/RegisterViewModel.cs`, `TongaKids/ViewModels/SignInViewModel.cs`
- Modify: `TongaKids/Views/SplashPage.xaml.cs`, `TongaKids/AppShell.xaml`, `TongaKids/MauiProgram.cs`

**Interfaces:**
- Consumes: `IAuthService`.
- Produces: routes `//register` and `//signin`; on success both navigate to `//profiles`.

Splash routing becomes: onboarding (first launch) → register (no account) → sign-in
(account exists, not signed in this launch) → profiles.

- [ ] **Step 1: Write the register view model**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Services;

namespace TongaKids.ViewModels;

public sealed partial class RegisterViewModel(IAuthService auth) : ObservableObject
{
    public string SecurityQuestion => AuthService.SecurityQuestion;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _contact = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _securityAnswer = string.Empty;
    [ObservableProperty] private string _error = string.Empty;
    [ObservableProperty] private bool _hasError;

    [RelayCommand]
    private async Task SubmitAsync()
    {
        HasError = false;

        // Parent-facing screen, so plain honest validation messages are correct here.
        if (string.IsNullOrWhiteSpace(Name))
        {
            Fail("Please enter your name.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Contact))
        {
            Fail("Please enter an email address or phone number.");
            return;
        }

        if (Password.Length < 6)
        {
            Fail("Your password needs at least 6 characters.");
            return;
        }

        if (string.IsNullOrWhiteSpace(SecurityAnswer))
        {
            Fail("Please answer the security question so you can reset your password later.");
            return;
        }

        if (!await auth.RegisterAsync(Name, Contact, Password, SecurityAnswer))
        {
            Fail("We could not create the account. Please try again.");
            return;
        }

        await Shell.Current.GoToAsync("//profiles");
    }

    private void Fail(string message)
    {
        Error = message;
        HasError = true;
    }
}
```

- [ ] **Step 2: Write the register page**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:controls="clr-namespace:TongaKids.Controls"
             xmlns:vm="clr-namespace:TongaKids.ViewModels"
             x:Class="TongaKids.Views.RegisterPage"
             x:DataType="vm:RegisterViewModel"
             Shell.NavBarIsVisible="False">

    <ScrollView>
        <VerticalStackLayout Padding="{StaticResource ScreenPadding}" Spacing="16">

            <Label Text="Set up TongaKids Read" Style="{StaticResource HeadlineLg}"
                   TextColor="{StaticResource Primary}" HorizontalTextAlignment="Center" />
            <Label Text="A parent or guardian creates the account. Your child will only tap their picture to start learning."
                   Style="{StaticResource BodyMd}" HorizontalTextAlignment="Center" />

            <Entry Placeholder="Your name" Text="{Binding Name}" />
            <Entry Placeholder="Email or phone number" Text="{Binding Contact}"
                   Keyboard="Email" />
            <Entry Placeholder="Password" Text="{Binding Password}" IsPassword="True" />

            <Label Text="{Binding SecurityQuestion}" Style="{StaticResource LabelLg}" />
            <Entry Placeholder="Your answer" Text="{Binding SecurityAnswer}" />

            <Label Text="{Binding Error}" IsVisible="{Binding HasError}"
                   Style="{StaticResource BodyMd}" TextColor="{StaticResource Error}" />

            <controls:TactileButton Text="Create account"
                                    BackgroundFill="{StaticResource PrimaryContainer}"
                                    DepthColor="{StaticResource OnPrimaryContainer}"
                                    TextColor="{StaticResource OnPrimary}"
                                    Command="{Binding SubmitCommand}" />
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

```csharp
using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class RegisterPage : ContentPage
{
    public RegisterPage(RegisterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
```

- [ ] **Step 3: Write the sign-in view model and page**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Services;

namespace TongaKids.ViewModels;

public sealed partial class SignInViewModel(IAuthService auth) : ObservableObject
{
    [ObservableProperty] private string _contact = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _error = string.Empty;
    [ObservableProperty] private bool _hasError;

    [RelayCommand]
    private async Task SignInAsync()
    {
        HasError = false;

        if (!await auth.SignInAsync(Contact, Password))
        {
            Error = "That password did not match. Please try again.";
            HasError = true;
            return;
        }

        await Shell.Current.GoToAsync("//profiles");
    }

    [RelayCommand]
    private async Task ForgotPasswordAsync()
    {
        var page = Application.Current?.Windows[0].Page;
        if (page is null)
        {
            return;
        }

        var answer = await page.DisplayPromptAsync(
            "Reset password", AuthService.SecurityQuestion, "Next", "Cancel");
        if (string.IsNullOrWhiteSpace(answer))
        {
            return;
        }

        var newPassword = await page.DisplayPromptAsync(
            "Reset password", "Choose a new password (at least 6 characters)", "Save", "Cancel");
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            return;
        }

        var ok = await auth.ResetPasswordAsync(Contact, answer, newPassword);
        await page.DisplayAlert(
            ok ? "Password changed" : "That answer did not match",
            ok ? "You can sign in with your new password." : "Please try again.",
            "OK");
    }
}
```

`TongaKids/Views/SignInPage.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:controls="clr-namespace:TongaKids.Controls"
             xmlns:vm="clr-namespace:TongaKids.ViewModels"
             x:Class="TongaKids.Views.SignInPage"
             x:DataType="vm:SignInViewModel"
             Shell.NavBarIsVisible="False">

    <VerticalStackLayout Padding="{StaticResource ScreenPadding}" Spacing="16"
                         VerticalOptions="Center">

        <Label Text="Welcome back" Style="{StaticResource HeadlineLg}"
               TextColor="{StaticResource Primary}" HorizontalTextAlignment="Center" />

        <Entry Placeholder="Email or phone number" Text="{Binding Contact}" Keyboard="Email" />
        <Entry Placeholder="Password" Text="{Binding Password}" IsPassword="True" />

        <Label Text="{Binding Error}" IsVisible="{Binding HasError}"
               Style="{StaticResource BodyMd}" TextColor="{StaticResource Error}" />

        <controls:TactileButton Text="Sign in"
                                BackgroundFill="{StaticResource PrimaryContainer}"
                                DepthColor="{StaticResource OnPrimaryContainer}"
                                TextColor="{StaticResource OnPrimary}"
                                Command="{Binding SignInCommand}" />

        <Label Text="Forgot password?" Style="{StaticResource LabelLg}"
               TextColor="{StaticResource Secondary}" HorizontalTextAlignment="Center">
            <Label.GestureRecognizers>
                <TapGestureRecognizer Command="{Binding ForgotPasswordCommand}" />
            </Label.GestureRecognizers>
        </Label>
    </VerticalStackLayout>
</ContentPage>
```

```csharp
using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class SignInPage : ContentPage
{
    public SignInPage(SignInViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
```

- [ ] **Step 4: Add routes and update splash routing**

In `AppShell.xaml`, add two ShellContent entries above the `profiles` entry:

```xml
<ShellContent Route="register"
              ContentTemplate="{DataTemplate views:RegisterPage}"
              Shell.TabBarIsVisible="False" />

<ShellContent Route="signin"
              ContentTemplate="{DataTemplate views:SignInPage}"
              Shell.TabBarIsVisible="False" />
```

Replace `SplashPage.OnAppearing` (Plan 1 Task 9) with:

```csharp
protected override async void OnAppearing()
{
    base.OnAppearing();

    await LoadingBar.ProgressTo(1.0, 1200, Easing.CubicInOut);

    if (!Preferences.Default.Get("onboarding_complete", false))
    {
        await Shell.Current.GoToAsync("//onboarding");
        return;
    }

    var auth = Handler!.MauiContext!.Services.GetRequiredService<IAuthService>();
    var registered = await auth.IsRegisteredAsync();

    await Shell.Current.GoToAsync(registered ? "//signin" : "//register");
}
```

Add `using TongaKids.Services;` to `SplashPage.xaml.cs`. Onboarding's `FinishAsync`
(Plan 1 Task 9) must also change its destination from `//profiles` to `//register`.

- [ ] **Step 5: Register, run, verify the whole entry flow**

Add to `MauiProgram.cs`:

```csharp
builder.Services.AddTransient<RegisterViewModel>();
builder.Services.AddTransient<SignInViewModel>();
builder.Services.AddTransient<RegisterPage>();
builder.Services.AddTransient<SignInPage>();
```

Uninstall the app first so this is a genuine first run:

```bash
adb uninstall com.companyname.tongakids
dotnet build TongaKids/TongaKids.csproj -f net10.0-android -t:Run
```

Confirm each:

1. Splash → onboarding → **register**. Submitting with a 3-character password shows
   "Your password needs at least 6 characters" and does not proceed.
2. A valid registration goes to profile selection. Add a learner and reach Home.
3. Force-close and relaunch: splash goes to **sign-in**, not registration.
4. A wrong password shows "That password did not match" and stays put.
5. The correct password reaches profile selection.
6. "Forgot password?" with the wrong security answer reports no match; with the right
   answer it changes the password, and the new password then signs in.
7. Navigate to `selfcheck` and confirm the auth checks pass alongside the Plan 1 checks.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "Add guardian registration and sign-in

Satisfies report section 3.4.2 while keeping the child's path free of any
credential field: a guardian registers once, then children pick a face.
Recovery is a security question, since there is no backend to send mail.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 19: Story content and library (UC-02)

**Files:**
- Create: `TongaKids/Models/Story.cs`, `StoryPage.cs`, `StoryReadState.cs`, `ReadingSession.cs`
- Create: `TongaKids/Data/StoryRepository.cs`
- Modify: `TongaKids/Models/ContentPack.cs`, `TongaKids/Data/ContentSeeder.cs`,
  `TongaKids/Data/TongaKidsDatabase.cs`, `TongaKids/Resources/Raw/content/chitonga-content.json`
- Modify: `TongaKids/Views/StoriesPage.xaml(.cs)`, create `TongaKids/ViewModels/StoryLibraryViewModel.cs`

**Interfaces:**
- Produces:
  - `IStoryRepository.GetStoriesAsync() → Task<List<Story>>`, `GetPagesAsync(int storyId) → Task<List<StoryPage>>`,
    `GetReadStateAsync(int learnerId, int storyId) → Task<StoryReadState?>`, `SaveReadStateAsync(StoryReadState) → Task`,
    `AddSessionAsync(ReadingSession) → Task`, `GetSessionsAsync(int learnerId) → Task<List<ReadingSession>>`
  - Navigation to `reader?storyId=N`

> **Content note.** The two stories below carry **English placeholder text** so the reader
> is testable the moment it is built. They are structural samples, not Chitonga content.
> Replace `text` with real Chitonga before the defence — two short folktales of five pages
> each is a small authoring job and makes the demo far stronger. Everything else in this
> task works unchanged when you do.

- [ ] **Step 1: Write the models**

```csharp
// Story.cs
using SQLite;

namespace TongaKids.Models;

[Table("Story")]
public class Story
{
    [PrimaryKey] public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CoverImageKey { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
```

```csharp
// StoryPage.cs
using SQLite;

namespace TongaKids.Models;

[Table("StoryPage")]
public class StoryPage
{
    [PrimaryKey] public int Id { get; set; }
    [Indexed] public int StoryId { get; set; }
    public int PageNumber { get; set; }
    public string Text { get; set; } = string.Empty;
    public string ImageKey { get; set; } = string.Empty;
    public string AudioKey { get; set; } = string.Empty;
    /// <summary>Used by the words-per-minute calculation.</summary>
    public int WordCount { get; set; }
}
```

```csharp
// StoryReadState.cs
using SQLite;

namespace TongaKids.Models;

[Table("StoryReadState")]
public class StoryReadState
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int LearnerId { get; set; }
    [Indexed] public int StoryId { get; set; }
    public int LastPageRead { get; set; }
    public bool IsCompleted { get; set; }
}
```

```csharp
// ReadingSession.cs
using SQLite;

namespace TongaKids.Models;

/// <summary>
/// One reading sitting. Feeds the words-per-minute figure on the parent dashboard.
/// </summary>
[Table("ReadingSession")]
public class ReadingSession
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int LearnerId { get; set; }
    [Indexed] public int StoryId { get; set; }
    public int WordsRead { get; set; }
    public long DurationMs { get; set; }
    public DateTime StartedAt { get; set; }
}
```

Add all four to `TongaKidsDatabase.GetConnectionAsync`:

```csharp
await connection.CreateTableAsync<Story>();
await connection.CreateTableAsync<StoryPage>();
await connection.CreateTableAsync<StoryReadState>();
await connection.CreateTableAsync<ReadingSession>();
```

- [ ] **Step 2: Extend the content pack**

Add DTOs to `TongaKids/Models/ContentPack.cs`:

```csharp
public sealed class ContentStory
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
    [JsonPropertyName("coverImageKey")] public string CoverImageKey { get; set; } = string.Empty;
    [JsonPropertyName("sortOrder")] public int SortOrder { get; set; }
    [JsonPropertyName("pages")] public List<ContentStoryPage> Pages { get; set; } = [];
}

public sealed class ContentStoryPage
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("pageNumber")] public int PageNumber { get; set; }
    [JsonPropertyName("text")] public string Text { get; set; } = string.Empty;
    [JsonPropertyName("imageKey")] public string ImageKey { get; set; } = string.Empty;
    [JsonPropertyName("audioKey")] public string AudioKey { get; set; } = string.Empty;
}
```

And to `ContentPack` itself:

```csharp
[JsonPropertyName("stories")] public List<ContentStory> Stories { get; set; } = [];
```

In `chitonga-content.json`, bump `"version"` to `2` and add a `stories` array as a sibling
of `levels`:

```json
"stories": [
  {
    "id": 1,
    "title": "Sulwe a Fulwe",
    "coverImageKey": "illus_lion_reading",
    "sortOrder": 1,
    "pages": [
      { "id": 101, "pageNumber": 1, "text": "PLACEHOLDER - replace with Chitonga. Long ago, Hare and Tortoise lived by the river.", "imageKey": "illus_lion_reading", "audioKey": "story1_p1" },
      { "id": 102, "pageNumber": 2, "text": "PLACEHOLDER - replace with Chitonga. Hare was fast. Tortoise was slow but wise.", "imageKey": "illus_splash_baobab", "audioKey": "story1_p2" },
      { "id": 103, "pageNumber": 3, "text": "PLACEHOLDER - replace with Chitonga. They agreed to race to the big tree.", "imageKey": "illus_splash_baobab", "audioKey": "story1_p3" }
    ]
  },
  {
    "id": 2,
    "title": "Mwana a Mulonga",
    "coverImageKey": "illus_splash_baobab",
    "sortOrder": 2,
    "pages": [
      { "id": 201, "pageNumber": 1, "text": "PLACEHOLDER - replace with Chitonga. A child walked to the river at sunrise.", "imageKey": "illus_splash_baobab", "audioKey": "story2_p1" },
      { "id": 202, "pageNumber": 2, "text": "PLACEHOLDER - replace with Chitonga. The water was cool and the birds were singing.", "imageKey": "illus_lion_reading", "audioKey": "story2_p2" }
    ]
  }
]
```

- [ ] **Step 3: Seed the stories**

In `ContentSeeder.SeedIfNeededAsync`, add to the wipe block:

```csharp
await db.DeleteAllAsync<StoryPage>();
await db.DeleteAllAsync<Story>();
```

And after the levels loop, before `Preferences.Default.Set`:

```csharp
foreach (var story in pack.Stories)
{
    if (story.Id <= 0 || string.IsNullOrWhiteSpace(story.Title))
    {
        continue;
    }

    await db.InsertAsync(new Story
    {
        Id = story.Id,
        Title = story.Title,
        CoverImageKey = story.CoverImageKey,
        SortOrder = story.SortOrder
    });

    foreach (var page in story.Pages)
    {
        if (page.Id <= 0)
        {
            continue;
        }

        await db.InsertAsync(new StoryPage
        {
            Id = page.Id,
            StoryId = story.Id,
            PageNumber = page.PageNumber,
            Text = page.Text,
            ImageKey = page.ImageKey,
            AudioKey = page.AudioKey,
            // Word count is derived, never authored — it must match the text exactly
            // or words-per-minute silently lies.
            WordCount = page.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
        });
    }
}
```

- [ ] **Step 4: Write the repository**

`TongaKids/Data/StoryRepository.cs`:

```csharp
using TongaKids.Models;

namespace TongaKids.Data;

public interface IStoryRepository
{
    Task<List<Story>> GetStoriesAsync();
    Task<List<StoryPage>> GetPagesAsync(int storyId);
    Task<StoryReadState?> GetReadStateAsync(int learnerId, int storyId);
    Task SaveReadStateAsync(StoryReadState state);
    Task AddSessionAsync(ReadingSession session);
    Task<List<ReadingSession>> GetSessionsAsync(int learnerId);
}

public sealed class StoryRepository(ITongaKidsDatabase database) : IStoryRepository
{
    public async Task<List<Story>> GetStoriesAsync()
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<Story>().OrderBy(s => s.SortOrder).ToListAsync();
    }

    public async Task<List<StoryPage>> GetPagesAsync(int storyId)
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<StoryPage>()
            .Where(p => p.StoryId == storyId)
            .OrderBy(p => p.PageNumber)
            .ToListAsync();
    }

    public async Task<StoryReadState?> GetReadStateAsync(int learnerId, int storyId)
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<StoryReadState>()
            .Where(s => s.LearnerId == learnerId && s.StoryId == storyId)
            .FirstOrDefaultAsync();
    }

    public async Task SaveReadStateAsync(StoryReadState state)
    {
        var db = await database.GetConnectionAsync();
        if (state.Id == 0)
        {
            await db.InsertAsync(state);
        }
        else
        {
            await db.UpdateAsync(state);
        }
    }

    public async Task AddSessionAsync(ReadingSession session)
    {
        var db = await database.GetConnectionAsync();
        await db.InsertAsync(session);
    }

    public async Task<List<ReadingSession>> GetSessionsAsync(int learnerId)
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<ReadingSession>().Where(s => s.LearnerId == learnerId).ToListAsync();
    }
}
```

- [ ] **Step 5: Write the library view model and page**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Data;
using TongaKids.Models;

namespace TongaKids.ViewModels;

public sealed partial class StoryLibraryViewModel(IStoryRepository stories) : ObservableObject
{
    public ObservableCollection<Story> Stories { get; } = [];

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            Stories.Clear();
            foreach (var story in await stories.GetStoriesAsync())
            {
                Stories.Add(story);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Library] load failed: {ex}");
        }
    }

    [RelayCommand]
    private static async Task OpenAsync(Story? story)
    {
        if (story is not null)
        {
            await Shell.Current.GoToAsync($"reader?storyId={story.Id}");
        }
    }
}
```

`TongaKids/Views/StoriesPage.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:models="clr-namespace:TongaKids.Models"
             xmlns:vm="clr-namespace:TongaKids.ViewModels"
             x:Class="TongaKids.Views.StoriesPage"
             x:DataType="vm:StoryLibraryViewModel"
             Shell.NavBarIsVisible="False">

    <ScrollView>
        <VerticalStackLayout Padding="{StaticResource ScreenPadding}" Spacing="16">
            <Label Text="Story Library" Style="{StaticResource HeadlineLg}"
                   TextColor="{StaticResource Primary}" />
            <Label Text="Choose a story to read together." Style="{StaticResource BodyMd}" />

            <CollectionView ItemsSource="{Binding Stories}" SelectionMode="None">
                <CollectionView.ItemsLayout>
                    <LinearItemsLayout Orientation="Vertical" ItemSpacing="16" />
                </CollectionView.ItemsLayout>
                <CollectionView.ItemTemplate>
                    <DataTemplate x:DataType="models:Story">
                        <Border Style="{StaticResource Card}" Padding="12">
                            <Border.GestureRecognizers>
                                <TapGestureRecognizer
                                    Command="{Binding Source={RelativeSource AncestorType={x:Type vm:StoryLibraryViewModel}}, Path=OpenCommand}"
                                    CommandParameter="{Binding .}" />
                            </Border.GestureRecognizers>
                            <Grid ColumnDefinitions="Auto,*" ColumnSpacing="16">
                                <Border Grid.Column="0" WidthRequest="88" HeightRequest="88"
                                        StrokeThickness="0">
                                    <Border.StrokeShape>
                                        <RoundRectangle CornerRadius="12" />
                                    </Border.StrokeShape>
                                    <Image Source="{Binding CoverImageKey, StringFormat='{0}.png'}"
                                           Aspect="AspectFill" />
                                </Border>
                                <Label Grid.Column="1" Text="{Binding Title}"
                                       Style="{StaticResource TitleLg}" VerticalOptions="Center" />
                            </Grid>
                        </Border>
                    </DataTemplate>
                </CollectionView.ItemTemplate>
            </CollectionView>
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

```csharp
using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class StoriesPage : ContentPage
{
    private readonly StoryLibraryViewModel _viewModel;

    public StoriesPage(StoryLibraryViewModel viewModel)
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

- [ ] **Step 6: Register, run, verify**

Add to `MauiProgram.cs`:

```csharp
builder.Services.AddSingleton<IStoryRepository, StoryRepository>();
builder.Services.AddTransient<StoryLibraryViewModel>();
```

Run: `dotnet build TongaKids/TongaKids.csproj -f net10.0-android -t:Run`

Expected: the Stories tab lists two stories with cover art. Because the pack version went
from 1 to 2, the seeder re-runs — confirm **phonics progress from Plan 1 survived**, since
seeding must never touch learner state. Tapping a story opens the (not yet built) reader.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "Add story models, seeding, and library screen

Extends the content pack with stories and pages. Word counts are derived
from the text at seed time rather than authored, so words-per-minute can
never disagree with what is on the page.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 20: Story reader with narration (UC-02, UC-03)

**Files:**
- Create: `TongaKids/Views/StoryReaderPage.xaml(.cs)`, `TongaKids/ViewModels/StoryReaderViewModel.cs`
- Modify: `TongaKids/AppShell.xaml.cs`, `TongaKids/MauiProgram.cs`

**Interfaces:**
- Consumes: `IStoryRepository`, `IAudioService`, `ILearnerSession`; query parameter `storyId`.
- Produces: route `reader`; persisted `ReadingSession` and `StoryReadState` rows — the
  first real consumer of `IProgressCalculator.WordsPerMinute`.

- [ ] **Step 1: Write the view model**

```csharp
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Data;
using TongaKids.Models;
using TongaKids.Services;

namespace TongaKids.ViewModels;

[QueryProperty(nameof(StoryId), "storyId")]
public sealed partial class StoryReaderViewModel(
    IStoryRepository stories,
    IAudioService audio,
    ILearnerSession session) : ObservableObject
{
    [ObservableProperty] private int _storyId;
    [ObservableProperty] private string _pageText = string.Empty;
    [ObservableProperty] private string _imageKey = string.Empty;
    [ObservableProperty] private string _pageLabel = string.Empty;
    [ObservableProperty] private bool _isLastPage;

    private List<StoryPage> _pages = [];
    private int _index;
    private int _wordsRead;
    private readonly Stopwatch _timer = new();

    partial void OnStoryIdChanged(int value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (StoryId <= 0)
        {
            return;
        }

        try
        {
            _pages = await stories.GetPagesAsync(StoryId);
            _index = 0;
            _wordsRead = 0;
            _timer.Restart();
            ShowPage();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Reader] load failed: {ex}");
        }
    }

    private void ShowPage()
    {
        if (_pages.Count == 0)
        {
            PageText = string.Empty;
            PageLabel = string.Empty;
            return;
        }

        var page = _pages[_index];
        PageText = page.Text;
        ImageKey = page.ImageKey;
        PageLabel = $"Page {_index + 1} of {_pages.Count}";
        IsLastPage = _index == _pages.Count - 1;
    }

    [RelayCommand]
    private async Task PlayNarrationAsync()
    {
        if (_pages.Count > 0)
        {
            await audio.PlayAsync(_pages[_index].AudioKey);
        }
    }

    [RelayCommand]
    private async Task NextAsync()
    {
        if (_pages.Count == 0)
        {
            return;
        }

        // Count the page just finished, then move on.
        _wordsRead += _pages[_index].WordCount;

        if (_index < _pages.Count - 1)
        {
            _index++;
            ShowPage();
            return;
        }

        await FinishAsync();
    }

    [RelayCommand]
    private void Previous()
    {
        if (_index > 0)
        {
            _index--;
            ShowPage();
        }
    }

    private async Task FinishAsync()
    {
        _timer.Stop();
        await audio.StopAsync();

        var learner = session.Current;
        if (learner is not null)
        {
            try
            {
                await stories.AddSessionAsync(new ReadingSession
                {
                    LearnerId = learner.Id,
                    StoryId = StoryId,
                    WordsRead = _wordsRead,
                    DurationMs = _timer.ElapsedMilliseconds,
                    StartedAt = DateTime.UtcNow
                });

                var state = await stories.GetReadStateAsync(learner.Id, StoryId)
                            ?? new StoryReadState { LearnerId = learner.Id, StoryId = StoryId };
                state.LastPageRead = _pages.Count;
                state.IsCompleted = true;
                await stories.SaveReadStateAsync(state);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Reader] save failed: {ex}");
            }
        }

        await Shell.Current.GoToAsync("//main/stories");
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
             x:Class="TongaKids.Views.StoryReaderPage"
             x:DataType="vm:StoryReaderViewModel"
             Title="{Binding PageLabel}">

    <Grid RowDefinitions="*,Auto" Padding="{StaticResource ScreenPadding}" RowSpacing="16">

        <ScrollView Grid.Row="0">
            <VerticalStackLayout Spacing="20">
                <Border Style="{StaticResource Card}" Padding="0" HeightRequest="260">
                    <Image Source="{Binding ImageKey, StringFormat='{0}.png'}" Aspect="AspectFill" />
                </Border>

                <!-- body-lg at 18px: comfortable for an emerging reader. -->
                <Label Text="{Binding PageText}" Style="{StaticResource BodyLg}"
                       LineHeight="1.6" />

                <VerticalStackLayout Spacing="8" HorizontalOptions="Center">
                    <Border WidthRequest="88" HeightRequest="88"
                            BackgroundColor="{StaticResource TertiaryContainer}"
                            StrokeThickness="0">
                        <Border.StrokeShape>
                            <RoundRectangle CornerRadius="44" />
                        </Border.StrokeShape>
                        <Border.GestureRecognizers>
                            <TapGestureRecognizer Command="{Binding PlayNarrationCommand}" />
                        </Border.GestureRecognizers>
                        <Label Text="volume_up" Style="{StaticResource Icon}" FontSize="40"
                               TextColor="{StaticResource OnTertiaryContainer}"
                               HorizontalOptions="Center" VerticalOptions="Center" />
                    </Border>
                    <Label Text="LISTEN" Style="{StaticResource LabelLg}" CharacterSpacing="2"
                           HorizontalTextAlignment="Center" />
                </VerticalStackLayout>
            </VerticalStackLayout>
        </ScrollView>

        <Grid Grid.Row="1" ColumnDefinitions="*,*" ColumnSpacing="12">
            <controls:TactileButton Grid.Column="0" Text="Back"
                                    BackgroundFill="{StaticResource SurfaceContainerLowest}"
                                    DepthColor="{StaticResource OutlineVariant}"
                                    TextColor="{StaticResource Secondary}"
                                    Command="{Binding PreviousCommand}" />
            <controls:TactileButton Grid.Column="1" Text="Next"
                                    BackgroundFill="{StaticResource PrimaryContainer}"
                                    DepthColor="{StaticResource OnPrimaryContainer}"
                                    TextColor="{StaticResource OnPrimary}"
                                    Command="{Binding NextCommand}" />
        </Grid>
    </Grid>
</ContentPage>
```

```csharp
using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class StoryReaderPage : ContentPage
{
    public StoryReaderPage(StoryReaderViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
```

- [ ] **Step 3: Register the route, run, verify**

In `AppShell.xaml.cs`: `Routing.RegisterRoute("reader", typeof(StoryReaderPage));`

In `MauiProgram.cs`:

```csharp
builder.Services.AddTransient<StoryReaderViewModel>();
builder.Services.AddTransient<StoryReaderPage>();
```

Run: `dotnet build TongaKids/TongaKids.csproj -f net10.0-android -t:Run`

Expected: opening "Sulwe a Fulwe" shows page 1 with its illustration and text. Next
advances through all three pages; the title bar tracks "Page N of 3". LISTEN logs
`[Audio] no clip for 'story1_p1'` with **no crash**. Finishing the last page returns to
the library. The words-per-minute figure this produced is shown in Task 24.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "Add story reader with narration and reading-session capture

Records words read and elapsed time per sitting, which is what makes the
parent dashboard's words-per-minute figure real rather than decorative.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 21: Progress analytics and the learner progress screen (UC-04)

**Files:**
- Create: `TongaKids/Services/IProgressAnalytics.cs`, `TongaKids/Services/ProgressAnalytics.cs`
- Modify: `TongaKids/Views/ProgressPage.xaml(.cs)`, create `TongaKids/ViewModels/ProgressViewModel.cs`
- Modify: `TongaKids/MauiProgram.cs`

**Interfaces:**
- Consumes: `IProgressRepository`, `IStoryRepository`, `IContentRepository`, `IProgressCalculator`.
- Produces:
  - `record LearnerStats(int LessonsMastered, int LessonsTotal, int StarsEarned, int AccuracyPercent, double WordsPerMinute, int StoriesRead, string WeakestLessonTitle)`
  - `IProgressAnalytics.ForLearnerAsync(int learnerId) → Task<LearnerStats>`

One analytics service serves both the child's progress screen and the parent dashboard
(Task 24). Two screens computing the same figures separately is how they drift apart.

- [ ] **Step 1: Write the analytics service**

```csharp
namespace TongaKids.Services;

public sealed record LearnerStats(
    int LessonsMastered,
    int LessonsTotal,
    int StarsEarned,
    int AccuracyPercent,
    double WordsPerMinute,
    int StoriesRead,
    string WeakestLessonTitle);

public interface IProgressAnalytics
{
    Task<LearnerStats> ForLearnerAsync(int learnerId);
}
```

```csharp
using TongaKids.Data;

namespace TongaKids.Services;

public sealed class ProgressAnalytics(
    IProgressRepository progress,
    IStoryRepository stories,
    IContentRepository content,
    IProgressCalculator calculator) : IProgressAnalytics
{
    public async Task<LearnerStats> ForLearnerAsync(int learnerId)
    {
        try
        {
            var lessonProgress = await progress.GetForLearnerAsync(learnerId);

            var levels = await content.GetLevelsAsync();
            var lessonsTotal = 0;
            var lessonTitles = new Dictionary<int, string>();
            foreach (var level in levels)
            {
                foreach (var lesson in await content.GetLessonsAsync(level.Id))
                {
                    lessonsTotal++;
                    lessonTitles[lesson.Id] = lesson.Title;
                }
            }

            // Mean of each attempted lesson's best score. Lessons never attempted
            // are excluded rather than counted as zero — a parent should see how
            // the child did at what they tried, not be punished for untouched content.
            var accuracy = lessonProgress.Count == 0
                ? 0
                : lessonProgress.Sum(p => p.BestScorePercent) / lessonProgress.Count;

            var sessions = await stories.GetSessionsAsync(learnerId);
            var totalWords = sessions.Sum(s => s.WordsRead);
            var totalMs = sessions.Sum(s => s.DurationMs);
            var wpm = calculator.WordsPerMinute(totalWords, totalMs);

            var weakest = lessonProgress
                .OrderBy(p => p.BestScorePercent)
                .FirstOrDefault();

            var weakestTitle = weakest is null
                ? "Nothing yet"
                : lessonTitles.GetValueOrDefault(weakest.LessonId, "A lesson");

            return new LearnerStats(
                LessonsMastered: lessonProgress.Count(p => p.IsMastered),
                LessonsTotal: lessonsTotal,
                StarsEarned: lessonProgress.Sum(p => p.StarsEarned),
                AccuracyPercent: accuracy,
                WordsPerMinute: wpm,
                StoriesRead: sessions.Select(s => s.StoryId).Distinct().Count(),
                WeakestLessonTitle: weakestTitle);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Analytics] failed: {ex}");
            return new LearnerStats(0, 0, 0, 0, 0, 0, "Nothing yet");
        }
    }
}
```

- [ ] **Step 2: Write the learner progress screen**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Services;

namespace TongaKids.ViewModels;

public sealed partial class ProgressViewModel(
    IProgressAnalytics analytics,
    ILearnerSession session) : ObservableObject
{
    [ObservableProperty] private string _title = "My Progress";
    [ObservableProperty] private int _stars;
    [ObservableProperty] private string _lessonsSummary = string.Empty;
    [ObservableProperty] private string _storiesSummary = string.Empty;
    [ObservableProperty] private double _completion;

    [RelayCommand]
    public async Task LoadAsync()
    {
        var learner = session.Current;
        if (learner is null)
        {
            await Shell.Current.GoToAsync("//profiles");
            return;
        }

        var stats = await analytics.ForLearnerAsync(learner.Id);

        Title = $"{learner.Name}'s Progress";
        Stars = Math.Min(stats.StarsEarned, 5);
        LessonsSummary = $"{stats.LessonsMastered} of {stats.LessonsTotal} lessons mastered";
        StoriesSummary = stats.StoriesRead == 1
            ? "1 story read"
            : $"{stats.StoriesRead} stories read";
        Completion = stats.LessonsTotal == 0
            ? 0
            : (double)stats.LessonsMastered / stats.LessonsTotal;
    }
}
```

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:controls="clr-namespace:TongaKids.Controls"
             xmlns:vm="clr-namespace:TongaKids.ViewModels"
             x:Class="TongaKids.Views.ProgressPage"
             x:DataType="vm:ProgressViewModel"
             Shell.NavBarIsVisible="False">

    <ScrollView>
        <VerticalStackLayout Padding="{StaticResource ScreenPadding}" Spacing="24">

            <Label Text="{Binding Title}" Style="{StaticResource HeadlineLg}"
                   TextColor="{StaticResource Primary}" />

            <controls:StarRating Earned="{Binding Stars}" Total="5" StarSize="44"
                                 HorizontalOptions="Center" />

            <Border Style="{StaticResource Card}">
                <VerticalStackLayout Spacing="12">
                    <Label Text="{Binding LessonsSummary}" Style="{StaticResource TitleLg}" />
                    <ProgressBar Progress="{Binding Completion}"
                                 ProgressColor="{StaticResource Tertiary}"
                                 Style="{StaticResource PillProgress}" />
                </VerticalStackLayout>
            </Border>

            <Border Style="{StaticResource Card}">
                <Label Text="{Binding StoriesSummary}" Style="{StaticResource TitleLg}" />
            </Border>

        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

```csharp
using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class ProgressPage : ContentPage
{
    private readonly ProgressViewModel _viewModel;

    public ProgressPage(ProgressViewModel viewModel)
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

```csharp
builder.Services.AddSingleton<IProgressAnalytics, ProgressAnalytics>();
builder.Services.AddTransient<ProgressViewModel>();
```

Run and confirm the Progress tab shows the learner's name, earned stars, the mastered
count, and a story count that matches what was actually read in Task 20.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "Add progress analytics and the learner progress screen

One analytics service computes every reported figure, so the child screen
and the parent dashboard cannot drift apart.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 22: Parental gateway PIN challenge (UC-05)

**Files:**
- Create: `TongaKids/Models/ParentSettings.cs`
- Create: `TongaKids/Services/IParentalGateService.cs`, `ParentalGateService.cs`
- Create: `TongaKids/Views/ParentalGatePage.xaml(.cs)`, `TongaKids/ViewModels/ParentalGateViewModel.cs`
- Modify: `TongaKids/Data/TongaKidsDatabase.cs`, `AppShell.xaml.cs`, `MauiProgram.cs`

**Interfaces:**
- Produces:
  - `IParentalGateService.HasPinAsync() → Task<bool>`, `SetPinAsync(string pin) → Task`,
    `VerifyPinAsync(string pin) → Task<bool>`, `IsUnlocked { get; }`, `Lock()`
  - Routes `parentgate`, and gated routes `consent`, `parentdashboard`

The gate must be genuinely closed: `IsUnlocked` is in-memory only and resets whenever the
app restarts or the parent leaves the parent area.

- [ ] **Step 1: Write the model and service**

```csharp
// ParentSettings.cs
using SQLite;

namespace TongaKids.Models;

[Table("ParentSettings")]
public class ParentSettings
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string PinHash { get; set; } = string.Empty;
    public string PinSalt { get; set; } = string.Empty;
    public DateTime? ConsentGivenAt { get; set; }
    public int ConsentVersion { get; set; }
    public bool SyncEnabled { get; set; }
}
```

Add `await connection.CreateTableAsync<ParentSettings>();` to the database.

```csharp
using System.Security.Cryptography;
using System.Text;
using TongaKids.Data;
using TongaKids.Models;

namespace TongaKids.Services;

public interface IParentalGateService
{
    bool IsUnlocked { get; }
    Task<bool> HasPinAsync();
    Task SetPinAsync(string pin);
    Task<bool> VerifyPinAsync(string pin);
    void Lock();
    Task<ParentSettings> GetSettingsAsync();
    Task SaveSettingsAsync(ParentSettings settings);
}

public sealed class ParentalGateService(ITongaKidsDatabase database) : IParentalGateService
{
    private const int Iterations = 100_000;

    /// <summary>In-memory only: closing the app relocks the gate.</summary>
    public bool IsUnlocked { get; private set; }

    public async Task<bool> HasPinAsync() =>
        !string.IsNullOrEmpty((await GetSettingsAsync()).PinHash);

    public async Task SetPinAsync(string pin)
    {
        var settings = await GetSettingsAsync();
        var salt = RandomNumberGenerator.GetBytes(16);
        settings.PinSalt = Convert.ToBase64String(salt);
        settings.PinHash = Convert.ToBase64String(Derive(pin, salt));
        await SaveSettingsAsync(settings);
        IsUnlocked = true;
    }

    public async Task<bool> VerifyPinAsync(string pin)
    {
        var settings = await GetSettingsAsync();
        if (string.IsNullOrEmpty(settings.PinHash))
        {
            return false;
        }

        var salt = Convert.FromBase64String(settings.PinSalt);
        var ok = CryptographicOperations.FixedTimeEquals(
            Derive(pin, salt), Convert.FromBase64String(settings.PinHash));

        IsUnlocked = ok;
        return ok;
    }

    public void Lock() => IsUnlocked = false;

    public async Task<ParentSettings> GetSettingsAsync()
    {
        var db = await database.GetConnectionAsync();
        return await db.Table<ParentSettings>().FirstOrDefaultAsync() ?? new ParentSettings();
    }

    public async Task SaveSettingsAsync(ParentSettings settings)
    {
        var db = await database.GetConnectionAsync();
        if (settings.Id == 0)
        {
            await db.InsertAsync(settings);
        }
        else
        {
            await db.UpdateAsync(settings);
        }
    }

    private static byte[] Derive(string pin, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(pin ?? string.Empty), salt, Iterations,
            HashAlgorithmName.SHA256, 32);
}
```

- [ ] **Step 2: Write the gate screen**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Services;

namespace TongaKids.ViewModels;

public sealed partial class ParentalGateViewModel(IParentalGateService gate) : ObservableObject
{
    [ObservableProperty] private string _pin = string.Empty;
    [ObservableProperty] private string _heading = "Parents only";
    [ObservableProperty] private string _instruction = string.Empty;
    [ObservableProperty] private string _error = string.Empty;
    [ObservableProperty] private bool _hasError;

    private bool _isFirstTime;

    [RelayCommand]
    public async Task LoadAsync()
    {
        _isFirstTime = !await gate.HasPinAsync();
        Instruction = _isFirstTime
            ? "Choose a 4-digit PIN. You will need it to see your child's progress."
            : "Enter your 4-digit PIN.";
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        HasError = false;

        if (Pin.Length != 4 || !Pin.All(char.IsDigit))
        {
            Error = "The PIN must be 4 digits.";
            HasError = true;
            return;
        }

        if (_isFirstTime)
        {
            await gate.SetPinAsync(Pin);
        }
        else if (!await gate.VerifyPinAsync(Pin))
        {
            Error = "That PIN is not correct.";
            HasError = true;
            Pin = string.Empty;
            return;
        }

        await Shell.Current.GoToAsync("parentdashboard");
    }
}
```

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:controls="clr-namespace:TongaKids.Controls"
             xmlns:vm="clr-namespace:TongaKids.ViewModels"
             x:Class="TongaKids.Views.ParentalGatePage"
             x:DataType="vm:ParentalGateViewModel"
             Title="Parents only">

    <VerticalStackLayout Padding="{StaticResource ScreenPadding}" Spacing="20"
                         VerticalOptions="Center">

        <Label Text="lock" Style="{StaticResource Icon}" FontSize="64"
               TextColor="{StaticResource Primary}" HorizontalOptions="Center" />

        <Label Text="{Binding Heading}" Style="{StaticResource HeadlineLg}"
               HorizontalTextAlignment="Center" />
        <Label Text="{Binding Instruction}" Style="{StaticResource BodyMd}"
               HorizontalTextAlignment="Center" />

        <Entry Text="{Binding Pin}" Keyboard="Numeric" MaxLength="4" IsPassword="True"
               HorizontalTextAlignment="Center" FontSize="32" />

        <Label Text="{Binding Error}" IsVisible="{Binding HasError}"
               Style="{StaticResource BodyMd}" TextColor="{StaticResource Error}"
               HorizontalTextAlignment="Center" />

        <controls:TactileButton Text="Continue"
                                BackgroundFill="{StaticResource Primary}"
                                DepthColor="{StaticResource OnPrimaryFixed}"
                                TextColor="{StaticResource OnPrimary}"
                                Command="{Binding SubmitCommand}" />
    </VerticalStackLayout>
</ContentPage>
```

```csharp
using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class ParentalGatePage : ContentPage
{
    private readonly ParentalGateViewModel _viewModel;

    public ParentalGatePage(ParentalGateViewModel viewModel)
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

- [ ] **Step 3: Register, run, verify the gate actually closes**

```csharp
builder.Services.AddSingleton<IParentalGateService, ParentalGateService>();
builder.Services.AddTransient<ParentalGateViewModel>();
builder.Services.AddTransient<ParentalGatePage>();
```

`AppShell.xaml.cs`: `Routing.RegisterRoute("parentgate", typeof(ParentalGatePage));`

Verify: first entry asks you to choose a PIN. Force-close, reopen, and the gate now asks
for the PIN — and a wrong PIN does not let you through.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "Add parental gateway PIN challenge

PBKDF2-hashed PIN with constant-time comparison. The unlocked flag is
in-memory only, so restarting the app relocks the parent area.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 23: Consent screen (UC-06)

**Files:**
- Create: `TongaKids/Views/ConsentPage.xaml(.cs)`, `TongaKids/ViewModels/ConsentViewModel.cs`
- Modify: `AppShell.xaml.cs`, `MauiProgram.cs`

**Interfaces:**
- Consumes: `IParentalGateService`.
- Produces: route `consent`; writes `ParentSettings.ConsentGivenAt` and `ConsentVersion`.

- [ ] **Step 1: Write the view model**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Services;

namespace TongaKids.ViewModels;

public sealed partial class ConsentViewModel(IParentalGateService gate) : ObservableObject
{
    public const int CurrentConsentVersion = 1;

    [ObservableProperty] private bool _hasConsented;
    [ObservableProperty] private string _statusText = "Consent has not been given.";

    [RelayCommand]
    public async Task LoadAsync()
    {
        var settings = await gate.GetSettingsAsync();
        HasConsented = settings.ConsentGivenAt is not null;
        StatusText = HasConsented
            ? $"Consent given on {settings.ConsentGivenAt:d MMMM yyyy}."
            : "Consent has not been given.";
    }

    [RelayCommand]
    private async Task GiveConsentAsync()
    {
        var settings = await gate.GetSettingsAsync();
        settings.ConsentGivenAt = DateTime.UtcNow;
        settings.ConsentVersion = CurrentConsentVersion;
        await gate.SaveSettingsAsync(settings);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task WithdrawConsentAsync()
    {
        var settings = await gate.GetSettingsAsync();
        settings.ConsentGivenAt = null;
        await gate.SaveSettingsAsync(settings);
        await LoadAsync();
    }
}
```

- [ ] **Step 2: Write the page**

The wording matters here — it is the screen that demonstrates the report's Data Protection
Act compliance claim.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:controls="clr-namespace:TongaKids.Controls"
             xmlns:vm="clr-namespace:TongaKids.ViewModels"
             x:Class="TongaKids.Views.ConsentPage"
             x:DataType="vm:ConsentViewModel"
             Title="Consent">

    <ScrollView>
        <VerticalStackLayout Padding="{StaticResource ScreenPadding}" Spacing="16">

            <Label Text="Your child's data" Style="{StaticResource HeadlineLg}"
                   TextColor="{StaticResource Primary}" />

            <Border Style="{StaticResource Card}">
                <VerticalStackLayout Spacing="12">
                    <Label Style="{StaticResource BodyMd}"
                           Text="TongaKids Read stores your child's first name, the lessons they complete, their scores, and how long they spend reading." />
                    <Label Style="{StaticResource BodyMd}"
                           Text="All of this is kept on this device only. Nothing is sent over the internet, and the app works with no connection at all." />
                    <Label Style="{StaticResource BodyMd}"
                           Text="You can withdraw your consent at any time. This is asked of you under the Zambia Data Protection Act 2021." />
                </VerticalStackLayout>
            </Border>

            <Label Text="{Binding StatusText}" Style="{StaticResource LabelLg}" />

            <controls:TactileButton Text="I give my consent"
                                    IsVisible="{Binding HasConsented, Converter={StaticResource Invert}}"
                                    BackgroundFill="{StaticResource Tertiary}"
                                    DepthColor="{StaticResource OnTertiaryFixed}"
                                    TextColor="{StaticResource OnTertiary}"
                                    Command="{Binding GiveConsentCommand}" />

            <controls:TactileButton Text="Withdraw consent"
                                    IsVisible="{Binding HasConsented}"
                                    BackgroundFill="{StaticResource SurfaceContainerLowest}"
                                    DepthColor="{StaticResource OutlineVariant}"
                                    TextColor="{StaticResource Error}"
                                    Command="{Binding WithdrawConsentCommand}" />
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

- [ ] **Step 3: Add the invert converter**

Create `TongaKids/Converters/InvertBoolConverter.cs`:

```csharp
using System.Globalization;

namespace TongaKids.Converters;

public sealed class InvertBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not true;
}
```

Register in `App.xaml` alongside the others: `<converters:InvertBoolConverter x:Key="Invert" />`

- [ ] **Step 4: Code-behind, register, verify**

```csharp
using TongaKids.ViewModels;

namespace TongaKids.Views;

public partial class ConsentPage : ContentPage
{
    private readonly ConsentViewModel _viewModel;

    public ConsentPage(ConsentViewModel viewModel)
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

Register the route `consent`, the page and the view model. Verify giving consent shows the
date, withdrawing clears it, and both survive an app restart.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "Add consent screen

States plainly what is stored, that it never leaves the device, and that
consent can be withdrawn. Records the timestamp and version.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 24: Parent dashboard (UC-07)

**Files:**
- Create: `TongaKids/Views/ParentDashboardPage.xaml(.cs)`, `TongaKids/ViewModels/ParentDashboardViewModel.cs`
- Modify: `AppShell.xaml.cs`, `MauiProgram.cs`

**Interfaces:**
- Consumes: `IProgressAnalytics`, `ILearnerRepository`, `IParentalGateService`.
- Produces: route `parentdashboard`.

Shows real numbers: reading accuracy, words per minute, lessons mastered, and the weakest
lesson — the "areas where the child is struggling" the report's UC-07 calls for. **No charts.**

- [ ] **Step 1: Write the view model**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Data;
using TongaKids.Services;

namespace TongaKids.ViewModels;

public sealed record LearnerReport(
    string Name, string Accuracy, string WordsPerMinute, string Lessons, string Weakest);

public sealed partial class ParentDashboardViewModel(
    ILearnerRepository learners,
    IProgressAnalytics analytics,
    IParentalGateService gate) : ObservableObject
{
    public ObservableCollection<LearnerReport> Reports { get; } = [];

    [ObservableProperty] private bool _isEmpty;

    [RelayCommand]
    public async Task LoadAsync()
    {
        // Defence in depth: never render this screen if the gate was bypassed.
        if (!gate.IsUnlocked)
        {
            await Shell.Current.GoToAsync("//main/settings");
            return;
        }

        Reports.Clear();

        foreach (var learner in await learners.GetAllAsync())
        {
            var stats = await analytics.ForLearnerAsync(learner.Id);
            Reports.Add(new LearnerReport(
                Name: learner.Name,
                Accuracy: $"{stats.AccuracyPercent}%",
                WordsPerMinute: $"{stats.WordsPerMinute:0} wpm",
                Lessons: $"{stats.LessonsMastered} of {stats.LessonsTotal}",
                Weakest: stats.WeakestLessonTitle));
        }

        IsEmpty = Reports.Count == 0;
    }

    [RelayCommand]
    private static async Task OpenConsentAsync() => await Shell.Current.GoToAsync("consent");
}
```

- [ ] **Step 2: Write the page**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:controls="clr-namespace:TongaKids.Controls"
             xmlns:vm="clr-namespace:TongaKids.ViewModels"
             x:Class="TongaKids.Views.ParentDashboardPage"
             x:DataType="vm:ParentDashboardViewModel"
             Title="Progress Dashboard">

    <ScrollView>
        <VerticalStackLayout Padding="{StaticResource ScreenPadding}" Spacing="16">

            <Label Text="How your children are doing" Style="{StaticResource HeadlineLg}"
                   TextColor="{StaticResource Primary}" />

            <Label Text="No learning recorded yet." Style="{StaticResource BodyMd}"
                   IsVisible="{Binding IsEmpty}" />

            <CollectionView ItemsSource="{Binding Reports}" SelectionMode="None">
                <CollectionView.ItemsLayout>
                    <LinearItemsLayout Orientation="Vertical" ItemSpacing="16" />
                </CollectionView.ItemsLayout>
                <CollectionView.ItemTemplate>
                    <DataTemplate x:DataType="vm:LearnerReport">
                        <Border Style="{StaticResource Card}">
                            <VerticalStackLayout Spacing="12">
                                <Label Text="{Binding Name}" Style="{StaticResource TitleLg}" />

                                <Grid ColumnDefinitions="*,*" RowDefinitions="Auto,Auto"
                                      RowSpacing="12">
                                    <VerticalStackLayout Grid.Row="0" Grid.Column="0">
                                        <Label Text="Reading accuracy" Style="{StaticResource LabelLg}" />
                                        <Label Text="{Binding Accuracy}" Style="{StaticResource HeadlineLg}"
                                               TextColor="{StaticResource Tertiary}" />
                                    </VerticalStackLayout>

                                    <VerticalStackLayout Grid.Row="0" Grid.Column="1">
                                        <Label Text="Reading speed" Style="{StaticResource LabelLg}" />
                                        <Label Text="{Binding WordsPerMinute}" Style="{StaticResource HeadlineLg}"
                                               TextColor="{StaticResource Secondary}" />
                                    </VerticalStackLayout>

                                    <VerticalStackLayout Grid.Row="1" Grid.Column="0">
                                        <Label Text="Lessons mastered" Style="{StaticResource LabelLg}" />
                                        <Label Text="{Binding Lessons}" Style="{StaticResource TitleLg}" />
                                    </VerticalStackLayout>

                                    <VerticalStackLayout Grid.Row="1" Grid.Column="1">
                                        <Label Text="Needs practice" Style="{StaticResource LabelLg}" />
                                        <Label Text="{Binding Weakest}" Style="{StaticResource BodyMd}" />
                                    </VerticalStackLayout>
                                </Grid>
                            </VerticalStackLayout>
                        </Border>
                    </DataTemplate>
                </CollectionView.ItemTemplate>
            </CollectionView>

            <controls:TactileButton Text="Consent and data"
                                    BackgroundFill="{StaticResource Secondary}"
                                    DepthColor="{StaticResource OnSecondaryFixedVariant}"
                                    TextColor="{StaticResource OnSecondary}"
                                    Command="{Binding OpenConsentCommand}" />
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

Code-behind follows the same pattern as `ConsentPage` in Task 23 Step 4, with
`ParentDashboardViewModel` in place of `ConsentViewModel` and the class renamed to
`ParentDashboardPage`.

- [ ] **Step 3: Register, run, verify**

Register the route `parentdashboard`, the page, and the view model.

Verify: entering the correct PIN reaches the dashboard, showing each learner with real
accuracy, a words-per-minute figure that reflects the reading done in Task 20, the mastered
count, and the weakest lesson. "Consent and data" opens the consent screen.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "Add parent dashboard

Reading accuracy, words per minute, lessons mastered and the weakest lesson,
all computed from real rows by the shared analytics service.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 25: Settings, offline verification, and the demo pass

**Files:**
- Modify: `TongaKids/Views/SettingsPage.xaml(.cs)`, create `TongaKids/ViewModels/SettingsViewModel.cs`
- Modify: `TongaKids/MauiProgram.cs`
- Modify: `TongaKids/TongaKids.csproj`

- [ ] **Step 1: Write the settings screen**

Settings is the child-reachable entry to the parent area, plus profile switching.

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TongaKids.Services;

namespace TongaKids.ViewModels;

public sealed partial class SettingsViewModel(
    ILearnerSession session,
    IParentalGateService gate) : ObservableObject
{
    [ObservableProperty] private string _currentLearner = string.Empty;

    [RelayCommand]
    public Task LoadAsync()
    {
        CurrentLearner = session.Current?.Name ?? "No one selected";
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task SwitchLearnerAsync()
    {
        session.Clear();
        await Shell.Current.GoToAsync("//profiles");
    }

    [RelayCommand]
    private async Task OpenParentAreaAsync()
    {
        // Always relock before presenting the gate: no stale unlock from earlier.
        gate.Lock();
        await Shell.Current.GoToAsync("parentgate");
    }

#if DEBUG
    [RelayCommand]
    private static async Task OpenSelfCheckAsync() => await Shell.Current.GoToAsync("selfcheck");
#endif
}
```

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:controls="clr-namespace:TongaKids.Controls"
             xmlns:vm="clr-namespace:TongaKids.ViewModels"
             x:Class="TongaKids.Views.SettingsPage"
             x:DataType="vm:SettingsViewModel"
             Shell.NavBarIsVisible="False">

    <ScrollView>
        <VerticalStackLayout Padding="{StaticResource ScreenPadding}" Spacing="16">

            <Label Text="Settings" Style="{StaticResource HeadlineLg}"
                   TextColor="{StaticResource Primary}" />

            <Border Style="{StaticResource Card}">
                <VerticalStackLayout Spacing="4">
                    <Label Text="Currently learning" Style="{StaticResource LabelLg}" />
                    <Label Text="{Binding CurrentLearner}" Style="{StaticResource TitleLg}" />
                </VerticalStackLayout>
            </Border>

            <controls:TactileButton Text="Switch learner"
                                    BackgroundFill="{StaticResource SecondaryContainer}"
                                    DepthColor="{StaticResource OnSecondaryContainer}"
                                    TextColor="{StaticResource OnSecondaryFixed}"
                                    Command="{Binding SwitchLearnerCommand}" />

            <controls:TactileButton Text="Parents only"
                                    BackgroundFill="{StaticResource Primary}"
                                    DepthColor="{StaticResource OnPrimaryFixed}"
                                    TextColor="{StaticResource OnPrimary}"
                                    Command="{Binding OpenParentAreaCommand}" />
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

Code-behind follows the `ProgressPage` pattern from Task 21 Step 2, renamed to
`SettingsPage` with `SettingsViewModel`.

- [ ] **Step 2: Verify offline operation**

This is the report's central technical claim and must be demonstrated, not asserted.

```bash
adb shell svc wifi disable
adb shell svc data disable
```

With **all connectivity off**, walk the entire app: sign in, pick a learner, complete a
phonics lesson and its game, read a story to the end, open Progress, enter the parent area
and view the dashboard.

Expected: every screen works identically. Nothing hangs, nothing times out, nothing shows
a connectivity message. Then re-enable:

```bash
adb shell svc wifi enable
```

- [ ] **Step 3: Confirm no network code exists**

```bash
grep -rniE "httpclient|webrequest|socket|https?://" TongaKids --include=*.cs --include=*.xaml
```

Expected: **no matches** outside XML namespace declarations (`xmlns=`). Any real hit is a
violation of the offline-first constraint and must be removed.

- [ ] **Step 4: Shrink the illustrations**

The 14 PNGs are 512×512 and total about 4.6 MB unoptimised. Header avatars render at 56dp.
Add to `TongaKids.csproj` inside the images `ItemGroup`:

```xml
<MauiImage Update="Resources\Images\avatar_header_*.png" Resize="True" BaseSize="128,128" />
```

Rebuild and confirm the app still shows every avatar correctly.

- [ ] **Step 5: Full demo rehearsal**

Uninstall, then walk the complete path once, in the order you will present it:

```bash
adb uninstall com.companyname.tongakids
dotnet build TongaKids/TongaKids.csproj -f net10.0-android -t:Run
```

1. Splash → onboarding → **register** (UC: report §3.4.2)
2. Profile selection → add a learner → Home
3. Learn Phonics → Level 1 → lesson → tap to hear → **UC-01, UC-03**
4. Play Game → answer all correctly → 5 stars → Level 2 unlocks → **mastery rule**
5. Stories tab → open a story → read to the end → **UC-02**
6. Progress tab → **UC-04**
7. Settings → Parents only → PIN → dashboard → **UC-05, UC-07**
8. Consent and data → give consent → **UC-06**

Every one of the seven use cases is reachable in under two minutes of tapping.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "Add settings, verify offline operation, shrink avatars

Completes the demo path: all seven use cases from the report are reachable.
Confirms no network code exists anywhere in the project.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Definition of done for Plan 2

- [ ] A guardian can register, sign out by restarting, sign back in, and reset a forgotten password.
- [ ] The child's path still contains no credential field anywhere.
- [ ] Two stories are readable end to end, with narration wired (silent until recorded).
- [ ] The Progress tab shows real figures for the current learner.
- [ ] The parent area cannot be entered without the correct PIN, and relocks on restart.
- [ ] Consent can be given and withdrawn, and survives a restart.
- [ ] The dashboard shows accuracy, words per minute, mastered lessons and the weakest lesson.
- [ ] The entire app works with Wi-Fi and mobile data disabled.
- [ ] `grep` for network APIs returns nothing.
- [ ] All seven use cases are demonstrable in one continuous walkthrough.

## Before the defence — owner tasks

These are content jobs, not code. The app runs without them; it presents far better with them.

1. **Replace the placeholder story text** in `chitonga-content.json` with real Chitonga.
   Two short folktales, three to five pages each. The word counts recompute automatically.
2. **Record the audio.** Run `python3 tools/generate-audio-manifest.py` for the exact list.
   Even five clips covering Level 1 vowels makes the tap-to-hear demo land.
3. **Fill the empty `exampleWord` and `gloss` fields** for the vowels and for DA, MA, PA.
4. **Decide the data-protection position** (spec open decision #4) in case you are asked
   why a children's app collects a guardian email.
