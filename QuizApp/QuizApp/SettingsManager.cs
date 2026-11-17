using System.Text.Json;

namespace QuizApp;

internal static class SettingsManager
{
    private const float MinimumFontSize = 9F;
    private const float MaximumFontSize = 24F;
    private const float FontSizeStep = 1F;

    public const int MinimumImageZoom = 50;
    public const int MaximumImageZoom = 400;
    public const int ImageZoomStep = 10;

    private static readonly string SettingsFilePath =
        Path.Combine(AppContext.BaseDirectory, "settings.json");

    private sealed class AppSettingsData
    {
        public float FontSize { get; set; } = 12F;
        public int ImageZoomPercent { get; set; } = 100;
        public double SplitterRatio { get; set; } = 0.25;
    }

    public static float GlobalFontSize { get; private set; } = 12F;
    public static int ImageZoomPercent { get; private set; } = 100;
    public static double SplitterRatio { get; private set; } = 0.25;

    public static event EventHandler? FontSizeChanged;
    public static event EventHandler? ImageZoomChanged;
    public static event EventHandler? SplitterRatioChanged;

    static SettingsManager()
    {
        LoadSettings();
    }

    public static void ZoomIn() => SetFontSize(GlobalFontSize + FontSizeStep);
    public static void ZoomOut() => SetFontSize(GlobalFontSize - FontSizeStep);

    public static void SetFontSize(float value)
    {
        var next = Math.Clamp(value, MinimumFontSize, MaximumFontSize);
        if (Math.Abs(next - GlobalFontSize) < 0.01F)
            return;

        GlobalFontSize = next;
        SaveSettings();
        FontSizeChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void ZoomImageIn() => SetImageZoom(ImageZoomPercent + ImageZoomStep);
    public static void ZoomImageOut() => SetImageZoom(ImageZoomPercent - ImageZoomStep);

    public static void SetImageZoom(int value)
    {
        var next = Math.Clamp(value, MinimumImageZoom, MaximumImageZoom);
        if (next == ImageZoomPercent)
            return;

        ImageZoomPercent = next;
        SaveSettings();
        ImageZoomChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void SetSplitterRatio(double value)
    {
        var next = Math.Clamp(value, 0.10, 0.70);
        if (Math.Abs(next - SplitterRatio) < 0.005)
            return;

        SplitterRatio = next;
        SaveSettings();
        SplitterRatioChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                var data = JsonSerializer.Deserialize<AppSettingsData>(json);
                if (data is not null)
                {
                    GlobalFontSize = Math.Clamp(data.FontSize, MinimumFontSize, MaximumFontSize);
                    ImageZoomPercent = Math.Clamp(data.ImageZoomPercent, MinimumImageZoom, MaximumImageZoom);
                    SplitterRatio = Math.Clamp(data.SplitterRatio, 0.10, 0.70);
                }
            }
        }
        catch
        {
            // Ignore load error and keep defaults
        }
    }

    public static void SaveSettings()
    {
        try
        {
            var data = new AppSettingsData
            {
                FontSize = GlobalFontSize,
                ImageZoomPercent = ImageZoomPercent,
                SplitterRatio = SplitterRatio
            };
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch
        {
            // Ignore save error
        }
    }

    public static void ApplyFont(Control control)
    {
        control.Font = new Font(control.Font.FontFamily, GlobalFontSize, control.Font.Style);
        if (control is DataGridView grid)
        {
            grid.DefaultCellStyle.Font = control.Font;
            grid.ColumnHeadersDefaultCellStyle.Font = control.Font;
            grid.RowTemplate.DefaultCellStyle.Font = control.Font;
        }
        foreach (Control child in control.Controls)
            ApplyFont(child);
    }
}
