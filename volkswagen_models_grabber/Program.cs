using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Diagnostics;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.DevTools;
using System.Xml;

internal class Program
{
    private static async Task Main(string[] args)
    {

        Console.WriteLine("Grabber started!");


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

        int maxParallelTasks = 3;
        SemaphoreSlim semaphore = new SemaphoreSlim(maxParallelTasks); // Set the maximum parallel tasks

        foreach (var url in modelsURIList)
        {
            await semaphore.WaitAsync();

            Task task = Task.Run(async () =>
            {
                // getting model name
                string name = GetModelName(url);

                // declaring paths based on model name
                string htmlDirPath = $"../../../res/{name}";
                string htmlFilePath = $"../../../res/{name}/{name}.html";
                string cssFilePath = $"../../../res/{name}/styles.css";
                string jsFilePath = $"../../../res/{name}/script.js";

                //get html content
                var htmlContent = await GetHtmlAsync(url);

                semaphore.Release();

                //save htmlContent
                Directory.CreateDirectory(htmlDirPath);
                Directory.CreateDirectory($"{htmlDirPath}/img"); // imgs directory
                await File.WriteAllTextAsync(htmlFilePath, htmlContent);

                //// grab css
                var cssContent = GrabCssAsync(htmlFilePath);

                // save css content
                try
                {
                    await File.AppendAllLinesAsync(cssFilePath, cssContent);

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                // grab js
                var hugeScriptList = await GrabJsAsync(htmlFilePath);

                // save js content
                try
                {
                    await File.AppendAllLinesAsync(jsFilePath, hugeScriptList);
                }
                catch (Exception e)
                {
                    await Console.Out.WriteLineAsync(e.Message);
                }

                //clean html
                List<string> xPathList = new List<string>()
                {
                    "//div[@class=\"StyledBackgroundShim-jtfaQb hHHWof\"]", // top nav bar
                    "//div[div[a[@title='Richiedi preventivo']]]", // richiedi preventivo 
                    "//div[a[@title='Configuratore']] | //div[a[@title='Configura']]", // configuratore 
                    "//nav[ol[li[div[a[span[div[span[contains(text(), 'Home')]]]]]]]]", // lil writes under buttons
                    "//div[@class='StyledLinkWrapper-bEHVV kyDJWM']", // lil writes under buttons
                    "//section[@id='promozioni']", // empty block for promotions
                    "//div[div[section[div[div[div[div[a[@title='Richiedi preventivo']]]]]]]]", // Richiedi preventivo
                    "//div[div[a[div[span[contains ( text(), 'Configura')]]]]]", // Configura multivan
                    "//footer",
                    "//header"
                };
                HtmlDocument doc = new();
                doc.Load(htmlFilePath);
                foreach (var xPath in xPathList)
                {
                    RemoveXPath(doc, xPath);
                }
                doc = CleanHtml(doc);  // breaks caddy

                doc = RemoveEqualXPath(doc, "//p[contains( text(), 'Fonte')]");
                doc = RemoveEqualXPath(doc, "//a[@href and not(@tabindex='-1') and not(@tabindex='0')]");

                // rewrite all href
                doc = RemoveHref(doc);



                doc = await SaveImageAsync(doc, name);

                doc.Save(htmlFilePath);
            });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        //Console.WriteLine("Adding references");

        stopwatch.Stop();
        Console.WriteLine($"\n\n\nTotal execution time: {stopwatch.Elapsed}");
    }

    private static async Task<HtmlDocument> SaveImageAsync(HtmlDocument doc, string name)
    {
        HtmlNodeCollection imgNodes = doc.DocumentNode.SelectNodes("//source[@srcset and @media='(min-aspect-ratio: 4/3)'] | //source[@srcset and @media='(min-width: 560px)'] | //img[@src]");

        if (imgNodes != null)
        {
            var counter = 0;
            foreach (HtmlNode imgNode in imgNodes)
            {
                string imageUrl = imgNode.GetAttributeValue("srcset", "");
                if (!string.IsNullOrWhiteSpace(imageUrl))
                {
                    imageUrl = imageUrl.Trim().Split("2400w,")[1].Split("2880w")[0];

                    var imgClient = new HttpClient();
                    var imgRequest = new HttpRequestMessage(HttpMethod.Get, imageUrl);
                    await Console.Out.WriteLineAsync(imageUrl);
                    Thread.Sleep(200);
                    try
                    {
                        var imgResponse = await imgClient.SendAsync(imgRequest);
                        imgResponse.EnsureSuccessStatusCode();

                        using (Stream stream = await imgResponse.Content.ReadAsStreamAsync())
                        using (FileStream fileStream = new FileStream($"../../../res/{name}/img/{counter++}.png", FileMode.Create))
                        {
                            await stream.CopyToAsync(fileStream);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine();
                        Console.WriteLine();
                        Console.WriteLine();
                        Console.WriteLine();
                        Console.WriteLine();
                        Console.WriteLine(e.Message);
                        Console.WriteLine();
                        Console.WriteLine();
                        Console.WriteLine();

                    }
                    

                    //imgNode.SetAttributeValue("srcset", $"img/{counter}.png");
                }

                // naming system
                //string imageName = imgNode.GetAttributeValue("alt", "");
                //if (!string.IsNullOrWhiteSpace(imageName))
                //{
                //    name = $"{imageName}.png";
                //    string imageNamesFilePath = $"../../../{name}";
                //}
                //else
                //{
                //    imageName = "None";
                //    numberedName++;
                //    string imageNamesFilePath = $"../../../{numberedName}";
                //}
            }
        }

        return doc;
    }

    private static HtmlDocument RemoveHref(HtmlDocument doc)
    {
        var nodes = doc.DocumentNode.SelectNodes("//a[@href]");

        if (nodes != null)
        {
            foreach (var node in nodes)
            {
                node.Attributes.Remove("href");
            }
        }

        return doc;
    }

    private static HtmlDocument RemoveEqualXPath(HtmlDocument doc, string xPath)
    {
        var targetNodes = doc.DocumentNode.SelectNodes(xPath);
        if (targetNodes != null)
        {
            foreach (var node in targetNodes)
            {
                node.Remove();
            }
        }

        return doc;
    }

    private static HtmlDocument CleanHtml(HtmlDocument doc)
    {
        try
        {
            var targetDiv = doc.DocumentNode.SelectSingleNode($"//div[div[section[div[div[div[div[div[div[div[div[div[div[div[div[span[p[span[a[span[contains( text(), 'Vai a')]]]]]]]]]]]]]]]]]]]]");

            if (targetDiv != null)
            {
                var followingDivs = targetDiv.SelectNodes("following-sibling::div");
                if (followingDivs != null)
                {
                    foreach (HtmlNode div in followingDivs)
                    {
                        div.Remove();
                    }
                }
                targetDiv.Remove();
            }

            targetDiv = doc.DocumentNode.SelectSingleNode($"//div[div[section[@id='download']]]");

            if (targetDiv != null)
            {
                var followingDivs = targetDiv.SelectNodes("following-sibling::div");
                if (followingDivs != null)
                {
                    foreach (HtmlNode div in followingDivs)
                    {
                        div.Remove();
                    }
                }
                targetDiv.Remove();
            }

            //var targetDiv = doc.DocumentNode.SelectSingleNode($"//div[div[section[@id='versioni']] | div[div[section[@id='scegli-la-versione']]]]");
            targetDiv = doc.DocumentNode.SelectSingleNode($"//div[div[section[@id='scegli-la-versione']]] | //div[div[section[@id='versioni']]]");

            if (targetDiv != null)
            {
                var followingDivs = targetDiv.SelectNodes("following-sibling::div");
                if (followingDivs != null)
                {
                    foreach (HtmlNode div in followingDivs)
                    {
                        div.Remove();
                    }
                }
                targetDiv.Remove();
            }

            return doc;
        }
        catch (Exception e)
        {
            Console.WriteLine(doc.GetHashCode());
            Console.WriteLine(e.Message);
        }

        return doc;
    }

    private static HtmlDocument RemoveXPath(HtmlDocument doc, string xPath)
    {
        HtmlNode nodeToRemove = doc.DocumentNode.SelectSingleNode(xPath);

        if (nodeToRemove != null)
        {
            nodeToRemove.Remove();
        }

        return doc;
    }

    private static async Task<List<string>> GrabJsAsync(string htmlFilePath)
    {
        List<string> scriptUrlList = new();

        HtmlDocument doc = new();
        doc.Load(htmlFilePath);

        HtmlNodeCollection scriptNodes = doc.DocumentNode.SelectNodes("//script[@src]");
        if (scriptNodes != null)
        {
            foreach (HtmlNode scriptNode in scriptNodes)
            {
                string src = scriptNode.GetAttributeValue("src", "");
                scriptUrlList.Add(src);
                scriptNode.Remove();
            }

            scriptNodes = doc.DocumentNode.SelectNodes("//script[@async]");

            foreach (HtmlNode scriptNode in scriptNodes)
            {
                string src = scriptNode.GetAttributeValue("async src", "");
                scriptUrlList.Add(src);
                scriptNode.Remove();
            }


            HtmlNode headNode = doc.DocumentNode.SelectSingleNode("//head");
            HtmlNode sNode = doc.CreateElement("script");
            sNode.SetAttributeValue("src", "script.js");
            headNode.AppendChild(sNode);

            HtmlNode lNode = doc.CreateElement("link");
            lNode.SetAttributeValue("rel", "stylesheet");
            lNode.SetAttributeValue("href", @"styles.css");
            lNode.SetAttributeValue("type", "text/css");
            headNode.AppendChild(lNode);
        }
        

        doc.Save(htmlFilePath);

        List<string> hugeScriptList = new();

        // download scripts
        foreach (var scriptUrl in scriptUrlList)
        {
            if (!scriptUrl.StartsWith("//javascript"))
            {
                string url = scriptUrl;
                if (scriptUrl.StartsWith("/idhub"))
                {
                    url = $"https://www.volkswagen.it{scriptUrl}";
                }
                if (scriptUrl.StartsWith("//"))
                {
                    url = $"https://{scriptUrl.Replace("//", "")}";
                }
                try
                {
                    hugeScriptList.Add(await DownloadContentAsync(url));
                }
                catch (Exception)
                {
                    await Console.Out.WriteLineAsync(url);
                }
            }
        }

        return hugeScriptList;
    }

    private static async Task<string> DownloadContentAsync(string scriptUrl)
    {
        if (scriptUrl != "")
        {
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, scriptUrl);
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var res = await response.Content.ReadAsStringAsync();

                return res;
            }
            catch (Exception e)
            {
                await Console.Out.WriteLineAsync($"{scriptUrl} said:\n{e.Message}");
            }
        }

        return string.Empty;
    }

    private static List<string> GrabCssAsync(string htmlFilePath)
    {
        List<string> stylesTagList = new();

        HtmlDocument doc = new();
        doc.Load(htmlFilePath);

        HtmlNodeCollection styleNodes = doc.DocumentNode.SelectNodes("//style");
        if (styleNodes != null)
        {
            try
            {
                Console.WriteLine(htmlFilePath);
                foreach (HtmlNode styleNode in styleNodes)
                {
                    string innerContent = styleNode.InnerHtml;
                    stylesTagList.Add(innerContent);

                    // remove tag
                    styleNode.Remove();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        


        doc.Save(htmlFilePath);

        return stylesTagList;
    }

    static string GetModelName(string url)
    {
        string name = url.Split('/').Last().Split(".").First();

        return name;
    }


    static async Task<string> GetHtmlAsync(string url)
    {
        try
        {
            var client = new HttpClient();

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception e)
        {
            await Console.Out.WriteLineAsync(url);
            await Console.Out.WriteLineAsync(e.Message);
        }

        return null;


        // headless mode messes with css and stuff

        //new DriverManager().SetUpDriver(new ChromeConfig());
        //ChromeDriver driver = new ChromeDriver();

        //driver.Navigate().GoToUrl(url);

        //WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        //wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
        //Console.WriteLine("waited");
        //Thread.Sleep(8000);

        //string pageSource = driver.PageSource;

        //driver.Quit();

        //return pageSource;
    }
}