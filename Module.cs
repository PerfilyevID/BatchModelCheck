extern alias revit;
using revit.Autodesk.Revit.DB.Events;
using revit.Autodesk.Revit.UI;
using revit.Autodesk.Revit.UI.Events;
using KPLN_Loader.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using static BatchModelCheck.ModuleData;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using static KPLN_Loader.Output.Output;
using revit::Autodesk.Revit.DB;
using KPLNDataBase.Collections;
using System.IO;
using BatchModelCheck.DB;
using BatchModelCheck.Tools;
using System.Text;
using System.Runtime.InteropServices;
using System.Data.SQLite;

namespace BatchModelCheck
{
    public class Module : IExternalModule
    {
        public Result Close()
        {
            return Result.Succeeded;
        }
        public Result Execute(UIControlledApplication application, string tabName)
        {
#if Revit2020
            MainWindowHandle = application.MainWindowHandle;
            HwndSource hwndSource = HwndSource.FromHwnd(MainWindowHandle);
            RevitWindow = hwndSource.RootVisual as Window;
#endif
#if Revit2018
            try
            {
                MainWindowHandle = WindowHandleSearch.MainWindowHandle.Handle;
            }
            catch (Exception) { }
#endif
            string assembly = Assembly.GetExecutingAssembly().Location.Split(new string[] { "\\" }, StringSplitOptions.None).Last().Split('.').First();
            string ribbonName = "Проверки";
            RibbonPanel panel = application.CreateRibbonPanel(tabName, ribbonName);
            if (KPLN_Loader.Preferences.User.Department.Id == 4)
            {
                AddPushButtonData("Открыть менеджер проверок", "Серийная\nпроверка", "Запуск проверки выбранных документов на ошибки.", string.Format("{0}.{1}", assembly, "Commands.CommandOpenDialog"), panel, new Source.Source(Common.Collections.Icon.OpenManager), true);
            }
            AddPushButtonData("Открыть окно статистики", "Окно\ncтатистики", "Отображение статистики по документам, которые были проверены на ошибки.", string.Format("{0}.{1}", assembly, "Commands.CommandShowStatistics"), panel, new Source.Source(Common.Collections.Icon.Statistics), true);
            panel.AddSlideOut();
            if (KPLN_Loader.Preferences.User.Department.Id == 4)
            {
                AddPushButtonData("Параметры", "Параметры", "Редактирование пользовательских настроек.", string.Format("{0}.{1}", assembly, "Commands.CommandShowSettings"), panel, new Source.Source(Common.Collections.Icon.Preferences), true);
            }
            application.DialogBoxShowing += OnDialogBoxShowing;
            application.ControlledApplication.ApplicationInitialized += OnInitialized;
            application.ControlledApplication.FailuresProcessing += OnFailureProcessing;
            application.ControlledApplication.DocumentOpened += OnOpened;
            return Result.Succeeded;
        }
        private string NormalizeString(string value)
        {
            string result = string.Empty;
            foreach(char c in value)
            {
                if (char.IsWhiteSpace(c))
                {
                    result += '_';
                }
                else
                {
                    result += c;
                }
            }
            return result;
        }
        private void CheckDocument(Document doc, DbDocument dbDoc)
        {
            try
            {
                DbRowData rowData = new DbRowData();
                rowData.Errors.Add(new DbError("Ошибка привязки к уровню", CheckTools.CheckLevels(doc)));
                rowData.Errors.Add(new DbError("Зеркальные элементы", CheckTools.CheckMirrored(doc)));
                rowData.Errors.Add(new DbError("Ошибка мониторинга осей", CheckTools.CheckMonitorGrids(doc)));
                rowData.Errors.Add(new DbError("Ошибка мониторинга уровней", CheckTools.CheckMonitorLevels(doc)));
                rowData.Errors.Add(new DbError("Дубликаты имен", CheckTools.CheckNames(doc)));
                rowData.Errors.Add(new DbError("Ошибки подгруженных связей", CheckTools.CheckSharedLocations(doc) + CheckTools.CheckLinkWorkSets(doc)));
                rowData.Errors.Add(new DbError("Предупреждения Revit", CheckTools.CheckErrors(doc)));
                rowData.Errors.Add(new DbError("Размер файла", CheckTools.CheckFileSize(dbDoc.Path)));
                DbController.WriteValue(dbDoc.Id.ToString(), rowData.ToString());
                //BotActions.SendRegularMessage(string.Format("👌 @{0}_{1} завершил проверку документа #{3} #{2}", NormalizeString(KPLN_Loader.Preferences.User.Family), NormalizeString(KPLN_Loader.Preferences.User.Name), NormalizeString(dbDoc.Project.Name), NormalizeString(dbDoc.Name)), Bot.Target.Process);
            }
            catch (Exception) { }
        }
        public void OnOpened(object sender, DocumentOpenedEventArgs args)
        {
            try
            {
                Document doc = args.Document;
                if (!CheckTools.AllWorksetsAreOpened(doc)) { return; }
                if (doc.IsWorkshared && !doc.IsDetached)
                {
                    string path = ModelPathUtils.ConvertModelPathToUserVisiblePath(doc.GetWorksharingCentralModelPath());
                    FileInfo centralPath = new FileInfo(path);
                    foreach (DbDocument dbDoc in KPLNDataBase.DbControll.Documents)
                    {
                        FileInfo metaCentralPath = new FileInfo(dbDoc.Path);
                        if (dbDoc.Code == "NONE") { continue; }
                        if (centralPath.FullName == metaCentralPath.FullName)
                        {
                            if (File.Exists(string.Format(@"Z:\Отдел BIM\03_Скрипты\09_Модули_KPLN_Loader\DB\BatchModelCheck\doc_id_{0}.sqlite", dbDoc.Id.ToString())))
                            {
                                List<DbRowData> rows = DbController.GetRows(dbDoc.Id.ToString());
                                if (rows.Count != 0)
                                {
                                    if ((DateTime.Now.Day - rows.Last().DateTime.Day > 7 && rows.Last().DateTime.Day != DateTime.Now.Day) || rows.Last().DateTime.Month != DateTime.Now.Month || rows.Last().DateTime.Year != DateTime.Now.Year)
                                    {
                                        CheckDocument(doc, dbDoc);
                                    }
                                }
                                else
                                {
                                    CheckDocument(doc, dbDoc);
                                }
                            }
                            else
                            {
                                CheckDocument(doc, dbDoc);
                            }
                        }
                    }
                }
            }
            catch (Exception) { }
        }
        public void OnFailureProcessing(object sender, FailuresProcessingEventArgs args)
        {
            if (!ModuleData.AutoConfirmEnabled || !ModuleData.up_close_dialogs)
            {
                return;
            }
            bool gotErrors = false;
            foreach (FailureMessageAccessor i in args.GetFailuresAccessor().GetFailureMessages())
            {
                if (i.GetSeverity() == FailureSeverity.Warning)
                {
                    args.GetFailuresAccessor().DeleteWarning(i);
                }
                else
                {
                    args.GetFailuresAccessor().ResolveFailure(i);
                    gotErrors = true;
                }
                args.GetFailuresAccessor().DeleteAllWarnings();
                args.GetFailuresAccessor().ResolveFailures(args.GetFailuresAccessor().GetFailureMessages());
            }
            if (gotErrors)
            {
                args.SetProcessingResult(FailureProcessingResult.ProceedWithCommit);
            }
            else
            {
                args.SetProcessingResult(FailureProcessingResult.Continue);
            }
        }
        public void OnInitialized(object sender, ApplicationInitializedEventArgs args)
        {
            KPLNDataBase.DbControll.Update();
        }

        public void OnDialogBoxShowing(object sender, DialogBoxShowingEventArgs args)
        {
            if (!ModuleData.AutoConfirmEnabled || !ModuleData.up_close_dialogs) 
            {
                return; 
            }
            HashSet<string> dialogIds = new HashSet<string>();
            SQLiteConnection sql = new SQLiteConnection();
            try
            {
                sql.ConnectionString = string.Format(@"Data Source=Z:\Отдел BIM\03_Скрипты\08_Базы данных\KPLN_Loader.db;Version=3;");
                sql.Open();
                using (SQLiteCommand cmd = new SQLiteCommand("SELECT Name FROM TaskDialogs", sql))
                {
                    using (SQLiteDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            dialogIds.Add(rdr.GetString(0));
                        }
                    }
                }
                sql.Close();
            }
            catch (Exception)
            {
                try
                {
                    sql.Close();
                }
                catch (Exception) { }
            }
            if (dialogIds.Contains(args.DialogId))
            {
                Print(string.Format("Всплывающий диалог: [{0}]...", args.DialogId), KPLN_Loader.Preferences.MessageType.System_Regular);
                sql = new SQLiteConnection();
                string value = "NONE";
                try
                {
                    sql.ConnectionString = string.Format(@"Data Source=Z:\Отдел BIM\03_Скрипты\08_Базы данных\KPLN_Loader.db;Version=3;");
                    sql.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(string.Format("SELECT OverrideResult FROM TaskDialogs WHERE Name = '{0}'", args.DialogId), sql))
                    {
                        using (SQLiteDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                value = rdr.GetString(0);
                            }
                        }
                    }
                    sql.Close();
                }
                catch (Exception)
                {
                    try
                    {
                        sql.Close();
                    }
                    catch (Exception) { }
                }
                if (value != "NONE")
                {
                    TaskDialogResult rslt;
                    if (Enum.TryParse(value, out rslt))
                    {
                        args.OverrideResult((int)rslt);
                        Print(string.Format("...действие «по умолчанию» - [{0}]", rslt.ToString("G")), KPLN_Loader.Preferences.MessageType.System_Regular);
                    }

                }
                else
                {
                    Print("...действие «по умолчанию» - [NONE]", KPLN_Loader.Preferences.MessageType.System_Regular);
                }
            }
            else
            {
                Print(string.Format("Не удалось идентифицировать всплывающий диалог: [{0}]", args.DialogId), KPLN_Loader.Preferences.MessageType.Critical);
                try
                {
                    sql.ConnectionString = string.Format(@"Data Source=Z:\Отдел BIM\03_Скрипты\08_Базы данных\KPLN_Loader.db;Version=3;");
                    sql.Open();
                    using (SQLiteCommand cmd = sql.CreateCommand())
                    {
                        cmd.CommandText = "INSERT INTO TaskDialogs ([Name], [OverrideResult]) VALUES (@Name, @OverrideResult)";
                        cmd.Parameters.Add(new SQLiteParameter() { ParameterName = "@Name", Value = args.DialogId });
                        cmd.Parameters.Add(new SQLiteParameter() { ParameterName = "@OverrideResult", Value = "NONE" });
                        cmd.ExecuteNonQuery();
                    }
                    sql.Close();
                }
                catch (Exception)
                {
                    try
                    {
                        sql.Close();
                    }
                    catch (Exception) { }
                }
                try
                {
                    Thread t = new Thread(() =>
                    {
                        SaveImage(args.DialogId.ToString());
                    });
                    t.Start();
                }
                catch (Exception)
                { }
                try
                {
                    if (args.Cancellable)
                    {
                        args.Cancel();
                        Print("...действие «по умолчанию» - [Закрыть]", KPLN_Loader.Preferences.MessageType.System_Regular);
                    }
                }
                catch (Exception)
                { }
            }
        }
        private static void SaveImage(string name)
        {
            try
            {
                Thread.Sleep(1500);
                int screenLeft = SystemInformation.VirtualScreen.Left;
                int screenTop = SystemInformation.VirtualScreen.Top;
                int screenWidth = SystemInformation.VirtualScreen.Width;
                int screenHeight = SystemInformation.VirtualScreen.Height;
                Bitmap bitmap = new Bitmap(screenWidth, screenHeight);
                Graphics graphics = Graphics.FromImage(bitmap as Image);
                graphics.CopyFromScreen(screenLeft, screenTop, 0, 0, bitmap.Size);
                bitmap.Save(string.Format(@"Z:\Отдел BIM\03_Скрипты\09_Модули_KPLN_Loader\DB\TaskDialogs\{0}_{1}.jpg", name, Guid.NewGuid().ToString()), ImageFormat.Jpeg);
            }
            catch (Exception)
            { }
        }
        private void AddPushButtonData(string name, string text, string description, string className, RibbonPanel panel, Source.Source imageSource, bool avclass)
        {
            PushButtonData data = new PushButtonData(name, text, Assembly.GetExecutingAssembly().Location, className);
            PushButton button = panel.AddItem(data) as PushButton;
            button.ToolTip = description;
            if (avclass)
            {
                button.AvailabilityClassName = "BatchModelCheck.Availability.StaticAvailable";
            }
            button.LongDescription = string.Format("Верстия: {0}\nСборка: {1}-{2}", ModuleData.Version, ModuleData.Build, ModuleData.Date);
            button.ItemText = text;
            button.LargeImage = new BitmapImage(new Uri(imageSource.Value));
        }
    }
}
