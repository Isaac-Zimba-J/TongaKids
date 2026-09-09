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
        Routing.RegisterRoute("reader", typeof(StoryReaderPage));
        Routing.RegisterRoute("parentgate", typeof(ParentalGatePage));
        Routing.RegisterRoute("parentdashboard", typeof(ParentDashboardPage));
        Routing.RegisterRoute("consent", typeof(ConsentPage));
        Routing.RegisterRoute("recordsounds", typeof(RecordSoundsPage));

#if DEBUG
        Routing.RegisterRoute("selfcheck", typeof(SelfCheckPage));
#endif
    }
}
