using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace ParameterQc.UI;

public partial class ResultWindow : Window
{
    private readonly PopulationReport _report;
    private readonly string _viewName;

    public ResultWindow(PopulationReport report, string viewName, int elementCount)
    {
        InitializeComponent();
        _report = report;
        _viewName = viewName;
        SubtitleText.Text = $"{viewName} · {elementCount} model elements · Elements with each parameter group complete";
        RulesText.Text = PopulationRules.Summary;
        ChartHost.Content = PopulationChart.Create(report);
        BuildLegend(report);
    }

    private void BuildLegend(PopulationReport report)
    {
        foreach (string group in report.Groups)
            LegendPanel.Children.Add(LegendItem(ParameterGroups.ColorHex(group), group));

        LegendPanel.Children.Add(LegendItem(ParameterGroups.TbdColorHex, "TBD"));
    }

    private static StackPanel LegendItem(string hex, string label)
    {
        var item = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 18, 6)
        };
        item.Children.Add(new Border
        {
            Width = 14,
            Height = 14,
            Margin = new Thickness(0, 0, 6, 0),
            CornerRadius = new CornerRadius(2),
            Background = ChartPalette.Brush(hex),
            VerticalAlignment = VerticalAlignment.Center
        });
        item.Children.Add(new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center
        });
        return item;
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        string safeView = string.Join("_", _viewName.Split(Path.GetInvalidFileNameChars()));
        var dialog = new SaveFileDialog
        {
            Title = "Export Parameter QC",
            Filter = "Excel workbook (*.xlsx)|*.xlsx",
            FileName = $"Parameter QC - {safeView} - {DateTime.Now:yyyy-MM-dd}.xlsx",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            PopulationWorkbook.Save(_report, _viewName, dialog.FileName);
            MessageBox.Show(this, $"Saved {dialog.FileName}", "Parameter QC", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Parameter QC", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
