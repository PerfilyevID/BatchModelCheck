namespace BatchModelCheck.Common
{
    public static class Collections
    {
        public enum WarningStatus { Opened, ReOpened, Closed, Ignore, RequestToIgnore, RejectedToIgnore, Removed }
        public enum Icon { OpenManager, Statistics }
        public enum LevelCheckResult { FullyInside, MostlyInside, TheLeastInside, NotInside }
        public enum CheckResult { NoSections, Sections, Corpus, Error }
    }
}
