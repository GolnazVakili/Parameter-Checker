# Parameter QC

Revit 2025 add-in. The **Parameter QC** button is on the **Automation** tab.

It checks instance parameters on the model elements visible in the active view.

1. Choose the parameters. The first tab, All, lists every instance parameter. The other tabs are GTAA_IFC, GTAA_PACT, GTAA-PACT(dimension), GTAA_Cobi(Maximo), GTAA_COBie(Classification), and GTAA_COBie. Assign a parameter to a group from its list, then Save as default. That file is kept in `%AppData%\ParameterQc\default-groups.json`. A checked name with no group is its own bar.
2. Choose the analysis. The first one is **QC – Populated Parameter**.
3. The chart shows one bar per parameter group for each model category. The bar is the percent of elements whose selected parameters in that group are all filled. The label is complete elements over counted elements. Export writes those same numbers to Excel.

A value of `0`, blank, `n/a`, or `TBD` means the element is not complete. Yes and No both count as filled. Maximo groups count only elements whose `Maximo.Registrable` is YES.

Build:

```powershell
dotnet test ParameterQc.slnx
dotnet build src\ParameterQc\ParameterQc.csproj -c Release
```

The Release build copies the add-in to `%AppData%\Autodesk\Revit\Addins\2025`. Restart Revit after the first install.
