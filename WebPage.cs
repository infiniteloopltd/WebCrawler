using System;

namespace CrawlerLibrary
{
    public class WebPage
    {
        public Uri Url { get; set; }
        public CrawlState State { get; set; }
    }

    public enum CrawlState
    {
        Visited,
        Unvisited,
        Error
    }
}
