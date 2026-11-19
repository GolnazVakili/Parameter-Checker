using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace ParameterQc.Commands;

public class ProjectDocumentAvailability : IExternalCommandAvailability
{
    public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories)
    {
        Document? document = applicationData.ActiveUIDocument?.Document;
        return document != null && !document.IsFamilyDocument;
    }
}
