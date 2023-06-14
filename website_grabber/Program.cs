using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;
using HtmlAgilityPack;

class Program
{
    static async Task Main()
    {

        string url = "https://www.volkswagen.it/it/modelli/up.html";
        string name = url.Split('.').ToArray()[1]; ;
        await Console.Out.WriteLineAsync($"URL: {url}");
        await Console.Out.WriteLineAsync($"\n\nName: {name}");

        // downlaod html + stuff
        await Console.Out.WriteLineAsync($"\n\nGetting HTML CSS and JS");
        await DownloadPageAsync(url, name);



        // get models url
        await Console.Out.WriteLineAsync("\n\nGetting models href");
        await GetModels(name);


        // get images
        // await GetAllImages(url, name);
        var urlsList = await GetAllImagesURLs(name);
        await GetAllImagesNames(name);
        int counter = 0;
        foreach (var item in urlsList)
        {
            await Console.Out.WriteLineAsync($"Downlaoding {item}");
            await DownloadImage(item, name, counter);
            counter++;
        }

        // clean html
        // remove: header, footer, configuration button, nav bar
        // rewrites srcs
        CleanHtml(name);





        await DownloadJavaScriptAndRemoveTagAsync(url);
    }


    static async Task<string> DownloadJavaScriptAndRemoveTagAsync(string pageUrl)
    {
        string scriptId = "spaModel";

        HttpClient httpClient = new HttpClient();

        // Download the HTML content of the page
        string htmlContent = await httpClient.GetStringAsync(pageUrl);

        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(htmlContent);

        // Find the <script> tag with the specified id
        HtmlNode scriptTag = doc.GetElementbyId(scriptId);

        if (scriptTag != null)
        {
            // Extract the JavaScript content from the script tag
            string javascriptContent = scriptTag.InnerHtml;

            // Save the JavaScript content to a file
            string javascriptFilePath = $"../../../res/volkswagen/script.js";
            File.AppendAllText(javascriptFilePath, javascriptContent);

            return javascriptContent;
        }

        return string.Empty;
    }










    //get images URLs and save in a txt file
    static async Task<List<string>> GetAllImagesURLs(string name)
    {
        List<string> imagesUrlsList = new List<string>();

        string htmlFilePath = $"../../../res/{name}/{name}.html";

        HtmlDocument document = new HtmlDocument();
        document.Load(htmlFilePath);

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
                imagesUrlsList.Add(linkString);
            }
            else if(linkString.StartsWith("https:"))
            {
                await Console.Out.WriteLineAsync(linkString);
                imagesUrlsList.Add(linkString);
            }
        }
        string filePath = $"../../../res/{name}/images_urls.txt";
        File.WriteAllLines(filePath, imagesUrlsList);

        return imagesUrlsList;
    }

    static async Task<List<string>> GetAllImagesNames(string name)
    {
        string filePath = $"../../../res/{name}/{name}.html";
        List<string> namesList = new List<string>();

        HtmlDocument document = new HtmlDocument();
        document.Load(filePath);

        // For every tag in the HTML containing the node img.
        foreach (var alt in document.DocumentNode.Descendants("img")
            .Where(i => !i.Ancestors("noscript").Any())
            .Select(i => i.Attributes["alt"]))
        {
            var altString = alt?.Value?.ToString();

            if (altString != null)
            {
                string legalString = ReplaceIllegalChar(altString);
                namesList.Add(legalString);
            }
        }

        string outputFilePath = $"../../../res/{name}/names.txt";
        File.WriteAllLines(outputFilePath, namesList);

        return namesList;
    }



    static async Task DownloadImage(string imageUrl, string name,int counter)
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

    static void RewriteHtmlImgTags(string name)
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

                //replace illegal filename character
                string modifiedString = ReplaceIllegalChar(alt);


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
            Console.WriteLine("File doesn't exist");
        }
    }

    static string ReplaceIllegalChar(string illegalString)
    {
        char[] illegalCharacters = { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };
        string legalString = illegalString;

        foreach (char character in illegalCharacters)
            legalString = legalString.Replace(character, '-');

        return legalString;
    }



    static Task<List<string>> GetModels(string name)
    {
        var getURLsRes = GetURLs(name);
        var getPathsRes = URL2Path(getURLsRes.Result, name);

        return Task.FromResult(getPathsRes.Result);
    }

    static Task<List<string>> GetURLs(string name)
    {
        string filePath = $"../../../res/{name}/{name}.html";
        string outputFilePath = $"../../../res/{name}/hrefs.txt";

        HtmlDocument doc = new HtmlDocument();
        doc.Load(filePath);

        // Select all a tags in the document
        var anchorTags = doc.DocumentNode.Descendants("a");

        // Collect the URLs from href attributes
        var hrefList = new List<string>();
        foreach (var anchorTag in anchorTags)
        {
            string href = anchorTag.GetAttributeValue("href", "");
            hrefList.Add(href);
        }

        // Save the URLs to a text file
        File.WriteAllLines(outputFilePath, hrefList);

        //URL2Path(hrefList, name);

        return Task.FromResult(hrefList);
    }

    static Task<List<string>> URL2Path(List<string> hrefList, string name)
    {
        string outputFilePath = $"../../../res/{name}/paths.txt";
        List<string> pathList = new();

        foreach (var item in hrefList)
        {
            if (Uri.IsWellFormedUriString(item, UriKind.Absolute))
            {
                Uri uri = new Uri(item);
                var path = uri.PathAndQuery.ToString();
                pathList.Add(path);
            }
            else
            {
                pathList.Add(item);
            }   
        }
        pathList.RemoveAll(path => path == "#");
        File.WriteAllLines(outputFilePath, pathList);

        return Task.FromResult(pathList);
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

    
    static async Task DownloadPageAsync(string url, string name)
    {
        var htmlContent = await DownloadContentAsync(url);
        var cssContent = await DownloadCssAsync(htmlContent, url);
        var jsContent = await DownloadJavaScriptAsync(htmlContent, url);

        await Console.Out.WriteLineAsync($"HTML: {htmlContent}");
        await Console.Out.WriteLineAsync($"CSS: {cssContent}");
        await Console.Out.WriteLineAsync($"JS: {jsContent}");

        await Console.Out.WriteLineAsync("Done!");


        string htmlFilePath = $"../../../res/{name}/{name}.html";
        string cssFilePath = $"../../../res/{name}/styles.css";
        string jsFilePath = $"../../../res/{name}/script.js";

        Directory.CreateDirectory($"../../../res/{name}");

        File.WriteAllText(htmlFilePath, htmlContent);
        File.WriteAllText(cssFilePath, cssContent);
        File.WriteAllText(jsFilePath, jsContent);
    }


    static void CleanHtml(string name)
    {
        RemoveHeader(name);
        RemoveFooter(name);
        RemoveConfiguratorButton(name);
        RemoveNav(name);
        RewriteHtmlImgTags(name);

        //js clearance
        RemoveScriptById(name, "spaModel");
    }

    static void RemoveFooter(string name)
    {
        HtmlDocument doc = new HtmlDocument();
        string filePath = $"../../../res/{name}/{name}.html";
        doc.Load(filePath);

        // Locate the footer
        HtmlNode footerToRemove = doc.DocumentNode.SelectSingleNode("//footer");

        // Check if the div element exists
        if (footerToRemove != null)
        {
            // Remove the div element from the document
            footerToRemove.Remove();
        }

        // Save the modified HTML document to a new file or overwrite the existing file
        doc.Save(filePath); // Replace with the desired file path
    }

    static void RemoveHeader(string name)
    {
        HtmlDocument doc = new HtmlDocument();
        string filePath = $"../../../res/{name}/{name}.html";
        doc.Load(filePath);

        // Locate the footer
        HtmlNode headerToRemove = doc.DocumentNode.SelectSingleNode("//header");

        // Check if the div element exists
        if (headerToRemove != null)
        {
            // Remove the div element from the document
            headerToRemove.Remove();
        }

        // Save the modified HTML document to a new file or overwrite the existing file
        doc.Save(filePath); // Replace with the desired file path
    }

    static void RemoveConfiguratorButton(string name)
    {
        //doesn't work

        HtmlDocument doc = new HtmlDocument();
        string filePath = $"../../../res/{name}/{name}.html";
        doc.Load(filePath);

        // Locate the button to remove
        HtmlNode buttonToRemove = doc.DocumentNode.SelectSingleNode("//button[@aria-label='Load your configuration using a dialog.']");

        // Check if the div element exists
        if (buttonToRemove != null)
        {
            // Remove the div element from the document
            buttonToRemove.Remove();
        }

        // Save the modified HTML document to a new file or overwrite the existing file
        doc.Save(filePath); // Replace with the desired file path
    }

    static void RemoveNav(string name)
    {
        //doesn't work

        HtmlDocument doc = new HtmlDocument();
        string filePath = $"../../../res/{name}/{name}.html";
        doc.Load(filePath);

        // Locate the nav to remove
        HtmlNode navToRemove = doc.DocumentNode.SelectSingleNode("//nav[@aria-label='breadcrumbs' and contains(@class, 'StyledBreadcrumbsWrapper-OTKrm')]");
        HtmlNode menuToRemove = doc.DocumentNode.SelectSingleNode("//nav[@role='navigation' and @aria-hidden='true' and contains(@class, 'StyledNav-rGtno')]");

        // Check if the nav element exists
        if (navToRemove != null)
        {
            // Remove the nav element from the document
            navToRemove.Remove();
        }

        if (menuToRemove != null)
        {
            menuToRemove.Remove();
        }

        // Save the modified HTML document to a new file or overwrite the existing file
        doc.Save(filePath); // Replace with the desired file path
    }

    static void RemoveScriptById(string name, string scriptId)
    {
        //doesn't work

        HtmlDocument doc = new HtmlDocument();
        string filePath = $"../../../res/{name}/{name}.html";
        doc.Load(filePath);

        // Locate the nav to remove
        HtmlNode scriptTag = doc.GetElementbyId(scriptId);

        // Check if the nav element exists
        if (scriptTag != null)
        {
            // Remove the nav element from the document
            scriptTag.Remove();
        }

        // Save the modified HTML document to a new file or overwrite the existing file
        doc.Save(filePath); // Replace with the desired file path
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
