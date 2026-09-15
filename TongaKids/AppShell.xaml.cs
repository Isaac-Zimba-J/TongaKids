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
        Routing.RegisterRoute("managestories", typeof(ManageStoriesPage));
        Routing.RegisterRoute("storyeditor", typeof(StoryEditorPage));
        Routing.RegisterRoute("managephonics", typeof(ManagePhonicsPage));
        Routing.RegisterRoute("lessoneditor", typeof(LessonEditorPage));

#if DEBUG
        Routing.RegisterRoute("selfcheck", typeof(SelfCheckPage));
#endif
    }
}
