using MudBlazor;

namespace MatchTFE.Client.Themes
{
    public class MatchTheme
    {
        public static MudTheme DefaultTheme = new MudTheme()
        {
            PaletteLight = new PaletteLight()
            {
                Primary = "#2563eb",
                PrimaryDarken = "#1d4ed8",
                PrimaryLighten = "#eff6ff",
                Background = "#f1f5f9",
                Surface = "#ffffff",
                DrawerBackground = "#ffffff",
                TextPrimary = "#0f172a",
                TextSecondary = "#64748b",
                LinesDefault = "#e2e8f0",
                AppbarBackground = "rgba(0,0,0,0)"
            },
            Typography = new Typography()
            {
                Default = new DefaultTypography()
                {
                    FontFamily = new[] { "Inter", "Helvetica", "Arial", "sans-serif" }
                }
            },
            LayoutProperties = new LayoutProperties()
            {
                DefaultBorderRadius = "12px"
            }
        };
    }
}
