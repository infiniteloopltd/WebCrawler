using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using HtmlAgilityPack;


namespace CrawlerLibrary
{
    public class Spider
    {
        /// <summary>
        /// A list of pages to be crawled
        /// </summary>
        private static List<WebPage> Queue = new List<WebPage>();

        /// <summary>
        /// A Url prefix that the crawled page must start with. 
        /// </summary>
        public static string Scope { get; set; }

        /// <summary>
        /// Starting page of crawl.
        /// </summary>
        public static Uri StartPage { get; set; }

        /// <summary>
        /// An Event that is fired when a new page is visited
        /// </summary>
        public static Action<WebPage, string> OnVisitedPage = null;

        /// <summary>
        /// Optional event, fired when an error happens during crawl
        /// </summary>
        public static Action<WebPage, Exception> OnCrawlError = null;

        /// <summary>
        /// When new items are added or removed from the queue, when zero, it's complete.
        /// </summary>
        public static Action<int> OnQueueUpdate = null;

        public static void Start()
        {
            // Use all versions of TLS
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            if (StartPage == null) throw new Exception("StartPage not set");
            if (Scope == "") throw new Exception("Scope not set");
            if (OnQueueUpdate == null) throw new Exception("OnQueueUpdate not attached to a handler");
            if (OnVisitedPage == null) throw new Exception("OnVisitedPage not attached to a handler");
            Queue.Add(new WebPage { Url = StartPage, State = CrawlState.Unvisited});
            var ts = new ThreadStart(SpiderThread);
            var t = new Thread(ts);
            t.Start();
        }

        private static void SpiderThread()
        {
            for (;;)
            {
                var webPage = Queue.FirstOrDefault(page => page.State == CrawlState.Unvisited);
                if (webPage == null) return;
                var http = new WebClient(); // Add proxy, Add useragent
                var html = "";
                try
                {
                    html = http.DownloadString(webPage.Url.ToString());
                }
                catch (Exception ex)
                {
                    webPage.State = CrawlState.Error;
                    OnQueueUpdate(Queue.Count(q => q.State == CrawlState.Unvisited));
                    if (OnCrawlError != null) OnCrawlError(webPage, ex);
                    continue;
                }
                webPage.State = CrawlState.Visited; // handle error?                
                if (!http.ResponseHeaders[HttpResponseHeader.ContentType].StartsWith("text/html"))
                {
                    webPage.State = CrawlState.Error;
                    OnQueueUpdate(Queue.Count(q => q.State == CrawlState.Unvisited));
                    continue;
                }
                OnVisitedPage(webPage,html);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                var root = webPage.Url.ToString().Substring(0,webPage.Url.ToString().LastIndexOf('/'));
                foreach (var link in doc.DocumentNode.SelectNodes("//a[@href]"))
                {
                    var href = link.Attributes["href"].Value;
                    if (href.StartsWith("#")) continue;
                    if (href.ToUpper().StartsWith("JAVASCRIPT:")) continue;
                    if (href.ToUpper().StartsWith("TEL:")) continue;
                    if (href.ToUpper().StartsWith("FTP:")) continue;
                    if (href.ToUpper().StartsWith("MAILTO:")) continue;
                    if (!href.ToUpper().StartsWith("HTTP://") && !href.ToUpper().StartsWith("HTTPS://"))
                    {
                        if (!href.StartsWith("/")) href = "/" + href;
                        // Check relative vs absolute
                        href = root + href;
                    }
                    else if (!href.ToUpper().StartsWith(Scope.ToUpper()))
                    {
                        // Linking to an external website (out of scope)
                        continue;
                    }

                    if (Queue.FirstOrDefault(page => page.Url.ToString() == href) != null)
                    {
                        // Duplicate
                        continue;
                    }
                    
                    // Check Scope
                    Queue.Add(new WebPage
                    {
                        Url = new Uri(href),
                        State = CrawlState.Unvisited
                    });
                }
                OnQueueUpdate(Queue.Count(q => q.State == CrawlState.Unvisited));
            }
        }
     

    }
}
