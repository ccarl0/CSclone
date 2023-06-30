using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Diagnostics;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;

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
        int maxParallelTasks = 3;
        SemaphoreSlim semaphore = new SemaphoreSlim(maxParallelTasks);

        // js script to be inject ot extract inner
        string script = @"
                                var shadowRoot = arguments[0].shadowRoot;
                                if (shadowRoot) {
                                    return shadowRoot.innerHTML;
                                } else {
                                    return null;
                                }
                ";


        new DriverManager().SetUpDriver(new ChromeConfig());



        foreach (var url in modelsURIList)
        {
            Task task = Task.Run(async () =>
            {
                await semaphore.WaitAsync();

                ChromeDriver driver = new ChromeDriver();

                driver.Navigate().GoToUrl(url);

                var driverShadowHosts = driver.FindElements(By.XPath("//*[@component-id]"));


                List<HtmlDocument> htmlShadowDocumentList = new();

                foreach (var driverShadowHost in driverShadowHosts)
                {
                    string driverShadowRootHtmlContent = (string)driver.ExecuteScript(script, driverShadowHost);

                    if (driverShadowRootHtmlContent != null)
                    {
                        HtmlDocument htmlShadowDocument = new();
                        
                        htmlShadowDocument.LoadHtml(driverShadowRootHtmlContent);

                        htmlShadowDocumentList.Add(htmlShadowDocument);
                    }
                }
                driver.Close();

                semaphore.Release();



                // download html file no shadow elements
                var htmlFilePath = await GetHtmlWithoutShadowAsync(url);


                HtmlDocument doc = new();
                doc.Load(htmlFilePath);


                var documentShadowHosts = doc.DocumentNode.SelectNodes("//*[@component-id]");

                // for each host in driverShadowHost add htmlShadow

                if (htmlShadowDocumentList.Count > documentShadowHosts.Count)
                {
                    Console.WriteLine("documentShadowHosts");
                    for (int i = 0; i < documentShadowHosts.Count; i++)
                    {
                        var child = htmlShadowDocumentList[i].DocumentNode;
                        documentShadowHosts[i].PrependChild(child);
                        doc.Save(htmlFilePath);
                    }
                }
                else
                {
                    Console.WriteLine("htmlShadowDocumentList");

                    if (htmlShadowDocumentList.Count == documentShadowHosts.Count)
                    {
                        Console.WriteLine("Uguale");
                    }
                    for (int i = 0; i < htmlShadowDocumentList.Count; i++)
                    {
                        var child = htmlShadowDocumentList[i].DocumentNode;
                        documentShadowHosts[i].PrependChild(child);
                        doc.Save(htmlFilePath);
                    }
                }
            });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        stopwatch.Stop();

        await Console.Out.WriteLineAsync("Finished");
        Console.WriteLine($"\n\n\nTotal execution time: {stopwatch.Elapsed}");
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
}