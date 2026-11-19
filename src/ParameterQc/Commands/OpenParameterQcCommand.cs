using System.Windows;
using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ParameterQc.Services;
using ParameterQc.UI;

namespace ParameterQc.Commands;

[Transaction(TransactionMode.ReadOnly)]
[Regeneration(RegenerationOption.Manual)]
public class OpenParameterQcCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            UIDocument? uiDocument = commandData.Application.ActiveUIDocument;
            Document? document = uiDocument?.Document;
            if (document == null)
            {
                TaskDialog.Show("Parameter QC", "Open a project first.");
                return Result.Cancelled;
            }

            if (document.IsFamilyDocument)
            {
                TaskDialog.Show("Parameter QC", "Open a project document. Parameter QC does not run in the family editor.");
                return Result.Cancelled;
            }

            View? view = uiDocument!.ActiveView;
            if (view == null)
            {
                TaskDialog.Show("Parameter QC", "Open a view that shows the model elements you want to check.");
                return Result.Cancelled;
            }

            ViewScan scan = ViewScanner.Scan(document, view);
            if (scan.ModelElementCount == 0)
            {
                TaskDialog.Show(
                    "Parameter QC",
                    "The active view has no model elements. Open a floor plan, 3D view, or section that shows the elements you want to check.");
                return Result.Cancelled;
            }

            if (scan.Parameters.Count == 0)
            {
                TaskDialog.Show("Parameter QC", "The model elements in this view have no instance parameters.");
                return Result.Cancelled;
            }

            var picker = new ParameterPickerWindow(scan.ViewName, scan.ModelElementCount, scan.Parameters);
            SetOwner(picker, commandData);
            if (picker.ShowDialog() != true)
                return Result.Cancelled;

            var analysis = new AnalysisChoiceWindow();
            SetOwner(analysis, commandData);
            if (analysis.ShowDialog() != true)
                return Result.Cancelled;

            IReadOnlyList<ElementRecord> records = ViewScanner.Read(document, scan.ElementIds, picker.Selection);
            PopulationReport report = ReportBuilder.Build(records, picker.Selection);
            var results = new ResultWindow(report, scan.ViewName, scan.ModelElementCount);
            SetOwner(results, commandData);
            results.ShowDialog();
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            TaskDialog.Show("Parameter QC", ex.Message);
            return Result.Failed;
        }
    }

    private static void SetOwner(Window window, ExternalCommandData commandData)
    {
        new WindowInteropHelper(window).Owner = commandData.Application.MainWindowHandle;
    }
}
