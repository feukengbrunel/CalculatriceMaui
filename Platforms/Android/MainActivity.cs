using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;

namespace CalculatriceMaui;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Icônes de la barre d'état en clair, lisibles sur le fond sombre de l'application.
        if (Window is not null && WindowCompat.GetInsetsController(Window, Window.DecorView) is { } insets)
            insets.AppearanceLightStatusBars = false;
    }
}
