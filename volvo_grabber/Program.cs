using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using PuppeteerSharp;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
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

        

        Stopwatch stopwatch = Stopwatch.StartNew();


        foreach (var modelURI in modelsURIList)
        {
            var browser = Puppeteer.LaunchAsync(options).Result;
            await GetPageHeadlessAsync(modelURI, browser);
        }

        

        //await Task.WhenAll(tasks);
        stopwatch.Stop();

        Console.WriteLine($"Elapsed: {stopwatch.Elapsed}");

    }

    static async Task GetPageHeadlessAsync(string url, PuppeteerSharp.IBrowser browser)
    {
        var page = await browser.NewPageAsync();
        await page.GoToAsync(url);

        //var page = await pages.First().GoToAsync(url);

        
        //Thread.Sleep(10000);
        await Console.Out.WriteLineAsync( "10 seconds passed ");
        
        var content = await page.GetContentAsync();

        //Console.WriteLine(content);
        string name = GetNameFromUrlAsync(url);
        SaveHtmlContentAsync(content, name);

        // grab js
        await SaveJsAsync(content, url, name, browser);

        //grab css
        await SaveCssAsync(content, url, name, browser);


        await browser.CloseAsync();

        // rewrite html

        // opening html one time for each rewriting
        string htmlFilePath = $"../../../res/{name}/{name}.html";

        // Load the HTML document from the file
        HtmlDocument htmlDoc = new HtmlDocument();
        htmlDoc.Load(htmlFilePath);

        // remove js
        htmlDoc = RemoveScriptTags(htmlDoc);

        // remove css relative tags
        htmlDoc = RemoveCssTags(htmlDoc);

        // add js and css
        htmlDoc = AddTags(htmlDoc);




        List<string> xpaths = new List<string>
        { "//a[@data-autoid='pdpSubmenu:cta']",
          "//*[@id='stats']/div/div/section/div[1]/div[2]/div[4]",
          "//*[@id='colorSelector']/div/div/section[1]/div[4]",
          "//*[@id='colorSelector']/div/div/section[2]",
          "//*[@id='__next']/div/div[15]/div/div/section",
          "//*[@data-autoid='Electrification']",
          "//*[@data-autoid='disclaimer']",
          "//*[@id='faqs']",
          "//*[@id='imageWithTextAndMarketingLinks']",
          "//*[@id='levelComparison']",
          "//*[@id='vcc-site-footer-shadow-container']",
          "//*[@id='sitenav:topbar']",
          //"//*[@id='colorSelector']",
          "//*[@id='onetrust-consent-sdk']" };

        foreach (var xpath in xpaths)
        {
            htmlDoc = RemoveElementsByXPath(htmlDoc, xpath);
        }


        // saving doc
        htmlDoc.Save(htmlFilePath);
    }

    private static HtmlDocument RemoveCssTags(HtmlDocument htmlDoc)
    {
        HtmlNodeCollection styleNodes = htmlDoc.DocumentNode.SelectNodes("//style");

        // Remove each <style> tag from the HTML document
        if (styleNodes != null)
        {
            foreach (HtmlNode styleNode in styleNodes)
            {
                styleNode.Remove();
            }
        }

        // Select <link> tags with href containing ".css"
        HtmlNodeCollection linkNodes = htmlDoc.DocumentNode.SelectNodes("//link[contains(@href, '.css')]");

        // Remove each <link> tag from the HTML document
        if (linkNodes != null)
        {
            foreach (HtmlNode linkNode in linkNodes)
            {
                linkNode.Remove();
            }
        }

        return htmlDoc;
    }

    static HtmlDocument RemoveElementsByXPath(HtmlDocument htmlDoc, string xpath)
    {
        // Select elements based on the XPath
        HtmlNodeCollection elements = htmlDoc.DocumentNode.SelectNodes(xpath);

        // Remove the selected elements from the HTML document
        if (elements != null)
        {
            foreach (HtmlNode element in elements)
            {
                element.Remove();
            }
        }

        return htmlDoc;
    }

    private static HtmlDocument RemoveScriptTags(HtmlDocument htmlDoc)
    {
        HtmlNodeCollection scriptNodes = htmlDoc.DocumentNode.SelectNodes("//script[@src]");

        // Remove each <script> tag from the HTML document
        if (scriptNodes != null)
        {
            foreach (HtmlNode scriptNode in scriptNodes)
            {
                scriptNode.Remove();
            }
        }

        return htmlDoc;
    }

    private static HtmlDocument AddTags(HtmlDocument htmlDoc)
    {
        // Create a new <script> element
        HtmlNode scriptNode = htmlDoc.CreateElement("script");
        scriptNode.SetAttributeValue("type", "text/javascript");
        scriptNode.SetAttributeValue("src", "script.js");

        // Append the <script> element to the document's <head> element
        HtmlNode headNode = htmlDoc.DocumentNode.SelectSingleNode("//head");
        
        headNode.AppendChild(scriptNode);

        // css
        HtmlNode linkNode = htmlDoc.CreateElement("link");

        // Set the attributes of the <link> node
        linkNode.Attributes.Add("rel", "stylesheet");
        linkNode.Attributes.Add("type", "text/css");
        linkNode.Attributes.Add("href", "styles.css");

        // Append the <link> node to the document's head
        headNode = htmlDoc.DocumentNode.SelectSingleNode("//head");

        headNode.AppendChild(linkNode);

        // Save the modified HTML document
        return htmlDoc;
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



    private static async Task SaveJsAsync(string content, string url, string name, PuppeteerSharp.IBrowser browser)
    {
        string jsFilePath = $"../../../res/{name}/script.js";
        List<string> scriptSrcList = new();
        
        // fetch src for each script
        scriptSrcList = GetAllScriptTags(name);
        
        // manipulate srcs
        scriptSrcList = ManipualateSrcAsync(scriptSrcList);
        File.WriteAllLines($"../../../res/{name}/src.txt", scriptSrcList);

        // get js headlessly
        foreach (var javascriptUrl in scriptSrcList)
        {
            string jsContent = await GetFromUrlHeadlesslyAsync(javascriptUrl, browser);
            await File.AppendAllTextAsync(jsFilePath, jsContent);
            //Thread.Sleep(2000);
        }
    }

    private static async Task<string> GetFromUrlHeadlesslyAsync(string javascriptUrl, IBrowser browser)
    {
        //var pages = await browser.PagesAsync();
        //var page = await pages.First().GoToAsync(javascriptUrl);
        //var content = await page.TextAsync();

        //var page = await browser.NewPageAsync();
        //await page.GoToAsync(javascriptUrl);
        //var content = await page.GetContentAsync();

        var disposableBrowser = await Puppeteer.LaunchAsync(options);
        var pages = await disposableBrowser.PagesAsync();
        var page = pages.FirstOrDefault();
        await page.GoToAsync(javascriptUrl);
        var content = await page.GetContentAsync();
        await disposableBrowser.CloseAsync();

        return content;
    }

    private static List<string> ManipualateSrcAsync(List<string> scriptsrclist)
    {

        for (int i = 0; i < scriptsrclist.Count; i++)
        {
            if (scriptsrclist[i].StartsWith("/static"))
            {
                string src = scriptsrclist[i];
                scriptsrclist[i] = $"https://www.volvocars.com/{src}";
            }
        }

        return scriptsrclist;
    }

    private static List<string> GetAllScriptTags(string name)
    {
        string htmlFilePath = $"../../../res/{name}/{name}.html";

        HtmlDocument htmlDoc = new HtmlDocument();
        htmlDoc.Load(htmlFilePath);

        // Select all <script> tags in the HTML document
        IEnumerable<HtmlNode> scriptNodes = htmlDoc.DocumentNode.Descendants("script");

        // Iterate over each <script> tag and retrieve the src attribute
        List<string> scriptSrcList = new List<string>();
        foreach (HtmlNode scriptNode in scriptNodes)
        {
            string srcAttributeValue = scriptNode.GetAttributeValue("src", string.Empty);
            if (!string.IsNullOrWhiteSpace(srcAttributeValue))
            {
                scriptSrcList.Add(srcAttributeValue);
            }
        }

        return scriptSrcList;
    }

    private static async Task SaveCssAsync(string content, string url, string name, PuppeteerSharp.IBrowser browser)
    {
        string cssFilePath = $"../../../res/{name}/styles.css";
        List<string> linkHrefList = new();

        // fetch src for each script
        linkHrefList = GetAllLinkTags(name);

        // manipulate href
        linkHrefList = ManipualateHrefAsync(linkHrefList);
        File.WriteAllLines($"../../../res/{name}/href.txt", linkHrefList);

        // get css headlessly
        foreach (var linkHref in linkHrefList)
        {
            string cssContent = await GetFromUrlHeadlesslyAsync(linkHref, browser);
            await File.AppendAllTextAsync(cssFilePath, cssContent);
            //Thread.Sleep(2000);
        }

        // style tags
        GetStyleContent(name);
    }

    private static void GetStyleContent(string name)
    {
        string htmlFilePath = $"../../../res/{name}/{name}.html";
        string cssFilePath = $"../../../res/{name}/styles.css";

        HtmlDocument htmlDoc = new HtmlDocument();
        htmlDoc.Load(htmlFilePath);

        HtmlNodeCollection styleNodes = htmlDoc.DocumentNode.SelectNodes("//style");

        List<string> styleContentList = new();

        if (styleNodes != null)
        {
            foreach (HtmlNode styleNode in styleNodes)
            {
                // Get the content of the <style> tag
                string styleContent = styleNode.InnerHtml;
                styleContentList.Add(styleContent);
            }
        }

        File.AppendAllLinesAsync(cssFilePath, styleContentList);
    }

    private static List<string> ManipualateHrefAsync(List<string> linkHrefList)
    {
        for (int i = 0; i < linkHrefList.Count; i++)
        {
            if (linkHrefList[i].StartsWith("/static"))
            {
                string href = linkHrefList[i];
                linkHrefList[i] = $"https://www.volvocars.com/{href}";
            }
        }

        return linkHrefList;
    }

    private static List<string> GetAllLinkTags(string name)
    {
        string htmlFilePath = $"../../../res/{name}/{name}.html";

        HtmlDocument htmlDoc = new HtmlDocument();
        htmlDoc.Load(htmlFilePath);

        // Select all <link> <style> tags in the HTML document
        HtmlNodeCollection linkNodes = htmlDoc.DocumentNode.SelectNodes("//link[contains(@href, '.css')]");

        // Iterate over each <script> tag and retrieve the src attribute
        List<string> hrefLinktNodes = new List<string>();
        foreach (var linktNode in linkNodes)
        {
            string hrefAttributeValue = linktNode.GetAttributeValue("href", string.Empty);
            if (!string.IsNullOrWhiteSpace(hrefAttributeValue))
            {
                hrefLinktNodes.Add(hrefAttributeValue);
            }
        }

        return hrefLinktNodes;
    }
}
