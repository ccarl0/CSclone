using AngleSharp.Dom;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using PuppeteerSharp;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml.Linq;


namespace volvo_model_kb_grabber;

internal class Program
{
    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    public const byte VK_CONTROL = 0x11;
    public const byte VK_S = 0x53;
    public const byte VK_RETURN = 0x0D;
    public const uint KEYEVENTF_KEYUP = 0x2;


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
            IsMobile = true,

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

        List<Task> tasks = new();
        Stopwatch stopwatch = Stopwatch.StartNew();


        // Define the parallel limit
        int parallelLimit = 1; // Set the desired number of parallel tasks

        // Create a semaphore to control the parallelis
        SemaphoreSlim semaphore = new SemaphoreSlim(parallelLimit);

        foreach (var modelURI in modelsURIList)
        {
            Task task = Task.Run(async () =>
            {
                await semaphore.WaitAsync();

                Thread.Sleep(1000);
                var browser = await Puppeteer.LaunchAsync(options);
                Thread.Sleep(1000);
                await browser.PagesAsync().Result[0].GoToAsync(modelURI);
                await Console.Out.WriteLineAsync(modelURI);


                Thread.Sleep(3000);
                // Simulate pressing Ctrl
                keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);

                // Simulate pressing S
                keybd_event(VK_S, 0, 0, UIntPtr.Zero);

                // Simulate releasing S
                keybd_event(VK_S, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

                // Simulate releasing Ctrl
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

                Thread.Sleep(2000);
                // Simulate pressing Enter
                keybd_event(VK_RETURN, 0, 0, UIntPtr.Zero);
                Thread.Sleep(100);
                // Simulate releasing Enter
                keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

                Thread.Sleep(3000);

                semaphore.Release();
                Thread.Sleep(20000);
                await browser.CloseAsync();

            });
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        //await Task.WhenAll(tasks);
        stopwatch.Stop();

        Console.WriteLine($"Elapsed: {stopwatch.Elapsed}");




        // html manipulation

        string directoryPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
        string[] htmlFiles = Directory.GetFiles(directoryPath, "*.html");

        foreach (string htmlFile in htmlFiles)
        {
            ManipulateFile(htmlFile);
        }
    }

    private static void ManipulateFile(string htmlFile)
    {
        HtmlDocument doc = new HtmlDocument();
        doc.Load(htmlFile);


        List<string> xPathList = new List<string>()
        {
            "//*[@id='onetrust-consent-sdk']", // XPath for cookie banner
            "//div[nav[@id='site-navigation']]", // top bar
            "//div[@data-autoid = 'PdpSubmenu']", // top subbar
            "//button[contains( text(),'Sfoglia')]", // sfoglia la galley
            "//a[contains( text(),'Esplora')]", // sfoglia la gallery
            //"//*[@id='__NEXT_DATA__']",

            "//div[@id='levelComparison']",
            "//section[@data-autoid='Electrification']",
            "//div[@id='imageWithTextAndMarketingLinks']",
            "//div[div[div[div[section[@data-autoid='promotions']]]]]",
            "//div[div[div[div[section[@data-autoid='faqs']]]]]",
            "//div[@id='vcc-site-footer']", // footer

            //disclaimer
            "//section[@data-autoid='disclaimer']",
            "//p[contains( text(), 'funziona')]",

        };

        int i = 0;
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
                        Console.Write($"{i}Removed:\t");
                        Console.WriteLine(xPath);
                        doc.Save(htmlFile);

                    }
                }
            }
        }

        Console.WriteLine(htmlFile);
    }
}
