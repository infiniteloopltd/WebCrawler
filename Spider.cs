using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using HtmlAgilityPack;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary; 

namespace CrawlerLibrary
{
    public class Spider
    {
        /// <summary>
        /// A list of pages to be crawled
        /// </summary>
        private static Dictionary<string, bool> Queue = new Dictionary<string, bool>();

        /// <summary>
        /// All pages visited
        /// </summary>
        private static Dictionary<string, bool> History = new Dictionary<string, bool>();
        
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
        public static Action<string, string> OnVisitedPage = null;

        /// <summary>
        /// Optional event, fired when an error happens during crawl
        /// </summary>
        public static Action<string, Exception> OnCrawlError = null;

        /// <summary>
        /// When new items are added or removed from the queue, when zero, it's complete.
        /// </summary>
        public static Action<int> OnQueueUpdate = null;

        /// <summary>
        /// Announce the Useragent so that webmasters will know the purpose of the visit.
        /// </summary>
        public static string UserAgent = "https://github.com/infiniteloopltd/WebCrawler";

        /// <summary>
        /// If a proxy is required, set here.
        /// </summary>
        public static WebProxy Proxy = null;

        public static void Start()
        {
            // Use all versions of TLS
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            if (StartPage == null) throw new Exception("StartPage not set");
            if (Scope == "") throw new Exception("Scope not set");
            if (OnQueueUpdate == null) throw new Exception("OnQueueUpdate not attached to a handler");
            if (OnVisitedPage == null) throw new Exception("OnVisitedPage not attached to a handler");
            // It's possible for the start page to be already present, if this is loaded from a saved state.
            if (!Queue.ContainsKey(StartPage.ToString())) Queue.Add(StartPage.ToString(),true);
            var ts = new ThreadStart(SpiderThread);
            var t = new Thread(ts);
            t.Start();
        }
       
        private static void SpiderThread()
        {
            for (;;)
            {
                if (Queue.Count == 0) return;
                var webPage = Queue.First().Key;
                var http = new WebClient();
                http.Headers.Add(HttpRequestHeader.UserAgent, UserAgent);
                if (Proxy != null) http.Proxy = Proxy;
                string html;
                try
                {
                    html = http.DownloadString(webPage);
                }
                catch (Exception ex)
                {
                    Queue.Remove(webPage);
                    History.Add(webPage, true);
                    OnQueueUpdate(Queue.Count);
                    if (OnCrawlError != null) OnCrawlError(webPage, ex);
                    continue;
                }
                Queue.Remove(webPage);
                if (!History.ContainsKey(webPage)) History.Add(webPage, true);
                var mimeType = http.ResponseHeaders[HttpResponseHeader.ContentType];
                if (!mimeType.StartsWith("text/html"))
                {   
                    if (OnCrawlError != null) OnCrawlError(webPage, new Exception("Incorrect MIME type " + mimeType) );
                    OnQueueUpdate(Queue.Count);
                    continue;
                }
                OnVisitedPage(webPage,html);
                OnQueueUpdate(Queue.Count);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                var root = webPage.Substring(0,webPage.LastIndexOf('/'));
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
                    
                    if (!href.ToUpper().StartsWith(Scope.ToUpper()))
                    {
                        // Linking to an external website (out of scope)
                        continue;
                    }

                    if (href.Contains("?") && webPage.Contains("?"))
                    {
                        // Don't queue child pages of pages with query-strings. 
                        continue;
                    }

                    if (History.ContainsKey(href))
                    {
                        // Historical Duplicate
                        continue;
                    }

                    if (Queue.ContainsKey(href))
                    {
                        // Recent Duplicate
                        continue;
                    }
                    
                    // Check Scope
                    Queue.Add(href,false);
                }
            }
        }

        public static void SaveState(string name)
        {
            Save(Queue, "queue-" + name + ".dat");
            Save(History, "history-" + name + ".dat");
        }

        public static void LoadState(string name)
        {
            Queue = Load("queue-" + name + ".dat");
            History = Load("history-" + name + ".dat");
        }


        private static void Save(Dictionary<string, bool> dictionary, string filename)
        {
            var fs = new FileStream(filename,FileMode.Create); 
            var b = new BinaryFormatter();
            b.Serialize(fs, dictionary);
            fs.Close();  
        }

        private static Dictionary<string, bool> Load(string filename)
        {
            var fs = new FileStream(filename, FileMode.Open); 
            var b = new BinaryFormatter();
            var dictionary = (Dictionary<string, bool>)b.Deserialize(fs);
            fs.Close();
            return dictionary;
        }


    }
}
