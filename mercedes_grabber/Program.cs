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
            });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();
        await Console.Out.WriteLineAsync( stopwatch.Elapsed.ToString());
    }

    private static string GetModelName(string modelURL)
    {
        return modelURL.Split("/")[5] + "-" + modelURL.Split("/")[6];
    }

    

    private static async Task<string> GrabHtmlAsync(string modelURL)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, modelURL);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
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
            string jsContent = await GetJsAsync(javascriptUrl);
            await File.AppendAllTextAsync(jsFilePath, jsContent);
            //Thread.Sleep(2000);
        }
    }

    private static async Task<string> GetJsAsync(string javascriptUrl)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, javascriptUrl);
        var response = await client.SendAsync(request);
        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            await Console.Out.WriteLineAsync($"{javascriptUrl} said: \n {e.Message} \n\n");
        }
        var jsContent = await response.Content.ReadAsStringAsync();

        return jsContent;
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