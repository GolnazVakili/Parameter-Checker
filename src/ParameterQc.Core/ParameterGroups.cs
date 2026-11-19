namespace ParameterQc;

public static class ParameterGroups
{
    public const string Ifc = "GTAA_IFC";
    public const string Pact = "GTAA_PACT";
    public const string PactDimension = "GTAA-PACT(dimension)";
    public const string Maximo = "GTAA_Cobi(Maximo)";
    public const string Classification = "GTAA_COBie(Classification)";
    public const string Cobie = "GTAA_COBie";
    public const string Other = "Other";

    public static readonly string[] Assignable =
    [
        Ifc,
        Pact,
        PactDimension,
        Maximo,
        Classification,
        Cobie
    ];

    public static readonly string[] DisplayOrder =
    [
        Ifc,
        Pact,
        PactDimension,
        Maximo,
        Classification,
        Cobie,
        Other
    ];

    public static bool IsAssignable(string group) => Array.IndexOf(Assignable, group) >= 0;

    public static bool IsMaximo(string group) => group == Maximo;

    public static string ColorHex(string group) => group switch
    {
        Ifc => "1F4E79",
        Pact => "2E7D4F",
        PactDimension => "34657F",
        Maximo => "C65911",
        Classification => "7030A0",
        Cobie => "0D7377",
        _ => "5B6770"
    };

    public const string TbdColorHex = "E6A317";
}

public static class PopulationRules
{
    public const string Summary =
        "Each bar is the percentage of elements whose selected parameters in that group are all filled. The number on the bar is complete elements over elements in that category. 0 is empty. n/a, blank, and TBD are not complete. Yes and No count as filled. Maximo counts elements whose Maximo.Registrable is YES.";
}
