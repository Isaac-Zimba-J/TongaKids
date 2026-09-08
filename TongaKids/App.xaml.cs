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
            // Seeding must never take the app down; an empty library is recoverable.
            System.Diagnostics.Debug.WriteLine($"[App] seeding failed: {ex}");
        }
    }
}
