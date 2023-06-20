using PuppeteerSharp;
using PuppeteerSharp.Input;
using System;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using MouseButton = PuppeteerSharp.Input.MouseButton;
using System.Security.Principal;
using System.Runtime.InteropServices;
using HtmlAgilityPack;

class Program
{
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern void SendKeys(string keys);

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
    static async Task Main(string[] args)
    {
        List<string> imgUrls = new();

        var browser = await Puppeteer.LaunchAsync(options);
        var page = await browser.NewPageAsync();
        await page.GoToAsync("https://www.volvocars.com/it/l/modelli/");
        Thread.Sleep(1500);
        await ScrollPageDown(page, 2);
        var content = await page.GetContentAsync();
        await File.WriteAllTextAsync("../../../res/index.html", content);
        await browser.CloseAsync();



        HtmlDocument doc = new HtmlDocument();
        doc.Load("../../../res/index.html"); // Replace "input.html" with your HTML file path or load the HTML from a string using doc.LoadHtml(htmlString)

        // Select all img tags
        HtmlNodeCollection imgTags = doc.DocumentNode.SelectNodes("//img");

        foreach (HtmlNode imgTag in imgTags)
        {
            // Get the src attribute value
            string src = imgTag.GetAttributeValue("src", "");
            if (!src.Contains(".svg"))
            {
                // paring url
                var url = src.Split("?").FirstOrDefault();
                await Console.Out.WriteLineAsync(url);
                imgUrls.Add(url);

            }
        }


        foreach (var imgUrl in imgUrls)
        {
            var disposableBrowser = await Puppeteer.LaunchAsync(options);
            var disposablePage = await disposableBrowser.NewPageAsync();
            await disposablePage.GoToAsync(imgUrl);
            await disposablePage.ClickAsync("body", new ClickOptions() { Button = MouseButton.Right });
            Thread.Sleep(500);
            SimulateKeyPress(0x28); // 0x28 is the virtual key code for Down arrow
            Thread.Sleep(500);
            SimulateKeyPress(0x28); // 0x28 is the virtual key code for Down arrow
            Thread.Sleep(1500);
            //SimulateKeyPress(0x28); // 0x28 is the virtual key code for Down arrow
            //Thread.Sleep(1000);

            SimulateKeyPress(0x0D); 
            Thread.Sleep(1500);

            SimulateKeyPress(0x0D);
            Thread.Sleep(1500);
            await disposableBrowser.CloseAsync();
        }
        File.WriteAllLines("../../../res/urls.txt", imgUrls);
    }

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte vk, byte scan, int flags, int extraInfo);

    private const int KEYEVENTF_EXTENDEDKEY = 0x1;
    private const int KEYEVENTF_KEYUP = 0x2;

    private static void SimulateKeyPress(byte keyCode)
    {
        keybd_event(keyCode, 0, KEYEVENTF_EXTENDEDKEY, 0);
        keybd_event(keyCode, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
    }

    private static async Task ScrollPageDown(IPage page, int times)
    {
        for (int i = 0; i < times; i++)
        {
            await page.EvaluateExpressionAsync("window.scrollBy(0, window.innerHeight)");
            await page.WaitForTimeoutAsync(800);
        }
    }
}
