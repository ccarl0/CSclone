using HtmlAgilityPack;
using System.Diagnostics;
using System.Net;
using System.Xml;
using System.Xml.Linq;

internal class Program
{
    static int desktop = 0;
    static int mobile = 0;
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

        int maxParallelTasks = 50;
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
                await File.WriteAllTextAsync(htmlFilePath, htmlContent);


                // get images urls
                GetImages(htmlFilePath, name);

                

            });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        Console.WriteLine("Adding references");

        stopwatch.Stop();
        Console.WriteLine($"\n\n\nTotal execution time: {stopwatch.Elapsed}");
        await Console.Out.WriteLineAsync($"Desktop: {desktop}");
        await Console.Out.WriteLineAsync($"Mobile: {mobile}");
    }

    static void GetImages(string htmlFilePath, string name)
    {
        HtmlDocument htmlDoc = new();
        htmlDoc.Load(htmlFilePath);

        // desktop images
        HtmlNode selectedNodeDesktop = htmlDoc.DocumentNode.SelectSingleNode("//source[@media='(min-aspect-ratio: 4/3)']");

        if (selectedNodeDesktop != null)
        {
            string fileUrl = selectedNodeDesktop.GetAttributeValue("srcSet", "");

            fileUrl = fileUrl.Split("2400w,")[1].Split(" 2880w")[0];
            
            DownloadImage(fileUrl, name);
            desktop++;
        }


        // mobile images
        HtmlNode selectedNodeMobile = htmlDoc.DocumentNode.SelectSingleNode("//source[@media='(min-aspect-ratio: 3/4) and (max-aspect-ratio: 4/3)']");

        if (selectedNodeMobile != null)
        {
            string fileUrl = selectedNodeMobile.GetAttributeValue("srcSet", "");

            fileUrl = fileUrl.Split("2400w,")[1].Split(" 2880w")[0];

            Console.WriteLine(fileUrl);

            DownloadImage(fileUrl, $"{name}-mobile");
            mobile++;
        }
        
        selectedNodeMobile = htmlDoc.DocumentNode.SelectSingleNode("//source[@media='(min-aspect-ratio: 1/1) and (max-aspect-ratio: 4/3)']");

        if (selectedNodeMobile != null)
        {
            string fileUrl = selectedNodeMobile.GetAttributeValue("srcSet", "");

            fileUrl = fileUrl.Split("2400w,")[1].Split(" 2880w")[0];

            Console.WriteLine(fileUrl);

            DownloadImage(fileUrl, $"{name}-mobile");
            mobile++;
        }
    }

    static void DownloadImage(string fileUrl, string name)
    {
        if (!string.IsNullOrEmpty(fileUrl))
        {
            using (WebClient webClient = new WebClient())
            {
                // Provide a path where the file will be saved
                string savePath = $"../../../res/{name}.png"; // Replace with your desired file path and extension

                // Download the file
                webClient.DownloadFile(fileUrl, savePath);
            }
        }
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
    }
}