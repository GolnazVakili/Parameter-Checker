using System.Windows.Media;

namespace ParameterQc.UI;

internal static class ChartPalette
{
    public static SolidColorBrush Brush(string hex)
    {
        byte red = Convert.ToByte(hex[..2], 16);
        byte green = Convert.ToByte(hex.Substring(2, 2), 16);
        byte blue = Convert.ToByte(hex.Substring(4, 2), 16);
        var brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }
}
