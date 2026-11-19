using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ParameterQc.UI;

public partial class ParameterPickerWindow : Window
{
    private readonly List<GroupSection> _groups = [];
    private bool _updating;

    public ParameterPickerWindow(string viewName, int elementCount, IReadOnlyList<ParameterOption> parameters)
    {
        InitializeComponent();
        ScopeText.Text = $"{viewName}. {elementCount} model elements. A group becomes one bar for each model category.";
        Build(parameters);
        UpdateCount();
    }

    public IReadOnlyList<ParameterSelection> Selection { get; private set; } = [];

    private void Build(IReadOnlyList<ParameterOption> parameters)
    {
        foreach (IGrouping<string, ParameterOption> group in parameters.GroupBy(parameter => parameter.Group))
        {
            var section = new GroupSection(group.Key);
            var header = new Grid { Margin = new Thickness(0, 8, 0, 4) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var swatch = new Border
            {
                Width = 14,
                Height = 14,
                Background = ChartPalette.Brush(ParameterGroups.ColorHex(group.Key)),
                VerticalAlignment = VerticalAlignment.Center,
                CornerRadius = new CornerRadius(2)
            };
            Grid.SetColumn(swatch, 0);
            header.Children.Add(swatch);

            var toggle = new CheckBox
            {
                Content = group.Key,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                IsThreeState = true
            };
            toggle.Click += (_, _) =>
            {
                if (_updating)
                    return;

                bool select = section.Rows.Any(row => row.Box.Visibility == Visibility.Visible && !row.Choice.IsSelected);
                SetGroup(section, select);
            };
            Grid.SetColumn(toggle, 2);
            header.Children.Add(toggle);
            section.Toggle = toggle;

            var rows = new StackPanel { Margin = new Thickness(22, 0, 0, 4) };
            foreach (ParameterOption option in group.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
            {
                var choice = new ParameterChoice(option.Name, option.Group);
                var box = new CheckBox
                {
                    Content = option.Name,
                    Margin = new Thickness(0, 2, 0, 2),
                    Tag = choice
                };
                box.Checked += (_, _) => OnRowChanged(section, choice, true);
                box.Unchecked += (_, _) => OnRowChanged(section, choice, false);
                rows.Children.Add(box);
                section.Rows.Add(new ParameterRow(choice, box));
            }

            GroupsPanel.Children.Add(header);
            GroupsPanel.Children.Add(rows);
            section.Header = header;
            section.RowsPanel = rows;
            _groups.Add(section);
        }
    }

    private void OnRowChanged(GroupSection section, ParameterChoice choice, bool selected)
    {
        if (_updating)
            return;

        choice.IsSelected = selected;
        RefreshGroup(section);
        UpdateCount();
    }

    private void SetGroup(GroupSection section, bool selected)
    {
        if (_updating)
            return;

        _updating = true;
        foreach (ParameterRow row in section.Rows.Where(row => row.Box.Visibility == Visibility.Visible))
        {
            row.Choice.IsSelected = selected;
            row.Box.IsChecked = selected;
        }

        _updating = false;
        RefreshGroup(section);
        UpdateCount();
    }

    private void RefreshGroup(GroupSection section)
    {
        ParameterRow[] visible = section.Rows.Where(row => row.Box.Visibility == Visibility.Visible).ToArray();
        bool all = visible.Length > 0 && visible.All(row => row.Choice.IsSelected);
        bool any = visible.Any(row => row.Choice.IsSelected);

        _updating = true;
        section.Toggle.IsChecked = all ? true : any ? null : false;
        _updating = false;
    }

    private void UpdateCount()
    {
        int count = _groups.Sum(group => group.Rows.Count(row => row.Choice.IsSelected));
        SelectionCountText.Text = count == 1 ? "1 parameter selected" : $"{count} parameters selected";
        OkButton.IsEnabled = count > 0;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        string query = SearchBox.Text.Trim();
        foreach (GroupSection section in _groups)
        {
            bool any = false;
            foreach (ParameterRow row in section.Rows)
            {
                bool match = query.Length == 0
                    || row.Choice.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
                row.Box.Visibility = match ? Visibility.Visible : Visibility.Collapsed;
                any |= match;
            }

            section.Header.Visibility = any ? Visibility.Visible : Visibility.Collapsed;
            section.RowsPanel.Visibility = any ? Visibility.Visible : Visibility.Collapsed;
            RefreshGroup(section);
        }
    }

    private void SelectShown_Click(object sender, RoutedEventArgs e)
    {
        foreach (GroupSection section in _groups.Where(group => group.Header.Visibility == Visibility.Visible))
            SetGroup(section, true);
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        _updating = true;
        foreach (GroupSection section in _groups)
        {
            foreach (ParameterRow row in section.Rows)
            {
                row.Choice.IsSelected = false;
                row.Box.IsChecked = false;
            }

            section.Toggle.IsChecked = false;
        }

        _updating = false;
        UpdateCount();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Selection = _groups
            .SelectMany(group => group.Rows)
            .Where(row => row.Choice.IsSelected)
            .Select(row => new ParameterSelection(row.Choice.Name, row.Choice.Group))
            .ToList();
        DialogResult = true;
    }

    private sealed class ParameterChoice
    {
        public ParameterChoice(string name, string group)
        {
            Name = name;
            Group = group;
        }

        public string Name { get; }
        public string Group { get; }
        public bool IsSelected { get; set; }
    }

    private sealed class ParameterRow
    {
        public ParameterRow(ParameterChoice choice, CheckBox box)
        {
            Choice = choice;
            Box = box;
        }

        public ParameterChoice Choice { get; }
        public CheckBox Box { get; }
    }

    private sealed class GroupSection
    {
        public GroupSection(string name) => Name = name;

        public string Name { get; }
        public CheckBox Toggle { get; set; } = null!;
        public FrameworkElement Header { get; set; } = null!;
        public StackPanel RowsPanel { get; set; } = null!;
        public List<ParameterRow> Rows { get; } = [];
    }
}
