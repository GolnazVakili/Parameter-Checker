namespace ParameterQc;

public sealed record ParameterOption(string Name, string Group);

public sealed record ParameterSelection(string Name, string Group);

public sealed record ElementRecord(
    string Category,
    bool MaximoRegistrable,
    IReadOnlyDictionary<string, SlotKind> Slots);

public sealed class GroupTally
{
    public GroupTally(int filled, int empty, int tbd, int excluded, int elements, int completeElements)
    {
        Filled = filled;
        Empty = empty;
        Tbd = tbd;
        Excluded = excluded;
        Elements = elements;
        CompleteElements = completeElements;
    }

    public int Filled { get; }
    public int Empty { get; }
    public int Tbd { get; }
    public int Excluded { get; }
    public int Elements { get; }
    public int CompleteElements { get; }

    public int Counted => Filled + Empty + Tbd;

    public bool HasCount => Counted > 0;

    public bool HasElements => Elements > 0;

    public double FilledPercent => HasCount ? 100d * Filled / Counted : 0d;

    public double TbdPercent => HasCount ? 100d * Tbd / Counted : 0d;

    public double CompletePercent => HasElements ? 100d * CompleteElements / Elements : 0d;

    public string Fraction => HasCount ? $"{Filled}/{Counted}" : "—";

    public string CompleteFraction => HasElements ? $"{CompleteElements}/{Elements}" : "—";
}

public sealed class CategoryRow
{
    public CategoryRow(string category, IReadOnlyDictionary<string, GroupTally> byGroup)
    {
        Category = category;
        ByGroup = byGroup;
    }

    public string Category { get; }
    public IReadOnlyDictionary<string, GroupTally> ByGroup { get; }
}

public sealed class PopulationReport
{
    public PopulationReport(
        IReadOnlyList<ParameterSelection> parameters,
        IReadOnlyList<string> groups,
        IReadOnlyList<CategoryRow> categories,
        CategoryRow total)
    {
        Parameters = parameters;
        Groups = groups;
        Categories = categories;
        Total = total;
    }

    public IReadOnlyList<ParameterSelection> Parameters { get; }
    public IReadOnlyList<string> Groups { get; }
    public IReadOnlyList<CategoryRow> Categories { get; }
    public CategoryRow Total { get; }
}

public static class ReportBuilder
{
    public static PopulationReport Build(
        IEnumerable<ElementRecord> elements,
        IReadOnlyList<ParameterSelection> parameters)
    {
        string[] groups = ParameterGroups.DisplayOrder
            .Where(group => parameters.Any(parameter => parameter.Group == group))
            .ToArray();

        var selectedByGroup = groups.ToDictionary(
            group => group,
            group => parameters.Where(parameter => parameter.Group == group).Select(parameter => parameter.Name).ToArray());

        var buckets = new Dictionary<string, Dictionary<string, MutableTally>>(StringComparer.OrdinalIgnoreCase);

        foreach (ElementRecord element in elements)
        {
            if (string.IsNullOrWhiteSpace(element.Category))
                continue;

            if (!buckets.TryGetValue(element.Category, out Dictionary<string, MutableTally>? row))
            {
                row = groups.ToDictionary(group => group, _ => new MutableTally());
                buckets[element.Category] = row;
            }

            foreach (string group in groups)
            {
                if (ParameterGroups.IsMaximo(group) && !element.MaximoRegistrable)
                    continue;

                MutableTally tally = row[group];
                bool complete = selectedByGroup[group].Length > 0;
                foreach (string name in selectedByGroup[group])
                {
                    SlotKind kind = element.Slots.TryGetValue(name, out SlotKind found)
                        ? found
                        : SlotKind.Empty;
                    tally.Add(kind);
                    if (kind != SlotKind.Filled)
                        complete = false;
                }

                tally.CountElement(complete);
            }
        }

        var categories = buckets
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new CategoryRow(pair.Key, Freeze(pair.Value, groups)))
            .ToList();

        var totalBuckets = groups.ToDictionary(group => group, _ => new MutableTally());
        foreach (CategoryRow category in categories)
        {
            foreach (string group in groups)
                totalBuckets[group].Add(category.ByGroup[group]);
        }

        return new PopulationReport(
            parameters,
            groups,
            categories,
            new CategoryRow("Total", Freeze(totalBuckets, groups)));
    }

    private static Dictionary<string, GroupTally> Freeze(
        Dictionary<string, MutableTally> source,
        IReadOnlyList<string> groups)
    {
        return groups.ToDictionary(group => group, group => source[group].ToTally());
    }

    private sealed class MutableTally
    {
        public int Filled { get; private set; }
        public int Empty { get; private set; }
        public int Tbd { get; private set; }
        public int Excluded { get; private set; }
        public int Elements { get; private set; }
        public int CompleteElements { get; private set; }

        public void CountElement(bool complete)
        {
            Elements++;
            if (complete)
                CompleteElements++;
        }

        public void Add(SlotKind kind)
        {
            switch (kind)
            {
                case SlotKind.Filled:
                    Filled++;
                    break;
                case SlotKind.Empty:
                    Empty++;
                    break;
                case SlotKind.Tbd:
                    Tbd++;
                    break;
                case SlotKind.Excluded:
                    Excluded++;
                    break;
            }
        }

        public void Add(GroupTally tally)
        {
            Filled += tally.Filled;
            Empty += tally.Empty;
            Tbd += tally.Tbd;
            Excluded += tally.Excluded;
            Elements += tally.Elements;
            CompleteElements += tally.CompleteElements;
        }

        public GroupTally ToTally() => new(Filled, Empty, Tbd, Excluded, Elements, CompleteElements);
    }
}
