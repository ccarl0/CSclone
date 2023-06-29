using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager;
using AngleSharp.Attributes;
using HtmlAgilityPack;
using OpenQA.Selenium.Support.UI;
using System.Net;
using System.Diagnostics;
using System.Net.Http;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("Mercedes models grabber started!"); 

        string modelsPathString = "../../../assets/models.txt";

        List<string> modelsURIList = new List<string>();

        try
        {
            modelsURIList = File.ReadAllLines(modelsPathString).ToList();
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"File not found: {modelsPathString}");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"An error occurred while reading the file: {ex.Message}");
        }

        Stopwatch stopwatch = Stopwatch.StartNew();

        List<Task> tasks = new List<Task>();
        int maxParallelTasks = 4;
        SemaphoreSlim semaphore = new SemaphoreSlim(maxParallelTasks); 
        
        foreach (var url in modelsURIList)
        {
            Task task = Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                new DriverManager().SetUpDriver(new ChromeConfig());
                ChromeDriver driver = new ChromeDriver();

                driver.Navigate().GoToUrl(url);

                string script = @"
                                var shadowRoot = arguments[0].shadowRoot;
                                if (shadowRoot) {
                                    return shadowRoot.innerHTML;
                                } else {
                                    return null;
                                }
                ";

                // finding shadowHost


                // download html file no shadow elements
                var htmlFilePath = await GetHtmlWithoutShadowAsync(url);


                HtmlDocument doc = new();
                doc.Load(htmlFilePath);

                var driverShadowHosts = driver.FindElements(By.XPath("//*[@component-id]"));

                var documentShadowHosts = doc.DocumentNode.SelectNodes("//*[@component-id]");

                int counter = 0;
                List<string> driverShadowRootHtmlContents = new();
                List<HtmlDocument> documentModifiedHtmlShadowElements = new();

                List<string> xPathShadowsList = new()
                {
                    "//body[contains(.//text(), 'Richiedi')]",
                    "//body[contains(.//text(), 'Interessato a')]",
                    "//body[.//span[contains( text(), 'Configura')]]",
                    "//body[.//a[@href='#highlight']]",
                    "//body[.//p[contains( text(), 'Registra')]]",
                    "//body[.//p[contains( text(), 'Berline')]]",
                    "//div[div[div[a[contains( text(), 'preventivo')]]]]",
                     //tiles button
                    "//header[div[span[contains( text(), 'Vai al')]]]",
                    "//header[div[span[contains( text(), 'Confronta')]]]",
                    "//header[div[span[contains( text(), 'Scarica')]]]",
                    "//header[div[span[contains( text(), 'Calcola')]]]",
                    "//ul[.//*[@href]]", // buttons like "Go to ECO Coach app"
                    "//body[.//*[contains( text(), 'Scopri')]]",
                    "//ul[.//*[contains( text(), 'Ricarica')]]",
                    "//body[.//button[@id='button-focused']]"
                };

                // for each host, run script to extract shadow as html content <string>, add to list (dunno why) save a node as html file.
                foreach (var shadowHost in driverShadowHosts)
                {
                    string driverSadowRootHtmlContent = (string)driver.ExecuteScript(script, shadowHost);
                    //Thread.Sleep(500);
                    HtmlDocument shadowDocument = new();
                    if (driverSadowRootHtmlContent != null)
                    {

                        await Console.Out.WriteLineAsync("a");
                        shadowDocument.LoadHtml(driverSadowRootHtmlContent);

                        foreach (var xPath in xPathShadowsList)
                        {
                            var nodesToRemove = shadowDocument.DocumentNode.SelectNodes(xPath);
                            if (nodesToRemove != null)
                            {
                                foreach (var node in nodesToRemove)
                                {
                                    if (node != null)
                                    {
                                        node.Remove();
                                    }
                                }
                            }
                        }
                    }

                    documentModifiedHtmlShadowElements.Add(shadowDocument);
                }

                int ShadowHostIndex = 0;

                foreach (var documentShadowHost in documentShadowHosts) documentShadowHost.AppendChild(documentModifiedHtmlShadowElements[ShadowHostIndex++].DocumentNode);

                doc = CleanHtml(doc); // not inside the shadow asses


                doc.Save(htmlFilePath);

                driver.Close();

                semaphore.Release();
            });

            tasks.Add(task);
        }
        await Task.WhenAll(tasks);

        stopwatch.Stop();

        await Console.Out.WriteLineAsync("Finished");
        Console.WriteLine($"\n\n\nTotal execution time: {stopwatch.Elapsed}");
    }

    private static HtmlDocument ManipulateShadowDoc(HtmlDocument shadowDoc)
    {
        List<string> xPathList = new List<string>()
        {
            
        };

        foreach (var xPath in xPathList)
        {
            var nodes = shadowDoc.DocumentNode.SelectNodes(xPath);
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    if (node != null)
                    {
                        node.Remove();
                    }
                }
            }
        }
        
        return shadowDoc;
    }

    private static HtmlDocument CleanHtml(HtmlDocument doc)
    {
        List<string> xPathList = new List<string>()
        {
            "//owc-vertical-navigation",
            "//div[div[iframe]]\r\n",
            "//owc-footer",
            "//owc-stage[@data-anchor-id='contact']",
            "//owc-next-best-activities",
            "//owc-banner-teaser",
            "//owc-subnavigation",
            "//eqpodc-flyout-button",
            "//fss-search-input",
            "//button[@aria-label='Menu']",
            "//button[@aria-label='menu']",
            "//iam-user-menu",
            "//fss-search-input"
        };

        doc = RemoveXPaths(doc, xPathList);

        return doc;
    }

    private static HtmlDocument RemoveXPaths(HtmlDocument doc, List<string> xPathList)
    {
        if (xPathList != null)
        {
            foreach (var xPath in xPathList)
            {
                var nodes = doc.DocumentNode.SelectNodes(xPath);

                if (nodes != null)
                {
                    foreach (var node in nodes)
                    {
                        if (node != null)
                        {
                            node.Remove();
                        }
                    }
                }
            }
        }

        return doc;
    }

    private static HtmlNode ConvertWebElement2HtmlNode(IWebElement webElement)
    {
        string htmlContent = webElement.GetAttribute("innerHTML");

        HtmlDocument htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(htmlContent);

        HtmlNode htmlNode = htmlDoc.DocumentNode;

        

        return htmlNode;
    }


    private static async Task<string> GetHtmlWithoutShadowAsync(string url)
    {
        string htmlFilePath = $"../../../res/{GetNameFromUrl(url)}/{GetNameFromUrl(url)}.html";

        Directory.CreateDirectory($"../../../res/{GetNameFromUrl(url)}");

        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://www.mercedes-benz.it/passengercars/models/saloon/eqe/overview.html/");
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        File.WriteAllText(htmlFilePath, await response.Content.ReadAsStringAsync());

        return htmlFilePath;
    }

    private static string GetNameFromUrl(string imgUrl)
    {
        return imgUrl.Split("models/")[1].Replace("/", "-").Split(".html")[0];
    }

    private static void DownloadImage(string imageUrl, int i)
    {
        using (WebClient webClient = new WebClient())
        {
            webClient.DownloadFile(imageUrl, $"../../../res/{i.ToString()}.png");
        }
    }
}