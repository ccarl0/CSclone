using HtmlAgilityPack;
using System.Diagnostics;
using System.Xml;

internal class Program
{
    private static async Task Main(string[] args)
    {
        string modelsPathString = "../../../assets/models.txt";

        List<string> modelsURLList = new List<string>();

        try
        {
            modelsURLList = File.ReadAllLines(modelsPathString).ToList();
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"File not found: {modelsPathString}");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"An error occurred while reading the file: {ex.Message}");
        }

        List<Task> tasks = new List<Task>();

        Stopwatch stopwatch = Stopwatch.StartNew();

        foreach (var modelURL in modelsURLList)
        {
            var task = Task.Run(async () =>
            {
                var htmlContent = await GrabHtmlAsync(modelURL);
                var name = GetModelName(modelURL);
                string htmlFilePath = $"../../../res/{name}/{name}.html";
                Directory.CreateDirectory($"../../../res/{name}");
                await File.WriteAllTextAsync(htmlFilePath, htmlContent);

                // grab js
                await SaveJsAsync(name);

                await SaveCssAsync(name);

                // rewrite html
                // opening html one time for each rewriting
                // Load the HTML document from the file
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.Load(htmlFilePath);

                // remove js
                htmlDoc = RemoveScriptTags(htmlDoc);

                // remove css relative tags
                htmlDoc = RemoveCssTags(htmlDoc);

                // add js and css
                htmlDoc = AddTags(htmlDoc);

                htmlDoc.Save(htmlFilePath);
            });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();
        await Console.Out.WriteLineAsync( stopwatch.Elapsed.ToString());
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

    private static async Task SaveCssAsync(string name)
    {
        string cssFilePath = $"../../../res/{name}/styles.css";
        List<string> linkHrefList = new();

        // fetch src for each script
        linkHrefList = GetAllLinkTags(name);

        // manipulate href
        linkHrefList = ManipualateHrefAsync(linkHrefList);
        File.WriteAllLines($"../../../res/{name}/href.txt", linkHrefList);

        // get css 
        foreach (var linkHref in linkHrefList)
        {
            string cssContent = await GetFromUrlAsync(linkHref);
            await File.AppendAllTextAsync(cssFilePath, cssContent);
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

    private static async Task<string> GetFromUrlAsync(string linkHref)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, linkHref);
        try
        {
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            return content;
        }
        catch (Exception e)
        {
            Console.WriteLine($"{linkHref} said:\n{e.Message}\n\n\n");
        }
        return null;
    }

    private static List<string> ManipualateHrefAsync(List<string> linkHrefList)
    {
        for (int i = 0; i < linkHrefList.Count; i++)
        {
            if (linkHrefList[i].StartsWith("/etc"))
            {
                string href = linkHrefList[i];
                linkHrefList[i] = $"https://www.mercedes-benz.it/{href}";
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





    private static string GetModelName(string modelURL)
    {
        return modelURL.Split("/")[5] + "-" + modelURL.Split("/")[6] + "-" + modelURL.Split("/").LastOrDefault().Split(".").FirstOrDefault();
    }

    private static async Task<string> GrabHtmlAsync(string modelURL)
    {
        try
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, modelURL);
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine($"{modelURL} said:\n{e.Message}\n\n\n");
        }

        return null;
    }

    private static async Task SaveJsAsync(string name)
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
            string jsContent = await GetJsAsync(javascriptUrl, name);
            await File.AppendAllTextAsync(jsFilePath, jsContent);
            //Thread.Sleep(2000);
        }
    }

    private static async Task<string> GetJsAsync(string javascriptUrl, string name)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, javascriptUrl);
        
        try
        {
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var jsContent = await response.Content.ReadAsStringAsync();
            return jsContent;

        }
        catch (Exception e)
        {
            Console.WriteLine(name);
            await Console.Out.WriteLineAsync($"{javascriptUrl} said: \n {e.Message} \n\n\n");

        }

        return null;
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

    private static List<string> ManipualateSrcAsync(List<string> scriptsrclist)
    {

        for (int i = 0; i < scriptsrclist.Count; i++)
        {
            if (scriptsrclist[i].StartsWith("/etc"))
            {
                string src = scriptsrclist[i];
                scriptsrclist[i] = $"https://www.mercedes-benz.it/{src}";
            }
        }

        return scriptsrclist;
    }


}