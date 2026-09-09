using TongaKids.Services.SelfCheck;

namespace TongaKids.Views;

public partial class SelfCheckPage : ContentPage
{
    private static readonly Color PassColor = Color.FromArgb("#006e1c");
    private static readonly Color FailColor = Color.FromArgb("#ba1a1a");

    private readonly IEnumerable<ISelfCheck> _checks;
    private bool _hasRun;

    public SelfCheckPage(IEnumerable<ISelfCheck> checks)
    {
        InitializeComponent();
        _checks = checks;
    }

    // Runs here rather than in the constructor: the checks are async, and
    // blocking on them while constructing the page would deadlock the UI thread.
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_hasRun)
        {
            return;
        }

        _hasRun = true;

        var (passed, total) = await SelfCheckRunner.RunAsync(_checks, (area, result) =>
        {
            if (result is null)
            {
                ResultsLayout.Add(new Label
                {
                    Text = area,
                    FontFamily = "NunitoBold",
                    FontSize = 16,
                    Margin = new Thickness(0, 12, 0, 0)
                });
                return;
            }

            ResultsLayout.Add(new Label
            {
                Text = $"{(result.Passed ? "PASS" : "FAIL")}  {result.Name}"
                       + (result.Passed ? string.Empty : $"  ({result.Detail})"),
                FontFamily = "NunitoRegular",
                FontSize = 13,
                TextColor = result.Passed ? PassColor : FailColor
            });
        });

        SummaryLabel.Text = $"{passed} of {total} checks passed";
        SummaryLabel.TextColor = passed == total ? PassColor : FailColor;
    }
}
