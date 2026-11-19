using Autodesk.Revit.DB;

namespace ParameterQc.Services;

internal static class ParameterSlots
{
    public static bool Include(Parameter parameter)
    {
        Definition? definition = parameter.Definition;
        if (definition == null || string.IsNullOrWhiteSpace(definition.Name))
            return false;

        if (definition is InternalDefinition internalDefinition && !internalDefinition.Visible)
        {
            bool shared = parameter.IsShared;
            bool known = KnownParameters.GroupFor(definition.Name) != ParameterGroups.Other
                || GroupDefaults.Load().ContainsKey(definition.Name);
            if (!shared && !known)
                return false;
        }

        return parameter.StorageType != StorageType.None;
    }

    public static SlotKind Read(Parameter? parameter)
    {
        if (parameter == null || parameter.StorageType == StorageType.None)
            return SlotKind.Empty;

        bool yesNo = IsYesNo(parameter);
        switch (parameter.StorageType)
        {
            case StorageType.String:
                return yesNo ? SlotKind.Filled : ValueRules.ClassifyText(parameter.AsString());
            case StorageType.Integer:
                return ValueRules.ClassifyInteger(parameter.AsInteger(), yesNo);
            case StorageType.Double:
                return ValueRules.ClassifyDouble(parameter.AsDouble(), yesNo);
            case StorageType.ElementId:
                ElementId id = parameter.AsElementId();
                return id == ElementId.InvalidElementId ? SlotKind.Empty : SlotKind.Filled;
            default:
                return SlotKind.Empty;
        }
    }

    public static string? Text(Parameter parameter)
    {
        if (parameter.StorageType == StorageType.String)
        {
            string? raw = parameter.AsString();
            return string.IsNullOrWhiteSpace(raw) ? parameter.AsValueString() : raw;
        }

        return parameter.AsValueString();
    }

    private static bool IsYesNo(Parameter parameter)
    {
        try
        {
            return parameter.Definition.GetDataType() == SpecTypeId.Boolean.YesNo;
        }
        catch
        {
            return false;
        }
    }
}
