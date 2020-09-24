extern alias revit;
using revit.Autodesk.Revit.Attributes;
using revit.Autodesk.Revit.DB;
using revit.Autodesk.Revit.UI;
using BatchModelCheck.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static KPLN_Loader.Output.Output;

namespace BatchModelCheck.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class CommandOpenDialog : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                PickProjects form = new PickProjects();
                form.Show();
                return Result.Succeeded;
            }
            catch (Exception e)
            { PrintError(e); }
            return Result.Failed;
        }
    }
}
