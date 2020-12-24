extern alias revit;

using BatchModelCheck.Forms;
using revit::Autodesk.Revit.Attributes;
using revit::Autodesk.Revit.DB;
using revit::Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchModelCheck.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class CommandShowSettings : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UserSettings form = new UserSettings();
                form.ShowDialog();
                return Result.Succeeded;
            }
            catch (Exception)
            {
                return Result.Failed;
            }
        }
    }
}
