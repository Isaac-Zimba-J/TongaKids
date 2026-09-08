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
