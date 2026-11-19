using System.Reflection;
using Autodesk.Revit.UI;

namespace ParameterQc;

public class App : IExternalApplication
{
    public static string AssemblyPath { get; private set; } = string.Empty;

    public Result OnStartup(UIControlledApplication application)
    {
        AssemblyPath = Assembly.GetExecutingAssembly().Location;
        CreateRibbon(application);
        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application) => Result.Succeeded;

    private static void CreateRibbon(UIControlledApplication app)
    {
        const string tabName = "Automation";
        try { app.CreateRibbonTab(tabName); }
        catch { /* tab already exists */ }

        RibbonPanel panel = GetOrCreatePanel(app, tabName, "Parameter QC");
        var buttonData = new PushButtonData(
            "ParameterQc.Open",
            "Parameter\nQC",
            AssemblyPath,
            "ParameterQc.Commands.OpenParameterQcCommand")
        {
            ToolTip = "Check how completely instance parameters are filled, by model category.",
            LongDescription = "Choose parameters, roll them up by group, and chart the filled percentage for each model category in the active view.",
            AvailabilityClassName = "ParameterQc.Commands.ProjectDocumentAvailability"
        };

        panel.AddItem(buttonData);
    }

    private static RibbonPanel GetOrCreatePanel(UIControlledApplication app, string tabName, string panelName)
    {
        foreach (RibbonPanel existing in app.GetRibbonPanels(tabName))
        {
            if (existing.Name.Equals(panelName, StringComparison.OrdinalIgnoreCase))
                return existing;
        }

        return app.CreateRibbonPanel(tabName, panelName);
    }
}
