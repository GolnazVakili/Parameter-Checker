using Autodesk.Revit.DB;

namespace ParameterQc.Services;

internal sealed class ViewScan
{
    public ViewScan(string viewName, int modelElementCount, IReadOnlyList<ElementId> elementIds, IReadOnlyList<ParameterOption> parameters)
    {
        ViewName = viewName;
        ModelElementCount = modelElementCount;
        ElementIds = elementIds;
        Parameters = parameters;
    }

    public string ViewName { get; }
    public int ModelElementCount { get; }
    public IReadOnlyList<ElementId> ElementIds { get; }
    public IReadOnlyList<ParameterOption> Parameters { get; }
}

internal static class ViewScanner
{
    public static ViewScan Scan(Document document, View view)
    {
        var ids = new List<ElementId>();
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (Element element in Collect(document, view))
        {
            if (!IsModelElement(element))
                continue;

            ids.Add(element.Id);
            foreach (Parameter parameter in InstanceParameters(element))
            {
                if (!ParameterSlots.Include(parameter))
                    continue;

                string name = parameter.Definition.Name;
                if (!names.ContainsKey(name))
                    names[name] = KnownParameters.GroupFor(name);
            }
        }

        IReadOnlyList<ParameterOption> options = names
            .OrderBy(pair => GroupIndex(pair.Value))
            .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new ParameterOption(pair.Key, pair.Value))
            .ToList();

        return new ViewScan(view.Name, ids.Count, ids, options);
    }

    public static IReadOnlyList<ElementRecord> Read(
        Document document,
        IReadOnlyList<ElementId> elementIds,
        IReadOnlyList<ParameterSelection> selected)
    {
        var wanted = new HashSet<string>(selected.Select(parameter => parameter.Name), StringComparer.OrdinalIgnoreCase);
        bool needsMaximo = selected.Any(parameter => ParameterGroups.IsMaximo(parameter.Group));
        var records = new List<ElementRecord>(elementIds.Count);

        foreach (ElementId id in elementIds)
        {
            Element? element = document.GetElement(id);
            if (!IsModelElement(element))
                continue;

            var slots = new Dictionary<string, SlotKind>(StringComparer.OrdinalIgnoreCase);
            foreach (Parameter parameter in InstanceParameters(element!))
            {
                if (!ParameterSlots.Include(parameter))
                    continue;

                string name = parameter.Definition.Name;
                if (!wanted.Contains(name) || slots.ContainsKey(name))
                    continue;

                slots[name] = ParameterSlots.Read(parameter);
            }

            bool registrable = needsMaximo && IsRegistrable(element);
            records.Add(new ElementRecord(element.Category!.Name, registrable, slots));
        }

        return records;
    }

    private static bool IsRegistrable(Element element)
    {
        foreach (Parameter parameter in InstanceParameters(element))
        {
            if (parameter.Definition == null)
                continue;
            if (!parameter.Definition.Name.Equals("Maximo.Registrable", StringComparison.OrdinalIgnoreCase))
                continue;

            return ValueRules.IsYes(ParameterSlots.Text(parameter));
        }

        return false;
    }

    private static readonly BuiltInParameter[] IfcBuiltIns =
    [
        BuiltInParameter.IFC_EXPORT_ELEMENT,
        BuiltInParameter.IFC_EXPORT_ELEMENT_AS,
        BuiltInParameter.IFC_EXPORT_PREDEFINEDTYPE
    ];

    private static IEnumerable<Parameter> InstanceParameters(Element element)
    {
        var seen = new HashSet<long>();
        var yieldBuffer = new List<Parameter>();

        void Consider(Parameter? parameter)
        {
            if (parameter?.Definition == null || parameter.Id == null)
                return;
            if (parameter.Element is ElementType)
                return;
            if (parameter.Element != null && parameter.Element.Id != element.Id)
                return;
            if (!seen.Add(parameter.Id.Value))
                return;

            yieldBuffer.Add(parameter);
        }

        foreach (Parameter parameter in element.Parameters)
            Consider(parameter);

        try
        {
            foreach (Parameter parameter in element.GetOrderedParameters())
                Consider(parameter);
        }
        catch
        {
            // Some elements do not expose the properties-palette list.
        }

        foreach (BuiltInParameter builtIn in IfcBuiltIns)
        {
            try
            {
                Consider(element.get_Parameter(builtIn));
            }
            catch
            {
                // This element does not carry that built-in parameter.
            }
        }

        return yieldBuffer;
    }

    private static IEnumerable<Element> Collect(Document document, View view)
    {
        return new FilteredElementCollector(document, view.Id)
            .WhereElementIsNotElementType();
    }

    private static bool IsModelElement(Element? element)
    {
        return element?.Category != null
            && element.Category.CategoryType == CategoryType.Model;
    }

    private static int GroupIndex(string group)
    {
        int index = Array.IndexOf(ParameterGroups.DisplayOrder, group);
        return index < 0 ? int.MaxValue : index;
    }
}
