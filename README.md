# WebCrawler
A Web Spider based using HTMLAgilityPack

### Sample Usage
```csharp

  Spider.OnQueueUpdate = q =>
  {   
      Console.WriteLine("Crawler Queue updated : " + q);
      if (q == 0)
      {
          // when this reaches 0, then the crawl is complete
          Console.WriteLine("Crawl Complete");
      }
  };
  Spider.OnVisitedPage = (webpage,content) =>
  {
      Console.WriteLine("Crawler visited : " + webpage.Url);
  };
  Spider.OnCrawlError = (webpage, ex) =>
  {
      Console.WriteLine("Crawler hit error at : " + webpage.Url);
  };
  Spider.StartPage = new Uri("https://www.cloudansweringmachine.com/");
  Spider.Scope = "https://www.cloudansweringmachine.com/"; // Don't leave this domain.
  Spider.Start();
  Console.WriteLine("Crawl stated, press enter to stop.");
  Console.ReadLine();

```
