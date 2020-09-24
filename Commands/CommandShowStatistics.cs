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
using BatchModelCheck.DB;
using System.IO;

namespace BatchModelCheck.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class CommandShowStatistics : IExternalCommand
    {
        private static bool InList(KPLNDataBase.Collections.DbProject project, List<KPLNDataBase.Collections.DbProject> projects)
        {
            foreach (var i in projects)
            {
                if (project.Id == i.Id)
                {
                    return true;
                }
            }
            return false;
        }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                List<KPLNDataBase.Collections.DbProject> projects = new List<KPLNDataBase.Collections.DbProject>();
                foreach (KPLNDataBase.Collections.DbDocument doc in KPLNDataBase.DbControll.Documents)
                {
                    if (File.Exists(string.Format(@"Z:\Отдел BIM\03_Скрипты\09_Модули_KPLN_Loader\DB\BatchModelCheck\doc_id_{0}.sqlite", doc.Id.ToString())))
                    {
                        if (!InList(doc.Project, projects) || projects.Count == 0)
                        {
                            projects.Add(doc.Project);
                        }
                    }
                }
                if (projects.Count == 0) 
                {
                    Print("Расчеты не найдены!", KPLN_Loader.Preferences.MessageType.Error);
                    return Result.Cancelled;
                }
                Picker pp = new Picker(projects);
                pp.ShowDialog();
                if (Picker.PickedProject != null)
                {
                    KPLNDataBase.Collections.DbProject pickedProject = Picker.PickedProject;
                    List<KPLNDataBase.Collections.DbDocument> documents = new List<KPLNDataBase.Collections.DbDocument>();
                    foreach (KPLNDataBase.Collections.DbDocument doc in KPLNDataBase.DbControll.Documents)
                    {
                        if (File.Exists(string.Format(@"Z:\Отдел BIM\03_Скрипты\09_Модули_KPLN_Loader\DB\BatchModelCheck\doc_id_{0}.sqlite", doc.Id.ToString())))
                        {
                            if (doc.Project.Id == pickedProject.Id)
                            {
                                documents.Add(doc);
                            }
                        }

                    }
                    Picker dp = new Picker(documents);
                    dp.ShowDialog();
                    if (Picker.PickedDocument != null)
                    {
                        KPLNDataBase.Collections.DbDocument pickedDocument = Picker.PickedDocument;
                        OutputDB form = new OutputDB(string.Format("{0}: {1}", pickedProject.Name, pickedDocument.Name), DbController.GetRows(pickedDocument.Id.ToString()));
                        form.Show();
                        return Result.Succeeded;
                    }
                }
            }
            catch (Exception e)
            {
                PrintError(e);
            }
            return Result.Failed;
        }
    }
}
