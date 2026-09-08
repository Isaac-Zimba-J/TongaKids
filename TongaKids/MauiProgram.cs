using Microsoft.Extensions.Logging;

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

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
