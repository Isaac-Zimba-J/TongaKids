using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using UXDivers.Popups.Maui;
using TongaKids.Data;
using TongaKids.Services;
using TongaKids.Services.SelfCheck;
using TongaKids.ViewModels;
using TongaKids.Views;

namespace TongaKids;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseUXDiversPopups()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("NunitoSans-Regular.ttf", "NunitoRegular");
				fonts.AddFont("NunitoSans-Medium.ttf", "NunitoMedium");
				fonts.AddFont("NunitoSans-Bold.ttf", "NunitoBold");
				fonts.AddFont("NunitoSans-ExtraBold.ttf", "NunitoExtraBold");
				fonts.AddFont("MaterialSymbolsOutlined.ttf", "MaterialSymbols");
			});

		// Data layer. Singletons: one connection, opened once, shared.
		builder.Services.AddSingleton<ITongaKidsDatabase, TongaKidsDatabase>();
		builder.Services.AddSingleton<ILearnerRepository, LearnerRepository>();
		builder.Services.AddSingleton<IContentRepository, ContentRepository>();
		builder.Services.AddSingleton<IProgressRepository, ProgressRepository>();
		builder.Services.AddSingleton<IStoryRepository, StoryRepository>();
		builder.Services.AddSingleton<IContentSeeder, ContentSeeder>();

		// Session is a singleton: one learner at a time, app-wide.
		builder.Services.AddSingleton<ILearnerSession, LearnerSession>();
		builder.Services.AddSingleton<IAuthService, AuthService>();

		// Engines. Pure classes with no MAUI or SQLite dependency.
		builder.Services.AddSingleton<IPhonicsEngine, PhonicsEngine>();
		builder.Services.AddSingleton<IProgressCalculator, ProgressCalculator>();
		builder.Services.AddSingleton<IMasteryEvaluator, MasteryEvaluator>();
		builder.Services.AddSingleton<IProgressAnalytics, ProgressAnalytics>();
		builder.Services.AddSingleton<IParentalGateService, ParentalGateService>();

		// Audio. Silent when a clip has not been recorded yet.
		builder.Services.AddSingleton(AudioManager.Current);
		builder.Services.AddSingleton<IAudioService, AudioService>();
		builder.Services.AddSingleton<IRecordingService, RecordingService>();

		// Popups and modals, via UXDivers Popups.
		builder.Services.AddSingleton<IDialogService, DialogService>();

		// View models are transient, matching their pages.
		builder.Services.AddTransient<OnboardingViewModel>();
		builder.Services.AddTransient<RegisterViewModel>();
		builder.Services.AddTransient<SignInViewModel>();
		builder.Services.AddTransient<ProfileSelectionViewModel>();
		builder.Services.AddTransient<HomeViewModel>();
		builder.Services.AddTransient<PhonicsLevelsViewModel>();
		builder.Services.AddTransient<PhonicsLessonViewModel>();
		builder.Services.AddTransient<MatchingGameViewModel>();
		builder.Services.AddTransient<LessonCompleteViewModel>();
		builder.Services.AddTransient<StoryLibraryViewModel>();
		builder.Services.AddTransient<StoryReaderViewModel>();
		builder.Services.AddTransient<ProgressViewModel>();
		builder.Services.AddTransient<ParentalGateViewModel>();
		builder.Services.AddTransient<ConsentViewModel>();
		builder.Services.AddTransient<ParentDashboardViewModel>();
		builder.Services.AddTransient<SettingsViewModel>();
		builder.Services.AddTransient<RecordSoundsViewModel>();
		builder.Services.AddTransient<ManageStoriesViewModel>();
		builder.Services.AddTransient<StoryEditorViewModel>();

		// Pages are transient so each navigation gets fresh state.
		builder.Services.AddTransient<SplashPage>();
		builder.Services.AddTransient<OnboardingPage>();
		builder.Services.AddTransient<RegisterPage>();
		builder.Services.AddTransient<SignInPage>();
		builder.Services.AddTransient<ProfileSelectionPage>();
		builder.Services.AddTransient<HomePage>();
		builder.Services.AddTransient<StoriesPage>();
		builder.Services.AddTransient<ProgressPage>();
		builder.Services.AddTransient<SettingsPage>();
		builder.Services.AddTransient<PhonicsLevelsPage>();
		builder.Services.AddTransient<PhonicsLessonPage>();
		builder.Services.AddTransient<MatchingGamePage>();
		builder.Services.AddTransient<LessonCompletePage>();
		builder.Services.AddTransient<StoryReaderPage>();
		builder.Services.AddTransient<ParentalGatePage>();
		builder.Services.AddTransient<ParentDashboardPage>();
		builder.Services.AddTransient<ConsentPage>();
		builder.Services.AddTransient<RecordSoundsPage>();
		builder.Services.AddTransient<ManageStoriesPage>();
		builder.Services.AddTransient<StoryEditorPage>();

#if DEBUG
		builder.Services.AddSingleton<ISelfCheck, ProgressSelfCheck>();
		builder.Services.AddSingleton<ISelfCheck, PhonicsEngineSelfCheck>();
		builder.Services.AddSingleton<ISelfCheck, AuthSelfCheck>();
		builder.Services.AddTransient<SelfCheckPage>();
		builder.Logging.AddDebug();
#endif

		// Android draws a Material underline under every EditText, tinted with the
		// platform accent, which is not part of DESIGN.md. Our entries sit inside
		// their own bordered card, so the underline is removed globally.
		Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping(
			"TongaKidsNoUnderline", (handler, view) =>
			{
#if ANDROID
				handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
#endif
			});

		Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping(
			"TongaKidsNoUnderline", (handler, view) =>
			{
#if ANDROID
				handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
#endif
			});

		return builder.Build();
	}
}
