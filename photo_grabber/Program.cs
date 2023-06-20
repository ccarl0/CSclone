using HtmlAgilityPack;
using System;
using System.IO;
using System.Net;

class Program
{
    static void Main(string[] args)
    {
        string url = "https://www.volkswagen-veicolicommerciali.it/it/modelli.html"; // Replace with the URL of the page containing the images

        // Download the HTML content
        string htmlContent = DownloadHtml(url);

        // Load the HTML document
        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(htmlContent);

        // Select all img tags
        HtmlNodeCollection imgTags = doc.DocumentNode.SelectNodes("//img");

        // Iterate through each img tag and download the src
        if (imgTags != null)
        {
            foreach (HtmlNode imgTag in imgTags)
            {
                // Get the src attribute value
                string src = imgTag.GetAttributeValue("src", "");

                // Download the image
                DownloadImage(src);
            }
        }
    }

    static string DownloadHtml(string url)
    {
        using (WebClient client = new WebClient())
        {
            return client.DownloadString(url);
        }
    }

    static void DownloadImage(string imageUrl)
    {
        using (WebClient client = new WebClient())
        {
            try
            {
                // Generate a unique filename for the image (or modify as per your requirements)
                string fileName = $"image_{Guid.NewGuid()}.jpg";

                // Get the full output directory path
                string outputDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\res"));

                // Combine the output directory and filename
                string filePath = Path.Combine(outputDir, fileName);

                // Create the output directory if it doesn't exist
                Directory.CreateDirectory(outputDir);

                // Download the image and save it to disk
                client.DownloadFile(imageUrl, filePath);
                Console.WriteLine($"Downloaded image: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading image: {ex.Message}");
            }
        }
    }
}
