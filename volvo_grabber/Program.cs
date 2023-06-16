using Microsoft.Extensions.Options;
using PuppeteerSharp;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

internal class Program
{
    static LaunchOptions options = new LaunchOptions()
    {
        Headless = false,
        ExecutablePath = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe",
        Product = Product.Chrome,
        DefaultViewport = new ViewPortOptions()
        {
            Height = 1440,
            Width = 2160,
            IsLandscape = true,
            IsMobile = false,
        }
    };
    private static async Task Main(string[] args)
    {
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


        foreach (var modelURI in modelsURIList)
        {
            var browser = Puppeteer.LaunchAsync(options).Result;
            await GetHtmlHeadLess(modelURI, browser);
        }

        //List<Task> tasks = new List<Task>();

        //Parallel.ForEach(modelsURIList, async modelURI =>
        //{
        //    Task task = Task.Run(async () =>
        //    {
        //        var browser = Puppeteer.LaunchAsync(options).Result;
        //        await GetHtmlHeadLess(modelURI, browser);
        //    });

        //    tasks.Add(task);
        //});


        //await Task.WhenAll(tasks);
        stopwatch.Stop();

        Console.WriteLine($"Elapsed: {stopwatch.Elapsed}");

    }

    static async Task GetHtmlHeadLess(string url, PuppeteerSharp.IBrowser browser)
    {
        var pages = await browser.PagesAsync();
        var page = await pages.First().GoToAsync(url);
        var content = await page.TextAsync();
        

        //Console.WriteLine(content);
        string name = GetNameFromUrlAsync(url);
        SaveHtmlContentAsync(content, name);

        await browser.CloseAsync();
    }

    private static string GetNameFromUrlAsync(string url)
    {
        return url.Split('/')[5];
    }

    private static void SaveHtmlContentAsync(string htmlContent, string name)
    {
        string htmlFilePath = $"../../../res/{name}/{name}.html";
        
        Directory.CreateDirectory($"../../../res/{name}");
        htmlContent = htmlContent.Replace("style=\"opacity:0\"", "");
        File.WriteAllText(htmlFilePath, htmlContent);
    }
}