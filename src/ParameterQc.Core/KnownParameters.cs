namespace ParameterQc;

public static class KnownParameters
{
    public static string GroupFor(string parameterName)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
            return ParameterGroups.Other;

        return Map.TryGetValue(parameterName.Trim(), out string? group)
            ? group
            : ParameterGroups.Other;
    }

    private static Dictionary<string, string> Build()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Add(map, ParameterGroups.Ifc, Ifc);
        Add(map, ParameterGroups.Pact, Pact);
        Add(map, ParameterGroups.Maximo, Maximo);
        Add(map, ParameterGroups.Classification, Classification);
        Add(map, ParameterGroups.Cobie, Cobie);
        return map;
    }

    private static void Add(Dictionary<string, string> map, string group, IEnumerable<string> names)
    {
        foreach (string name in names)
        {
            if (!map.TryAdd(name, group))
                throw new InvalidOperationException($"Parameter '{name}' is listed in more than one group.");
        }
    }

    private static readonly string[] Ifc =
    [
        "IfcExportAs",
        "IfcEntity",
        "PredefinedType",
        "ObjectType",
        "PropertySets",
        "Export to IFC",
        "Export to IFC As",
        "IfcExportType"
    ];

    private static readonly string[] Pact =
    [
        "Material",
        "Mark",
        "Type",
        "Project ID",
        "Location",
        "Level",
        "UoM",
        "Pipe System",
        "System Type",
        "Discipline",
        "Asset ID",
        "PACT ID",
        "Design Status",
        "Dataset Code",
        "GDB Tag",
        "GDB Asset Number",
        "GDB Status",
        "CCR",
        "Circuit",
        "Feeder Buss",
        "MZ Zone",
        "Voltage (V)",
        "Thickness (mm)",
        "Diameter (mm)",
        "Outer Diameter (mm)",
        "Length (m)",
        "Width (m)",
        "Height (m)",
        "Area (m2)",
        "Area (m²)",
        "Area 3D (m2)",
        "Area 3D (m²)",
        "Volume (m3)",
        "Volume (m³)",
        "Base Level (m)",
        "Top Level (m)",
        "Elevation (m)",
        "Slope (%)"
    ];

    private static readonly string[] Maximo =
    [
        "Maximo.Class",
        "Maximo.Registrable",
        "Maximo.SystemNum",
        "Maximo.Subsystem",
        "Maximo.Location",
        "Maximo.RoomArea"
    ];

    private static readonly string[] Classification =
    [
        "Classification.Omniclass.21.Number",
        "Classification.Omniclass.21.Description",
        "Classification.Omniclass.23.Number",
        "Classification.Omniclass.23.Description",
        "Classification.Uniformat",
        "Classification.MasterFormat"
    ];

    private static readonly string[] Cobie =
    [
        "COBie.Type.Name",
        "COBie.Type.Category",
        "COBie.Type.CreatedBy",
        "COBie.Type.Description",
        "COBie.Type.AssetType",
        "COBie.Type.ExpectedLife",
        "COBie.Type.DurationUnit",
        "COBie.Type.NominalLength",
        "COBie.Type.NominalWidth",
        "COBie.Type.NominalHeight",
        "COBie.Type.Material",
        "COBie.Type.Shape",
        "COBie.Type.Size",
        "COBie.Type.Color",
        "COBie.Type.Finish",
        "COBie.Type.Grade",
        "COBie.Type.Constituents",
        "COBie.Type.Features",
        "COBie.Type.AccessibilityPerformance",
        "COBie.Type.CodePerformance",
        "COBie.Type.SustainabilityPerformance",
        "COBie.Type.WarrantyDurationParts",
        "COBie.Type.WarrantyDurationLabor",
        "COBie.Type.WarrantyDurationUnit",
        "COBie.Type.WarrantyGuarantorParts",
        "COBie.Type.WarrantyGuarantorLabor",
        "COBie.Type.WarrantyDescription",
        "COBie.Type.ReplacementCost",
        "COBie.Type.Manufacturer",
        "COBie.Type.ModelNumber",
        "COBie.Component.Name",
        "COBie.Component.Description",
        "COBie.Component.TypeName",
        "COBie.Component.SpaceName",
        "COBie.Component.AssetIdentifier",
        "COBie.Component.TagNumber",
        "COBie.Component.SerialNumber",
        "COBie.Component.InstallationDate",
        "COBie.Component.WarrantyStartDate",
        "COBie.Component.BarCode",
        "COBie.System.Name",
        "COBie.System.Category",
        "COBie.System.Description",
        "COBie.Floor.Name",
        "COBie.Zone.Name",
        "COBie.Zone.Category",
        "COBie.Space.Name"
    ];

    private static readonly Dictionary<string, string> Map = Build();
}
