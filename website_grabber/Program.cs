using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using HtmlAgilityPack;

class Program
{
    static async Task Main()
    {
        Console.Write("Enter the URL of the web page: ");
        string url = Console.ReadLine();

        string name = url.Split('.').ToArray()[1];
        Console.WriteLine(name);

        // Create the folder if it doesn't exist
        Directory.CreateDirectory($"../../../res/{name}");

        // Download the HTML content
        string htmlContent = await DownloadContentAsync(url);
        string htmlFilePath = $"../../../res/{name}/{name}.html";
        File.WriteAllText(htmlFilePath, htmlContent);
        Console.WriteLine($"HTML Content downloaded and saved to {htmlFilePath}");

        // Download the CSS content
        string cssContent = await DownloadCssAsync(htmlContent, url);
        string cssFilePath = $"../../../res/{name}/styles.css";
        File.WriteAllText(cssFilePath, cssContent);
        Console.WriteLine($"CSS Content downloaded and saved to {cssFilePath}\"");

        // Download the JavaScript content
        string javascriptContent = await DownloadJavaScriptAsync(htmlContent, url);
        string javascriptFilePath = $"../../../res/{name}/script.js";
        File.WriteAllText(javascriptFilePath, javascriptContent);
        Console.WriteLine($"JavaScript Content downloaded and saved to {javascriptFilePath}");

        // Download images
        var urlsList = await GetAllImages(url, name);

        // Save images names
        var namesList = await GetAllImagesNames(url, name);

        int counter = 0 ;
        foreach ( var item in urlsList)
        {
            await Console.Out.WriteLineAsync($"Downlaoding {item}");
            await DownloadImage(item, name, counter);
            counter++;
        }


        await Console.Out.WriteLineAsync("Rewriting HTML");
        RewriteHtml(name);

        await Console.Out.WriteLineAsync("Getting page HREF");
        GetAllHREFs(url, name);
    }

    //get images URLs and save in a txt file
    static async Task<List<string>> GetAllImages(string url, string name)
    {
        List<string> ImageList = new List<string>();

        HttpClient client = new HttpClient();
        HttpResponseMessage response = await client.GetAsync(url);
        string source = await response.Content.ReadAsStringAsync();
        HtmlDocument document = new ();
        document.LoadHtml(source);

        // For every tag in the HTML containing the node img.
        foreach (var link in document.DocumentNode.Descendants("img")
            .Select(i => i.Attributes["src"]))
        {
            var linkString = link.Value.ToString();
            if (linkString.StartsWith("data:image"))
            {
                //skipping 64-encoded images
                continue;
            }
            else if (linkString.StartsWith("/idhub"))
            {
                var imagePath = linkString;
                linkString = $"https://www.volkswagen.it{imagePath}";
                ImageList.Add(linkString);
            }
            else if(linkString.StartsWith("https:"))
            {
                await Console.Out.WriteLineAsync(linkString);
                ImageList.Add(linkString);
            }
        }
        string filePath = $"../../../res/{name}/links.txt";
        File.WriteAllLines(filePath, ImageList);

        return ImageList;
    }

    static async Task<List<string>> GetAllImagesNames(string url, string name)
    {
        List<string> namesList = new List<string>();

        HttpClient client = new HttpClient();
        HttpResponseMessage response = await client.GetAsync(url);
        string source = await response.Content.ReadAsStringAsync();
        HtmlDocument document = new();
        document.LoadHtml(source);

        // For every tag in the HTML containing the node img.
        foreach (var alt in document.DocumentNode.Descendants("img")
        .Where(i => !i.Ancestors("noscript").Any())
        .Select(i => i.Attributes["alt"]))
        {
            var altString = alt.Value.ToString();

            char[] charactersToReplace = { '<', '>',':','"','/','\\','|','?','*' };
            string modifiedString = altString;

            foreach (char character in charactersToReplace)
            {
                modifiedString = modifiedString.Replace(character, '-');
            }

            Console.WriteLine(modifiedString);

            Console.WriteLine(altString);
            namesList.Add(modifiedString);
        }
        string filePath = $"../../../res/{name}/names.txt";
        File.WriteAllLines(filePath, namesList);

        return namesList;
    }

    static async Task DownloadImage(string imageUrl, string name, int counter)
    {
        string directoryPath = $"../../../res/{name}/img/";
        string nameFilePath = $"../../../res/{name}/names.txt";

        string[] lines = File.ReadAllLines(nameFilePath);
        List<string> namesList = new List<string>(lines);

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
            Console.WriteLine("Directory created: " + directoryPath);
        }
        else
        {
            Console.WriteLine("Directory already exists: " + directoryPath);
        }

        using (HttpClient client = new HttpClient())
        {
            try
            {
                using (HttpResponseMessage response = await client.GetAsync(imageUrl))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        using (Stream stream = await response.Content.ReadAsStreamAsync())
                        using (FileStream fileStream = new FileStream($"../../../res/{name}/img/{namesList[counter]}.png", FileMode.Create))
                        {
                            await stream.CopyToAsync(fileStream);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Failed to download image. Status code: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while downloading the image: {ex.Message}");
            }
        }
    }

    static void RewriteHtml(string name)
    {
        string filePath = $"../../../res/{name}/{name}.html";

        // Check if the file exists
        if (File.Exists(filePath))
        {
            // Load the HTML document from the file
            HtmlDocument doc = new HtmlDocument();
            doc.Load(filePath);

            // Select all img tags in the document
            foreach (HtmlNode imgTag in doc.DocumentNode.Descendants("img"))
            {
                // Get the alt attribute value
                string alt = imgTag.GetAttributeValue("alt", "");

                char[] charactersToReplace = { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };
                string modifiedString = alt;

                foreach (char character in charactersToReplace)
                {
                    modifiedString = modifiedString.Replace(character, '-');
                }

                Console.WriteLine(modifiedString);

                Console.WriteLine();
                Console.WriteLine(alt);
                Console.WriteLine(modifiedString);
                Console.WriteLine();

                // Set the alt value as the new src attribute value
                imgTag.SetAttributeValue("src", $"img\\{modifiedString}.png");
                Console.WriteLine($"img\\{modifiedString}.png");
            }

            // Get the modified HTML content
            string modifiedHtml = doc.DocumentNode.OuterHtml;

            doc.Save(filePath);
        }
        else
        {
            // Handle the case when the file doesn't exist
            // ...
            Console.WriteLine();
            Console.WriteLine("Error");
            Console.WriteLine();
        }
    }
    

    static void GetAllHREFs(string url, string name)
    {
        string filePath = $"../../../res/{name}/{name}.html";
        Console.WriteLine(filePath);
        string outputFilePath = $"../../../res/{name}/href.txt";

        HtmlDocument doc = new HtmlDocument();
        doc.Load(filePath);

        // Select all a tags in the document
        var anchorTags = doc.DocumentNode.Descendants("a");

        // Collect the URLs from href attributes
        var urls = new List<string>();
        foreach (var anchorTag in anchorTags)
        {
            string href = anchorTag.GetAttributeValue("href", "");
            urls.Add(href);
        }

        // Save the URLs to a text file
        File.WriteAllLines(outputFilePath, urls);

        Console.WriteLine("URLs saved to the file successfully.");
    }

    static async Task<string> DownloadContentAsync(string url)
    {
        using (HttpClient client = new HttpClient())
        {
            return await client.GetStringAsync(url);
        }
    }

    static async Task<string> DownloadCssAsync(string htmlContent, string baseUrl)
    {
        string cssUrl = ExtractCssUrl(htmlContent);
        if (!string.IsNullOrEmpty(cssUrl))
        {
            if (!Uri.IsWellFormedUriString(cssUrl, UriKind.Absolute))
            {
                cssUrl = new Uri(new Uri(baseUrl), cssUrl).AbsoluteUri;
            }
            return await DownloadContentAsync(cssUrl);
        }

        return string.Empty;
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

    static string ExtractCssUrl(string htmlContent)
    {
        // Extract the CSS URL from the HTML content
        // You can use a regular expression or HTML parsing library to do this
        // Here, I'll demonstrate a simple way using string manipulation

        int startIndex = htmlContent.IndexOf("<link rel=\"stylesheet\" href=\"");
        if (startIndex != -1)
        {
            startIndex += "<link rel=\"stylesheet\" href=\"".Length;
            int endIndex = htmlContent.IndexOf("\"", startIndex);
            if (endIndex != -1)
            {
                return htmlContent.Substring(startIndex, endIndex - startIndex);
            }
        }

        return string.Empty;
    }

    static string ExtractJavaScriptUrl(string htmlContent)
    {
        // Extract the JavaScript URL from the HTML content
        // You can use a regular expression or HTML parsing library to do this
        // Here, I'll demonstrate a simple way using string manipulation

        int startIndex = htmlContent.IndexOf("<script src=\"");
        if (startIndex != -1)
        {
            startIndex += "<script src=\"".Length;
            int endIndex = htmlContent.IndexOf("\"", startIndex);
            if (endIndex != -1)
            {
                return htmlContent.Substring(startIndex, endIndex - startIndex);
            }
        }

        return string.Empty;
    }
}
