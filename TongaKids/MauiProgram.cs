using Microsoft.Extensions.Logging;
using TongaKids.Data;
using TongaKids.Services;
using TongaKids.Services.SelfCheck;
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

		// Data layer. Singletons: one connection, opened once, shared.
		builder.Services.AddSingleton<ITongaKidsDatabase, TongaKidsDatabase>();
		builder.Services.AddSingleton<ILearnerRepository, LearnerRepository>();
		builder.Services.AddSingleton<IContentRepository, ContentRepository>();
		builder.Services.AddSingleton<IProgressRepository, ProgressRepository>();
		builder.Services.AddSingleton<IContentSeeder, ContentSeeder>();

		// Session is a singleton: one learner at a time, app-wide.
		builder.Services.AddSingleton<ILearnerSession, LearnerSession>();

		// Engines. Pure classes with no MAUI or SQLite dependency.
		builder.Services.AddSingleton<IPhonicsEngine, PhonicsEngine>();
		builder.Services.AddSingleton<IProgressCalculator, ProgressCalculator>();
		builder.Services.AddSingleton<IMasteryEvaluator, MasteryEvaluator>();

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
		builder.Services.AddSingleton<ISelfCheck, ProgressSelfCheck>();
		builder.Services.AddSingleton<ISelfCheck, PhonicsEngineSelfCheck>();
		builder.Services.AddTransient<SelfCheckPage>();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
