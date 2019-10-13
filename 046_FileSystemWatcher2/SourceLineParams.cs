using System;

namespace TeamSystem.Customizations
{
    internal sealed class SourceLineParams
    {
        public string Article { get; private set; }
        public string ArticleNo { get; private set; }
        public DateTime StartDateTime { get; private set; }
        public DateTime EndDateTime { get; private set; }
        public TimeSpan CycleTime { get; private set; }
        public TimeSpan BendTime { get; private set; }

        public TimeSpan DeclaredDuration
        {
            get { return this.EndDateTime - this.StartDateTime; }
        }

        public SourceLineParams(string article, string articleNo,
                                DateTime startDateTime, DateTime endDateTime,
                                TimeSpan cycleTime, TimeSpan bendTime)
        {
            this.Article = article;
            this.ArticleNo = articleNo;
            this.StartDateTime = startDateTime;
            this.EndDateTime = endDateTime;
            this.CycleTime = cycleTime;
            this.BendTime = bendTime;
        }
    }
}