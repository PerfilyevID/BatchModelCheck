extern alias revit;
using revit.Autodesk.Revit.DB;
using revit.Autodesk.Revit.UI;

namespace BatchModelCheck.Availability
{
    public class StaticAvailable : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories)
        {
            return true;
        }
    }
}
