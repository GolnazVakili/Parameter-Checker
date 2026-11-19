using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ParameterQc.UI;

internal static class PopulationChart
{
    public static FrameworkElement Create(PopulationReport report)
    {
        const double plotHeight = 320;
        const double axisWidth = 44;
        const double barWidth = 14;
        const double barGap = 10;
        const double categoryGap = 28;
        const double valueFont = 11;
        const double groupFont = 5.5;

        IReadOnlyList<string> groups = report.Groups;
        int groupCount = Math.Max(groups.Count, 1);
        double barsWidth = groupCount * barWidth + Math.Max(0, groupCount - 1) * barGap;
        double columnWidth = Math.Max(barsWidth + 16, 72);
        double fractionBand = MeasureVertical("100000/100000  100%", valueFont) + 8;
        double groupBand = groups.Count == 0 ? 16 : groups.Max(group => MeasureVertical(group, groupFont)) + 8;
        const double categoryBand = 44;

        double plotTop = fractionBand;
        double plotBottom = plotTop + plotHeight;
        double canvasHeight = plotBottom + 8 + groupBand + categoryBand;
        double canvasWidth = 12 + Math.Max(1, report.Categories.Count) * (columnWidth + categoryGap);

        var gridBrush = Frozen(Color.FromRgb(0xE4, 0xE9, 0xF0));
        var zeroBrush = Frozen(Color.FromRgb(0xB7, 0xC0, 0xCC));
        var tickBrush = Frozen(Color.FromRgb(0x33, 0x33, 0x33));
        var groupBrushes = groups.ToDictionary(group => group, group => ChartPalette.Brush(ParameterGroups.ColorHex(group)));

        var axis = new Canvas
        {
            Width = axisWidth,
            Height = canvasHeight,
            Background = Brushes.White
        };
        var plot = new Canvas
        {
            Width = canvasWidth,
            Height = canvasHeight,
            Background = Brushes.White,
            SnapsToDevicePixels = true
        };

        for (int percent = 0; percent <= 100; percent += 10)
        {
            double y = plotBottom - plotHeight * percent / 100d;
            plot.Children.Add(new Line
            {
                X1 = 0,
                X2 = canvasWidth,
                Y1 = y,
                Y2 = y,
                Stroke = percent == 0 ? zeroBrush : gridBrush,
                StrokeThickness = 1
            });

            var tick = new TextBlock
            {
                Text = percent.ToString(CultureInfo.CurrentCulture),
                FontSize = 11,
                Foreground = tickBrush,
                Width = axisWidth - 10,
                TextAlignment = TextAlignment.Right
            };
            Canvas.SetLeft(tick, 0);
            Canvas.SetTop(tick, y - 8);
            axis.Children.Add(tick);
        }

        for (int categoryIndex = 0; categoryIndex < report.Categories.Count; categoryIndex++)
        {
            CategoryRow category = report.Categories[categoryIndex];
            double columnLeft = 8 + categoryIndex * (columnWidth + categoryGap);
            double barsLeft = columnLeft + (columnWidth - barsWidth) / 2;

            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                string group = groups[groupIndex];
                GroupTally tally = category.ByGroup[group];
                double x = barsLeft + groupIndex * (barWidth + barGap);
                double barTop = DrawBar(plot, tally, x, plotTop, plotBottom, plotHeight, barWidth, groupBrushes[group], group);
                PlaceVertical(plot, BarLabel(tally), x + barWidth / 2, barTop - 1, valueFont, Brushes.Black, alignBottom: true);
                PlaceVertical(plot, group, x + barWidth / 2, plotBottom + 6, groupFont, Brushes.Black, alignBottom: false);
            }

            var categoryName = new TextBlock
            {
                Text = category.Category,
                Width = columnWidth,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.Black
            };
            Canvas.SetLeft(categoryName, columnLeft);
            Canvas.SetTop(categoryName, plotBottom + 8 + groupBand);
            plot.Children.Add(categoryName);
        }

        var axisScroll = new ScrollViewer
        {
            Content = axis,
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        var plotScroll = new ScrollViewer
        {
            Content = plot,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = Brushes.White
        };
        plotScroll.ScrollChanged += (_, _) =>
        {
            if (Math.Abs(axisScroll.VerticalOffset - plotScroll.VerticalOffset) > 0.5)
                axisScroll.ScrollToVerticalOffset(plotScroll.VerticalOffset);
        };

        var root = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = Brushes.White
        };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(axisWidth) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(axisScroll, 0);
        Grid.SetColumn(plotScroll, 1);
        root.Children.Add(axisScroll);
        root.Children.Add(plotScroll);
        return root;
    }

    private static string BarLabel(GroupTally tally)
    {
        if (!tally.HasElements)
            return "—";

        string percent = tally.CompletePercent.ToString("0.#", CultureInfo.CurrentCulture);
        return $"{tally.CompleteFraction}  {percent}%";
    }

    private static double DrawBar(
        Canvas canvas,
        GroupTally tally,
        double x,
        double plotTop,
        double plotBottom,
        double plotHeight,
        double barWidth,
        Brush groupBrush,
        string group)
    {
        string tip = tally.HasElements
            ? $"{group}\n{tally.CompleteElements} of {tally.Elements} elements complete\nValues filled {tally.Fraction}"
            : $"{group}\nNo elements counted";

        var hit = new Rectangle
        {
            Width = barWidth,
            Height = plotHeight,
            Fill = Brushes.Transparent,
            ToolTip = tip
        };
        Canvas.SetLeft(hit, x);
        Canvas.SetTop(hit, plotTop);
        canvas.Children.Add(hit);

        double filledHeight = tally.HasElements ? plotHeight * tally.CompletePercent / 100d : 0;
        if (filledHeight < 1)
        {
            var baseline = new Rectangle
            {
                Width = barWidth,
                Height = 2,
                Fill = groupBrush,
                ToolTip = tip
            };
            Canvas.SetLeft(baseline, x);
            Canvas.SetTop(baseline, plotBottom - 2);
            canvas.Children.Add(baseline);
            return plotBottom - 2;
        }

        if (filledHeight > 0)
        {
            var filled = new Rectangle
            {
                Width = barWidth,
                Height = Math.Max(filledHeight, 1),
                Fill = groupBrush,
                ToolTip = tip
            };
            Canvas.SetLeft(filled, x);
            Canvas.SetTop(filled, plotBottom - filledHeight);
            canvas.Children.Add(filled);
        }

        return plotBottom - filledHeight;
    }

    private static void PlaceVertical(
        Canvas canvas,
        string text,
        double centerX,
        double anchorY,
        double fontSize,
        Brush foreground,
        bool alignBottom)
    {
        var block = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            Foreground = foreground,
            LayoutTransform = new RotateTransform(90)
        };
        block.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Canvas.SetLeft(block, centerX - block.DesiredSize.Width / 2);
        Canvas.SetTop(block, alignBottom ? anchorY - block.DesiredSize.Height : anchorY);
        canvas.Children.Add(block);
    }

    private static double MeasureVertical(string text, double fontSize)
    {
        var block = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            LayoutTransform = new RotateTransform(90)
        };
        block.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        return block.DesiredSize.Height;
    }

    private static SolidColorBrush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
