using System.Windows;
using System.Windows.Controls;

namespace ParameterQc.UI;

public partial class ParameterPickerWindow : Window
{
    private const string Unassigned = "(not in a group)";

    private readonly List<ParameterChoice> _choices = [];
    private readonly List<TabSection> _tabs = [];
    private bool _updating;

    public ParameterPickerWindow(string viewName, int elementCount, IReadOnlyList<ParameterOption> parameters)
    {
        InitializeComponent();
        ScopeText.Text = $"{viewName}. {elementCount} model elements. All lists every parameter. Put each one in a group, then Save as default.";
        Build(parameters);
        UpdateCount();
    }

    public IReadOnlyList<ParameterSelection> Selection { get; private set; } = [];

    private void Build(IReadOnlyList<ParameterOption> parameters)
    {
        foreach (ParameterOption option in parameters.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (_choices.Any(choice => choice.Name.Equals(option.Name, StringComparison.OrdinalIgnoreCase)))
                continue;

            string group = ParameterGroups.IsAssignable(option.Group) ? option.Group : ParameterGroups.Other;
            _choices.Add(new ParameterChoice(option.Name, group));
        }

        AddTab("All", _choices);
        foreach (string group in ParameterGroups.Assignable)
            AddTab(group, _choices.Where(choice => choice.Group == group).ToList());
    }

    private void AddTab(string name, IReadOnlyList<ParameterChoice> choices)
    {
        var toggle = new CheckBox
        {
            Content = name,
            FontWeight = FontWeights.SemiBold,
            IsThreeState = true,
            Margin = new Thickness(4, 4, 4, 8)
        };
        var rows = new StackPanel();
        var section = new TabSection(name, toggle, rows);
        toggle.Click += (_, _) =>
        {
            if (_updating)
                return;

            bool select = section.Rows.Any(row => row.Container.Visibility == Visibility.Visible && !row.Choice.IsSelected);
            SetRows(section.Rows, select);
        };

        foreach (ParameterChoice choice in choices)
            section.Rows.Add(AddRow(section, choice));

        var panel = new StackPanel { Margin = new Thickness(8) };
        panel.Children.Add(toggle);
        panel.Children.Add(rows);

        var tab = new TabItem
        {
            Header = HeaderText(name, section.Rows.Count),
            Content = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = panel
            }
        };
        section.Tab = tab;
        ParameterTabs.Items.Add(tab);
        _tabs.Add(section);
        RefreshToggle(section);
    }

    private ParameterRow AddRow(TabSection section, ParameterChoice choice)
    {
        var box = new CheckBox
        {
            Content = choice.Name,
            VerticalAlignment = VerticalAlignment.Center,
            Tag = choice
        };
        box.IsChecked = choice.IsSelected;
        box.Checked += (_, _) => SetChoice(choice, true);
        box.Unchecked += (_, _) => SetChoice(choice, false);

        var combo = new ComboBox
        {
            Width = 250,
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        combo.Items.Add(Unassigned);
        foreach (string group in ParameterGroups.Assignable)
            combo.Items.Add(group);

        _updating = true;
        combo.SelectedItem = ParameterGroups.IsAssignable(choice.Group) ? choice.Group : Unassigned;
        _updating = false;
        combo.SelectionChanged += (_, _) => OnGroupChanged(choice, combo);

        var grid = new Grid { Margin = new Thickness(4, 2, 4, 2) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(combo, 1);
        grid.Children.Add(box);
        grid.Children.Add(combo);
        section.RowsPanel.Children.Add(grid);

        var row = new ParameterRow(section, choice, box, combo, grid);
        choice.Rows.Add(row);
        return row;
    }

    private void OnGroupChanged(ParameterChoice choice, ComboBox source)
    {
        if (_updating)
            return;

        string picked = source.SelectedItem as string ?? Unassigned;
        string group = picked == Unassigned ? ParameterGroups.Other : picked;
        if (choice.Group == group)
            return;

        choice.Group = group;
        _updating = true;
        foreach (ParameterRow row in choice.Rows)
            row.GroupList.SelectedItem = ParameterGroups.IsAssignable(group) ? group : Unassigned;
        _updating = false;

        foreach (ParameterRow row in choice.Rows.Where(row => row.Section.Name != "All").ToList())
        {
            row.Section.RowsPanel.Children.Remove(row.Container);
            row.Section.Rows.Remove(row);
            choice.Rows.Remove(row);
        }

        if (ParameterGroups.IsAssignable(group))
        {
            TabSection section = _tabs.First(tab => tab.Name == group);
            ParameterRow row = AddRow(section, choice);
            int index = section.Rows.FindIndex(existing =>
                string.Compare(existing.Choice.Name, choice.Name, StringComparison.OrdinalIgnoreCase) > 0);
            if (index < 0)
            {
                section.Rows.Add(row);
            }
            else
            {
                section.RowsPanel.Children.Remove(row.Container);
                section.RowsPanel.Children.Insert(index, row.Container);
                section.Rows.Insert(index, row);
            }
        }

        ApplySearch();
        RefreshToggles();
    }

    private void SetChoice(ParameterChoice choice, bool selected)
    {
        if (_updating)
            return;

        _updating = true;
        choice.IsSelected = selected;
        foreach (ParameterRow row in choice.Rows)
            row.Box.IsChecked = selected;
        _updating = false;
        RefreshToggles();
        UpdateCount();
    }

    private void SetRows(IEnumerable<ParameterRow> rows, bool selected)
    {
        if (_updating)
            return;

        _updating = true;
        foreach (ParameterChoice choice in rows
            .Where(row => row.Container.Visibility == Visibility.Visible)
            .Select(row => row.Choice)
            .Distinct())
        {
            choice.IsSelected = selected;
            foreach (ParameterRow row in choice.Rows)
                row.Box.IsChecked = selected;
        }

        _updating = false;
        RefreshToggles();
        UpdateCount();
    }

    private void RefreshToggles()
    {
        foreach (TabSection section in _tabs)
            RefreshToggle(section);
    }

    private void RefreshToggle(TabSection section)
    {
        ParameterRow[] visible = section.Rows.Where(row => row.Container.Visibility == Visibility.Visible).ToArray();
        bool all = visible.Length > 0 && visible.All(row => row.Choice.IsSelected);
        bool any = visible.Any(row => row.Choice.IsSelected);

        _updating = true;
        section.Toggle.IsChecked = all ? true : any ? null : false;
        section.Tab.Header = HeaderText(section.Name, visible.Length);
        _updating = false;
    }

    private void UpdateCount()
    {
        int count = _choices.Count(choice => choice.IsSelected);
        SelectionCountText.Text = count == 1 ? "1 parameter selected" : $"{count} parameters selected";
        OkButton.IsEnabled = count > 0;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplySearch();

    private void ApplySearch()
    {
        string query = SearchBox.Text.Trim();
        foreach (ParameterChoice choice in _choices)
        {
            bool match = query.Length == 0
                || choice.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
            foreach (ParameterRow row in choice.Rows)
                row.Container.Visibility = match ? Visibility.Visible : Visibility.Collapsed;
        }

        RefreshToggles();
    }

    private void SelectShown_Click(object sender, RoutedEventArgs e)
    {
        SetRows(_tabs.SelectMany(section => section.Rows), true);
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        _updating = true;
        foreach (ParameterChoice choice in _choices)
        {
            choice.IsSelected = false;
            foreach (ParameterRow row in choice.Rows)
                row.Box.IsChecked = false;
        }

        foreach (TabSection section in _tabs)
            section.Toggle.IsChecked = false;

        _updating = false;
        UpdateCount();
    }

    private void SaveDefault_Click(object sender, RoutedEventArgs e)
    {
        GroupDefaults.Save(_choices.Select(choice => new ParameterSelection(choice.Name, choice.Group)));
        int assigned = _choices.Count(choice => ParameterGroups.IsAssignable(choice.Group));
        SelectionCountText.Text = $"Saved {assigned} parameter assignments as the default.";
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Selection = _choices
            .Where(choice => choice.IsSelected)
            .Select(choice => new ParameterSelection(choice.Name, choice.Group))
            .ToList();
        DialogResult = true;
    }

    private static string HeaderText(string name, int count) => $"{name} ({count})";

    private sealed class ParameterChoice
    {
        public ParameterChoice(string name, string group)
        {
            Name = name;
            Group = group;
        }

        public string Name { get; }
        public string Group { get; set; }
        public bool IsSelected { get; set; }
        public List<ParameterRow> Rows { get; } = [];
    }

    private sealed class ParameterRow
    {
        public ParameterRow(TabSection section, ParameterChoice choice, CheckBox box, ComboBox groupList, FrameworkElement container)
        {
            Section = section;
            Choice = choice;
            Box = box;
            GroupList = groupList;
            Container = container;
        }

        public TabSection Section { get; }
        public ParameterChoice Choice { get; }
        public CheckBox Box { get; }
        public ComboBox GroupList { get; }
        public FrameworkElement Container { get; }
    }

    private sealed class TabSection
    {
        public TabSection(string name, CheckBox toggle, StackPanel rowsPanel)
        {
            Name = name;
            Toggle = toggle;
            RowsPanel = rowsPanel;
        }

        public string Name { get; }
        public CheckBox Toggle { get; }
        public StackPanel RowsPanel { get; }
        public TabItem Tab { get; set; } = null!;
        public List<ParameterRow> Rows { get; } = [];
    }
}
