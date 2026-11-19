using System.Windows;

namespace ParameterQc.UI;

public partial class AnalysisChoiceWindow : Window
{
    public AnalysisChoiceWindow()
    {
        InitializeComponent();
        SummaryText.Text = PopulationRules.Summary;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
