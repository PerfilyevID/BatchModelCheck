extern alias revit;
using revit::Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using static BatchModelCheck.Common.Collections;

namespace BatchModelCheck.Tools
{
    public class LevelChecker
    {
        public static List<LevelChecker> Levels = new List<LevelChecker>();
        public double Min { get; }
        public double Max { get; }
        public Level Level { get; }
        public Level UpperLevel { get; private set; }
        private Document Doc { get; }
        public static CheckResult CheckLevels(Document doc)
        {
            HashSet<int> x = new HashSet<int>();
            foreach (Level level in new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Levels).WhereElementIsNotElementType().ToElements())
            {
                string name = level.get_Parameter(BuiltInParameter.DATUM_TEXT).AsString();
                if (name.Contains('_'))
                {
                    string prefix = name.Split('_')[0];
                    if (prefix.StartsWith("С"))
                    {
                        x.Add(0);
                    }
                    else
                    {
                        if (prefix.StartsWith("К"))
                        {
                            x.Add(1);
                        }
                        else
                        {
                            x.Add(2);
                        }
                    }
                }
                else
                {
                    return CheckResult.Error;
                }
            }
            if (x.Count != 1)
            {
                return CheckResult.Error;
            }
            else
            {
                if (x.Contains(0)) { return CheckResult.Sections; }
                if (x.Contains(1)) { return CheckResult.Corpus; }
                if (x.Contains(2)) { return CheckResult.NoSections; }
            }
            return CheckResult.Error;
        }
        public static void AddLevel(Level level, Document doc, string code)
        {
            Levels.Add(new LevelChecker(doc, level, code));
        }
        public static LevelChecker GetLevelById(ElementId id)
        {
            foreach (LevelChecker lvl in Levels)
            {
                if (lvl.Level.Id.IntegerValue == id.IntegerValue)
                {
                    return lvl;
                }
            }
            return null;
        }
        public static List<LevelChecker> GetOtherLevelById(ElementId id)
        {
            List<LevelChecker> levels = new List<LevelChecker>();
            foreach (LevelChecker lvl in Levels)
            {
                if (lvl.Level.Id.IntegerValue != id.IntegerValue)
                {
                    levels.Add(lvl);
                }
            }
            return levels;
        }
        public LevelCheckResult GetLevelIntersection(BoundingBoxXYZ box)
        {
            if (box.Min.Z >= Max || box.Max.Z <= Min)
            {
                return LevelCheckResult.NotInside;
            }
            if (box.Min.Z >= Min && box.Max.Z <= Max)
            {
                return LevelCheckResult.FullyInside;
            }
            else
            {
                double elementHeight = box.Max.Z - box.Min.Z;
                double max = Math.Min(box.Max.Z, Max);
                double min = Math.Max(box.Min.Z, Min);
                double intersectHeight = max - min;
                if (intersectHeight < elementHeight / 2)
                {
                    return LevelCheckResult.TheLeastInside;
                }
                else
                {
                    return LevelCheckResult.MostlyInside;
                }
            }
        }

        public LevelCheckResult GetFloorLevelIntersection(BoundingBoxXYZ box)
        {
            if (box.Min.Z < Min - 1000 / 304.8 || box.Max.Z > Min + 1000 / 304.8)
            {
                return LevelCheckResult.NotInside;
            }
            if (box.Min.Z >= Min - 1000 / 304.8 && box.Max.Z <= Min + 1000 / 304.8)
            {
                return LevelCheckResult.FullyInside;
            }
            return LevelCheckResult.MostlyInside;
        }
        public LevelChecker(Document doc, Level level, string code)
        {
            Doc = doc;
            Level = level;
            Min = level.ProjectElevation;
            ElementId upperLevelId = level.get_Parameter(BuiltInParameter.LEVEL_UP_TO_LEVEL).AsElementId();
            if (upperLevelId == null || upperLevelId.IntegerValue == -1)
            {
                string name = Level.get_Parameter(BuiltInParameter.DATUM_TEXT).AsString();
                string part = null;
                if (name.Contains('_'))
                {
                    part = name.Split('_')[0];
                }
                UpperLevel = GetNearestUpperLevel(level.ProjectElevation, doc, part);
                if (code != null)
                {
                    UpperLevel = GetNearestUpperLevel(level.ProjectElevation, doc, part);
                }
                else
                {
                    UpperLevel = GetNearestUpperLevel(level.ProjectElevation, doc, null);
                }
                if (UpperLevel != null)
                {
                    Max = UpperLevel.ProjectElevation;
                }
                else
                {
                    Max = Min + 3000 / 304.8;
                }
            }
            else
            {
                UpperLevel = doc.GetElement(upperLevelId) as Level;
                Max = UpperLevel.ProjectElevation;
            }
        }
        private List<Level> GetLevelsByP(double elevation, Document doc, double min, double max, string part = null)
        {
            List<Level> levels = new List<Level>();
            foreach (Element element in new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Levels).WhereElementIsNotElementType().ToElements())
            {
                Level level = element as Level;
                if (part != null)
                {
                    if (!level.get_Parameter(BuiltInParameter.DATUM_TEXT).AsString().StartsWith(part))
                    {
                        continue;
                    }
                }
                if (level.ProjectElevation > elevation && level.ProjectElevation - elevation <= max / 304.8 && level.ProjectElevation - elevation >= min / 304.8)
                {
                    levels.Add(level);
                }
            }
            return levels;
        }
        public Level GetNearestUpperLevel(double elevation, Document doc, string part = null)
        {
            List<Level> levels = GetLevelsByP(elevation, doc, 2000, 5000, part);
            if (levels.Count == 0)
            {
                levels = GetLevelsByP(elevation, doc, 1000, 2000, part);
            }
            if (levels.Count == 0)
            {
                levels = GetLevelsByP(elevation, doc, 5000, 100000, part);
            }
            if (part != null)
            {
                if (levels.Count == 0)
                {
                    levels = GetLevelsByP(elevation, doc, 2000, 5000, null);
                }
                if (levels.Count == 0)
                {
                    levels = GetLevelsByP(elevation, doc, 1000, 2000, null);
                }
                if (levels.Count == 0)
                {
                    levels = GetLevelsByP(elevation, doc, 5000, 100000, null);
                }
            }
            if (levels.Count == 0)
            {
                return null;
            }
            if (levels.Count == 1)
            {
                return levels[0];
            }
            List<Level> sortedLevels = levels.OrderBy(o => o.ProjectElevation).ToList();
            return sortedLevels[0];
        }
    }
}
