using TongaKids.Data;
using TongaKids.Services.SelfCheck;

namespace TongaKids;

public partial class App : Application
{
    private readonly IContentSeeder _seeder;
    private readonly IEnumerable<ISelfCheck> _selfChecks;

    public App(IContentSeeder seeder, IEnumerable<ISelfCheck> selfChecks)
    {
        InitializeComponent();
        _seeder = seeder;
        _selfChecks = selfChecks;
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
            // Seeding must never take the app down; an empty library is recoverable.
            System.Diagnostics.Debug.WriteLine($"[App] seeding failed: {ex}");
        }

#if DEBUG
        // Print the engine self-check result to logcat so verification does not
        // depend on someone reading the diagnostic page.
        try
        {
            var (passed, total) = SelfCheckRunner.Run(_selfChecks, (area, result) =>
            {
                if (result is { Passed: false })
                {
                    System.Diagnostics.Debug.WriteLine($"[SelfCheck] FAIL {area}: {result.Name} ({result.Detail})");
                }
            });
            System.Diagnostics.Debug.WriteLine($"[SelfCheck] {passed} of {total} checks passed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SelfCheck] harness failed: {ex}");
        }
#endif
    }
}
