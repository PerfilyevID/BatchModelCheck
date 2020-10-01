extern alias revit;
using System;
using System.Collections.Generic;
using System.Linq;
using static KPLN_Loader.Output.Output;
using static BatchModelCheck.Common.Collections;
using revit.Autodesk.Revit.DB;
using System.IO;

namespace BatchModelCheck.Tools
{
    public static class CheckTools
    {
        public static int CheckFileSize(string path)
        {
            FileInfo fileInfo = new FileInfo(path);
            return (int)Math.Round((double)fileInfo.Length / 1000000);
        }
        public static int CheckErrors(Document doc)
        {
            return doc.GetWarnings().Count();
        }
        public static int CheckLevels(Document doc)
        {
            int ammount = 0;
            try
            {
                string code = null;
                switch (LevelChecker.CheckLevels(doc))
                {
                    case CheckResult.NoSections:
                        return ammount;
                    case CheckResult.Error:
                        return new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Levels).WhereElementIsNotElementType().Count();
                    case CheckResult.Corpus:
                        code = "К";
                        break;
                    case CheckResult.Sections:
                        code = "С";
                        break;
                }

                LevelChecker.Levels.Clear();
                foreach (Element element in new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Levels).WhereElementIsNotElementType().ToElements())
                {
                    LevelChecker.AddLevel(element as Level, doc, code);
                }
                foreach (BuiltInCategory cat in new BuiltInCategory[] { BuiltInCategory.OST_Windows,
                                                                        BuiltInCategory.OST_Doors,
                                                                        BuiltInCategory.OST_MechanicalEquipment,
                                                                        BuiltInCategory.OST_Walls,
                                                                        BuiltInCategory.OST_Floors,
                                                                        BuiltInCategory.OST_Ceilings,
                                                                        BuiltInCategory.OST_Furniture,
                                                                        BuiltInCategory.OST_GenericModel})
                {
                    foreach (Element element in new FilteredElementCollector(doc).OfCategory(cat).WhereElementIsNotElementType().ToElements())
                    {
                        if (element.GetType() == typeof(FamilyInstance))
                        {
                            if ((element as FamilyInstance).SuperComponent != null)
                            {
                                continue;
                            }
                        }
                        try
                        {
                            Level level = doc.GetElement(element.LevelId) as Level;
                            if (element.GetType() == typeof(FamilyInstance) && level == null)
                            {
                                try
                                {
                                    level = doc.GetElement((element as FamilyInstance).Host.LevelId) as Level;
                                }
                                catch (Exception)
                                {
                                    level = doc.GetElement(element.LevelId) as Level;
                                }
                            }
                            if (level == null) { continue; }
                            else
                            {
                                BoundingBoxXYZ box = element.get_BoundingBox(null);
                                LevelChecker checker = LevelChecker.GetLevelById(level.Id);
                                if (element.GetType() != typeof(Ceiling) && element.GetType() != typeof(Floor))
                                {
                                    LevelCheckResult result = checker.GetLevelIntersection(box);
                                    if (result == LevelCheckResult.FullyInside)
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        if (result != LevelCheckResult.TheLeastInside && result != LevelCheckResult.MostlyInside)
                                        {
                                            bool skip = false;
                                            foreach (LevelChecker c in LevelChecker.GetOtherLevelById(level.Id))
                                            {
                                                LevelCheckResult rslt = c.GetLevelIntersection(box);
                                                if (rslt == LevelCheckResult.FullyInside)
                                                {
                                                    ammount++;
                                                    skip = true;
                                                    break;
                                                }
                                            }
                                            if (!skip)
                                            {
                                                foreach (LevelChecker c in LevelChecker.GetOtherLevelById(level.Id))
                                                {
                                                    LevelCheckResult rslt = c.GetLevelIntersection(box);
                                                    if (rslt == LevelCheckResult.MostlyInside)
                                                    {
                                                        ammount++;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    LevelCheckResult result = checker.GetFloorLevelIntersection(box);
                                    if (result != LevelCheckResult.MostlyInside && result != LevelCheckResult.FullyInside)
                                    {
                                        bool skip = false;
                                        foreach (LevelChecker c in LevelChecker.GetOtherLevelById(level.Id))
                                        {
                                            LevelCheckResult rslt = c.GetFloorLevelIntersection(box);
                                            if (rslt == LevelCheckResult.FullyInside)
                                            {
                                                ammount++;
                                                skip = true;
                                                break;
                                            }
                                        }
                                        if (!skip)
                                        {
                                            foreach (LevelChecker c in LevelChecker.GetOtherLevelById(level.Id))
                                            {
                                                LevelCheckResult rslt = c.GetFloorLevelIntersection(box);
                                                if (rslt == LevelCheckResult.MostlyInside)
                                                {
                                                    ammount++;
                                                    skip = true;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception) { }
                    }
                }
            }
            catch (Exception e)
            { PrintError(e); }
            return ammount;
        }
        public static int CheckMirrored(Document doc)
        {
            int ammount = 0;
            try
            {
                foreach (BuiltInCategory category in new BuiltInCategory[] { BuiltInCategory.OST_Doors, BuiltInCategory.OST_Windows })
                {
                    foreach (Element element in new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).OfCategory(category).WhereElementIsNotElementType().ToElements())
                    {
                        try
                        {
                            FamilyInstance instance = element as FamilyInstance;
                            if (instance.Mirrored)
                            {
                                ammount++;
                            }
                        }
                        catch (Exception) { }
                    }
                }
            }
            catch (Exception e)
            { PrintError(e); }
            return ammount;
        }
        public static int CheckSharedLocations(Document doc)
        {
            int ammount = 0;
            try
            {
                HashSet<string> links = new HashSet<string>();
                foreach (RevitLinkInstance link in new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_RvtLinks).WhereElementIsNotElementType().ToElements())
                {
                    try
                    {
                        Document linkDocument = link.GetLinkDocument();
                        string name = linkDocument.PathName;
                        string currentPosition = link.Name.Split(new string[] { "позиция " }, StringSplitOptions.RemoveEmptyEntries).Last();
                        if (!links.Contains(name))
                        {
                            links.Add(name);
                        }
                        else
                        {
                            ammount++;
                        }
                        if (currentPosition == "<Не общедоступное>")
                        {
                            ammount++;
                        }
                        else
                        {
                            bool detected = false;
                            foreach (ProjectLocation i in doc.ProjectLocations)
                            {
                                if (i.Name == currentPosition)
                                {
                                    detected = true;
                                    if (currentPosition == "Встроенный")
                                    {
                                        ammount++;
                                    }
                                    else
                                    {
                                        if (!link.Pinned)
                                        {
                                            ammount++;

                                        }
                                    }
                                }
                            }
                            if (!detected)
                            {
                                foreach (ProjectLocation i in linkDocument.ProjectLocations)
                                {
                                    if (i.Name == currentPosition)
                                    {
                                        detected = true;
                                        if (currentPosition == "Встроенный")
                                        {
                                            ammount++;
                                        }
                                        else
                                        {
                                            if (!link.Pinned)
                                            {
                                                ammount++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        try
                        {
                            ammount++;
                        }
                        catch (Exception) { }
                    }
                }
            }
            catch (Exception e)
            { PrintError(e); }
            return ammount;
        }
        public static int CheckLinkWorkSets(Document doc)
        {
            int ammount = 0;
            try
            {
                List<Workset> worksets = new List<Workset>();
                foreach (Workset w in new FilteredWorksetCollector(doc).OfKind(WorksetKind.UserWorkset))
                {
                    if (!w.IsOpen)
                    {
                        ammount++;
                    }
                    else
                    {
                        worksets.Add(w);
                    }
                }
                foreach (RevitLinkInstance link in new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_RvtLinks).WhereElementIsNotElementType().ToElements())
                {
                    foreach (Workset w in worksets)
                    {
                        if (link.WorksetId.IntegerValue == w.Id.IntegerValue)
                        {
                            if (!w.Name.StartsWith("#"))
                            {
                                ammount++;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            { PrintError(e); }
            return ammount;
        }
        public static int CheckMonitorGrids(Document doc)
        {
            int ammount = 0;
            try
            {
                HashSet<int> ids = new HashSet<int>();
                foreach (Element element in new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Grids).WhereElementIsNotElementType().ToElements())
                {
                    try
                    {
                        if (element.IsMonitoringLinkElement())
                        {
                            RevitLinkInstance link = null;
                            List<string> names = new List<string>();
                            foreach (ElementId i in element.GetMonitoredLinkElementIds())
                            {
                                ids.Add(i.IntegerValue);
                                link = doc.GetElement(i) as RevitLinkInstance;
                                names.Add(link.Name);
                            }
                            if (link != null)
                            {
                                ammount++;
                            }
                        }
                        else
                        {
                            ammount++;
                        }
                    }
                    catch (Exception)
                    { }
                }
                if (ids.Count > 1)
                {
                    ammount++;

                }
            }
            catch (Exception e)
            { PrintError(e); }
            return ammount;
        }
        public static int CheckMonitorLevels(Document doc)
        {
            int ammount = 0;
            try
            {
                HashSet<int> ids = new HashSet<int>();
                foreach (Element element in new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Levels).WhereElementIsNotElementType().ToElements())
                {
                    try
                    {
                        if (element.IsMonitoringLinkElement())
                        {
                            RevitLinkInstance link = null;
                            List<string> names = new List<string>();
                            foreach (ElementId i in element.GetMonitoredLinkElementIds())
                            {
                                ids.Add(i.IntegerValue);
                                link = doc.GetElement(i) as RevitLinkInstance;
                                names.Add(link.Name);
                            }
                            if (link == null)
                            {
                                ammount++;
                            }
                        }
                        else
                        {
                            ammount++;
                        }
                    }
                    catch (Exception)
                    { }
                }
                if (ids.Count > 1)
                {
                    ammount++;
                }
            }
            catch (Exception e)
            { PrintError(e); }
            return ammount;
        }
        public static int CheckNames(Document doc)
        {
            int ammount = 0;
            try
            {
                HashSet<int> _fam_ids = new HashSet<int>();
                HashSet<string> _fam_names = new HashSet<string>();
                List<Family> _fams = new List<Family>();
                foreach (Family family in new FilteredElementCollector(doc).OfClass(typeof(Family)).ToElements())
                {
                    if (!_fam_ids.Contains(family.Id.IntegerValue))
                    {
                        _fam_ids.Add(family.Id.IntegerValue);
                        _fams.Add(family);
                        _fam_names.Add(family.Name);
                    }
                    else
                    {
                        continue;
                    }
                }
                foreach (Family family in _fams)
                {
                    try
                    {
                        HashSet<string> _sym_names = new HashSet<string>();
                        List<FamilySymbol> _syms = new List<FamilySymbol>();
                        string _family_name = family.Name;
                        if (IsInteger(_family_name[_family_name.Length - 1]) && !IsInteger(_family_name[_family_name.Length - 2]))
                        {
                            foreach (int i in new int[] { 1, 2 })
                            {
                                if (_fam_names.Contains(GetShortenedName(_family_name, i)))
                                {
                                    ammount++;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                        foreach (ElementId id in family.GetFamilySymbolIds())
                        {
                            FamilySymbol symbol = family.Document.GetElement(id) as FamilySymbol;
                            _syms.Add(symbol);
                            _sym_names.Add(symbol.Name);
                        }
                        HashSet<string> _check_names = new HashSet<string>();
                        foreach (FamilySymbol symbol in _syms)
                        {
                            string _symbol_name = symbol.Name;
                            if (IsInteger(_symbol_name[_symbol_name.Length - 1]) && !IsInteger(_symbol_name[_symbol_name.Length - 2]))
                            {
                                foreach (int i in new int[] { 1, 2 })
                                {
                                    _check_names.Add(GetShortenedName(_symbol_name, i));
                                    if (_sym_names.Contains(GetShortenedName(_symbol_name, i)))
                                    {
                                        ammount++;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }
                    catch (Exception)
                    { }
                }
            }
            catch (Exception)
            { }
            return ammount;
        }
        #region add
        private static string GetShortenedName(string value, int ammount = 1)
        {
            string result = string.Empty;
            for (int i = 0; i < value.Length - ammount; i++)
            {
                try
                {
                    result += value[i];
                }
                catch (Exception)
                { }
            }
            return result;
        }
        private static bool IsInteger(char c)
        {
            if ("0123456789".Contains(c))
            {
                return true;
            }
            return false;

        }
        private static List<string> GetTypes(Family family)
        {
            List<string> values = new List<string>();
            foreach (var i in family.GetFamilySymbolIds())
            {
                values.Add((family.Document.GetElement(i) as FamilySymbol).Name);
            }
            return values;
        }
        #endregion
    }
}
