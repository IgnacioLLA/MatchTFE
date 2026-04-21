using MudBlazor;

namespace MatchTFE.Client.Themes;
public static class MatchTheme
{
    private static readonly string[] _fontFamily = { "Inter", "Helvetica", "Arial", "sans-serif" };

    public static MudTheme DefaultTheme => new()
    {
        PaletteLight = CreateLightPalette(),
        PaletteDark = CreateDarkPalette(),
        LayoutProperties = CreateLayoutProperties(),
        Typography = CreateTypography()
    };

    private static PaletteLight CreateLightPalette() => new()
    {
        Primary = "#2563eb",
        PrimaryDarken = "#1d4ed8",
        PrimaryLighten = "#dbeafe",
        Secondary = "#7c3aed",
        Tertiary = "#10b981",
        Background = "#f8fafc",
        Surface = "#ffffff",
        DrawerBackground = "#ffffff",
        AppbarBackground = "#ffffff",
        TextPrimary = "#0f172a",
        TextSecondary = "#475569",
        LinesDefault = "#e2e8f0",
        TableLines = "#f1f5f9",
        Divider = "#e2e8f0",
        Success = "#059669",
        Error = "#dc2626",
        Info = "#0ea5e9",
        Warning = "#f59e0b",
        ActionDefault = "#64748b"
    };

    private static PaletteDark CreateDarkPalette() => new()
    {
        Primary = "#3b82f6",
        Secondary = "#a78bfa",
        Surface = "#1e293b",
        Background = "#0f172a",
        BackgroundGray = "#1e293b",
        DrawerBackground = "#1e293b",
        AppbarBackground = "#1e293b",
        TextPrimary = "#f8fafc",
        TextSecondary = "#94a3b8",
        LinesDefault = "#334155",
        Divider = "#334155"
    };

    private static LayoutProperties CreateLayoutProperties() => new()
    {
        DefaultBorderRadius = "8px",
        DrawerWidthLeft = "280px",
        AppbarHeight = "64px"
    };

    private static Typography CreateTypography() => new()
    {
        Default = new DefaultTypography()
        {
            FontFamily = _fontFamily,
            FontSize = ".875rem",
            LineHeight = "1.5",
            LetterSpacing = "0"
        },
        H1 = new H1Typography() { FontSize = "2.25rem", FontWeight = "800", LetterSpacing = "-.02em" },
        H2 = new H2Typography() { FontSize = "1.875rem", FontWeight = "700", LetterSpacing = "-.02em" },
        H3 = new H3Typography() { FontSize = "1.5rem", FontWeight = "700" },
        H4 = new H4Typography() { FontSize = "1.25rem", FontWeight = "600" },
        H5 = new H5Typography() { FontSize = "1.125rem", FontWeight = "600" },
        H6 = new H6Typography() { FontSize = "1rem", FontWeight = "600" },
        Button = new ButtonTypography()
        {
            TextTransform = "none",
            FontWeight = "600",
            FontSize = ".875rem"
        },
        Body1 = new Body1Typography() { FontSize = "1rem" },
        Body2 = new Body2Typography() { FontSize = ".875rem" },
        Caption = new CaptionTypography() { FontSize = ".75rem" }
    };
}