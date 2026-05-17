using CalorieLens.Services;
using CalorieLens.ViewModels;
using CalorieLens.Views;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace CalorieLens
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddTransient<AuthViewModel>();
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<CameraPage>();
            builder.Services.AddTransient<FoodResultPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}