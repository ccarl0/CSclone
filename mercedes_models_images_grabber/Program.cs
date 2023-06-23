using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager;
using AngleSharp.Attributes;
using HtmlAgilityPack;
using OpenQA.Selenium.Support.UI;
using System.Net;
using System.Diagnostics;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("Mercedes image grabber started!");


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

        int maxParallelTasks = 5;
        SemaphoreSlim semaphore = new SemaphoreSlim(maxParallelTasks); // Set the maximum parallel tasks
        int i = 0;

        foreach (var url in modelsURIList)
        {
            Task task = Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                new DriverManager().SetUpDriver(new ChromeConfig());
                ChromeDriver driver = new ChromeDriver();

                driver.Navigate().GoToUrl(url);

                // finding shadowHost
                IWebElement shadowHost = driver.FindElement(By.CssSelector("body > div.root.responsivegrid.owc-content-container > div > div > div > owc-stage.webcomponent.aem-GridColumn.aem-GridColumn--default--12.owc-image-stage-host"));

                // find element inside shadow tree
                var element = shadowHost.GetShadowRoot()
                .FindElement(By.CssSelector("div > div.owc-stage__image > div.owc-stage-image > picture > source:nth-child(1)"));

                // getting image url
                var imgUrl = element.GetAttribute("srcset");

                imgUrl = $"https://www.mercedes-benz.it{imgUrl}";

                driver.Close();

                semaphore.Release();

                var name = GetNameFromUrl(imgUrl);

                DownloadImage(imgUrl, i++);

                await Console.Out.WriteLineAsync(imgUrl);
            });

            tasks.Add(task);
        }
        await Task.WhenAll(tasks);
        
        stopwatch.Stop();

        await Console.Out.WriteLineAsync("Finished");
        Console.WriteLine($"\n\n\nTotal execution time: {stopwatch.Elapsed}");
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