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

        // grab js
        await SaveJsContentAsync(content, url, name);

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
        htmlContent = htmlContent.Replace("style=\"opacity:0\"", ""); // not definitive
        File.WriteAllText(htmlFilePath, htmlContent);
    }



    private static async Task SaveJsContentAsync(string content, string url, string name)
    {
        string jsFilePath = $"../../../res/{name}/script.js";
        var jsContent = await DownloadJavaScriptAsync(content, url);
        File.WriteAllText(jsFilePath, jsContent);
    }


    static async Task<string> DownloadJavaScriptAsync(string htmlContent, string baseUrl)
    {
        string javascriptUrl = ExtractJavaScriptUrl(htmlContent);
        if (!string.IsNullOrEmpty(javascriptUrl))
        {
            if (!Uri.IsWellFormedUriString(javascriptUrl, UriKind.Absolute))
            {
                javascriptUrl = new Uri(new Uri(baseUrl), javascriptUrl).AbsoluteUri;
            }
            return await DownloadContentAsync(javascriptUrl);
        }

        return string.Empty;
    }

    static string ExtractJavaScriptUrl(string htmlContent)
    {

        int startIndex = htmlContent.IndexOf("<script src=\"");
        if (startIndex != -1)
        {
            startIndex += "<script src=\"".Length;
            int endIndex = htmlContent.IndexOf("\"", startIndex);
            if (endIndex != -1)
            {
                Console.WriteLine(htmlContent.Substring(startIndex, endIndex - startIndex));
                return htmlContent.Substring(startIndex, endIndex - startIndex);
            }
        }

        return string.Empty;
    }
    static async Task<string> DownloadContentAsync(string url)
    {

        
        // da fare con l'headless





        var client = new HttpClient();

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        await Console.Out.WriteLineAsync(url);
        var response = await client.SendAsync(request);
        //Console.WriteLine(await response.Content.ReadAsStringAsync());
        return await response.Content.ReadAsStringAsync();
    }
}