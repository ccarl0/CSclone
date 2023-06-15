using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using HtmlAgilityPack;

class Program
{
    static async Task Main()
    {

        //var client = new HttpClient();
        //var request = new HttpRequestMessage(HttpMethod.Get, "https://www.volkswagen.it/it/modelli/nuova-polo.html");
        //var response = await client.SendAsync(request);
        //response.EnsureSuccessStatusCode();
        //Console.WriteLine(await response.Content.ReadAsStringAsync());
        //var a = await response.Content.ReadAsStringAsync();

        //string url = "https://www.volkswagen.it/it/modelli/id3.html";
        Console.WriteLine("Input url");
        string url = Console.ReadLine();
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
        //await GetAllImages(url, name);
        //var urlsList = await GetAllImagesURLs(name);
        //await GetAllImagesNames(name);
        //int counter = 0;
        //foreach (var item in urlsList)
        //{
        //    await Console.Out.WriteLineAsync($"Downlaoding {item}");
        //    await DownloadImage(item, name, counter);
        //    counter++;
        //}

        // clean html
        // remove: header, footer, configuration button, nav bar
        // rewrites srcs
        CleanHtml(name);





        //await DownloadJavaScriptAndRemoveTagAsync(url);

        AddScriptTag(name);


    }

    private static void AddScriptTag(string name)
    {
        Console.WriteLine("Adding script tag");
        string htmlFilePath = $"../../../res/{name}/{name}.html";
        string jsFilePath = $"../../../res/{name}/script.js";
        HtmlDocument htmlDocument = new HtmlDocument();
        htmlDocument.Load(htmlFilePath);

        // Create a new script element
        HtmlNode scriptTag = htmlDocument.CreateElement("script");

        // Set the src attribute to "script.js"
        scriptTag.SetAttributeValue("src", "script.js");

        // Read script.js code
        //string scriptContent = File.ReadAllText(jsFilePath);

        //Console.WriteLine(scriptContent);

        // Writing script into the tag
        //scriptTag.InnerHtml = scriptContent;

        // Append the script tag to the HTML document
        HtmlNode headNode = htmlDocument.DocumentNode.SelectSingleNode("//body");
        headNode.AppendChild(scriptTag);


        // Save the modified HTML document
        htmlDocument.Save(htmlFilePath);
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
                //await Console.Out.WriteLineAsync(linkString);
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
            //Console.WriteLine("Directory created: " + directoryPath);
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
                        //Console.WriteLine($"Failed to download image. Status code: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"An error occurred while downloading the image: {ex.Message}");
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
                //Console.WriteLine($"img\\{modifiedString}.png");
            }

            // Get the modified HTML content
            string modifiedHtml = doc.DocumentNode.OuterHtml;

            doc.Save(filePath);
        }
        else
        {
            // Handle the case when the file doesn't exist
            //Console.WriteLine("File doesn't exist");
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
        var client = new HttpClient();

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        //Console.WriteLine(await response.Content.ReadAsStringAsync());
        return await response.Content.ReadAsStringAsync();

        
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

        //await Console.Out.WriteLineAsync($"HTML: {htmlContent}");
        //await Console.Out.WriteLineAsync($"CSS: {cssContent}");
        //await Console.Out.WriteLineAsync($"JS: {jsContent}");

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

        //List<string> divToRemoveList = new List<string>
        //{
        //    "//*[@id=\"reactmount\"]/div/div[1]/div/div[2]/div", // header
        //    "//*[@id=\"stage_copy\"]/div/div[2]/div[3]", //richiedi preventivo button
        //    "//*[@id=\"cartechnicaldatasect_1698030854\"]/div/div", //configura button
        //    "//*[@id=\"reactmount\"]/div/div[1]/main/div/div/div[2]/div/div/div/div/div/div/div[1]/div", //promozioni
        //    "//*[@id=\"sectiongroup_1037184_uspsection_194170196\"]/div[2]/div/div[4]/div[2]/div[3]", // scopri i sistemi di assistenza button
        //    "//*[@id=\"sectiongroup_1037184_uspsection_194170196\"]/div[2]/div/div[3]/div[5]", // scopri i fari button
        //    "//*[@id=\"sectiongroup_1037184_uspsection_194170196\"]/div[2]/div/div[6]/div/div[3]", // scopri di più cockpit button
        //    "//*[@id=\"sectiongroup_7104651_powerteasersection_c\"]/div/div/div[6]", // scopri i sistemi di assistenza button
        //    "//*[@id=\"reactmount\"]/div/div[1]/main/div/div/div[2]/div/div/div/div/div/div/div[8]", // Nuova Polo TGI a metano
        //    "//*[@id=\"reactmount\"]/div/div[1]/main/div/div/div[2]/div/div/div/div/div/div/div[9]", //  scegli la versione
        //    "//*[@id=\"reactmount\"]/div/div[1]/main/div/div/div[2]/div/div/div/div/div/div/div[10]", // mobilità metano
        //    "//*[@id=\"reactmount\"]/div/div[1]/main/div/div/div[2]/div/div/div/div/div/div/div[11]", // maggiori informazioni
        //    "//*[@id=\"reactmount\"]/div/div[1]/main/div/div/div[2]/div/div/div/div/div/div/div[12]", //app e accessori
        //    "//*[@id=\"sectiongroup_1597799_twocolumnssection\"]", // listino
        //    "//*[@id=\"firstlevelteasersect\"]", // scopri le nuove polo
        //    "//*[@id=\"reactmount\"]/div/div[1]/main/div/div/div[2]/div/div/div/div/div/div/div[15]", // modelli e nuova polo gti
        //    "//*[@id=\"reactmount\"]/div/div[1]/main/div/div/div[2]/div/div/div/div/div/div/div[16]", // potrebbe interessarti
        //    "//*[@id=\"reactmount\"]/div/div[2]", // footer
        //    "//*[@id=\"versioni\"]", // versioni
        //    "//*[@id=\"newslettersignupsect\"]" //maggiori informazioni
        //};

        //List<string> divToRemoveList = new List<string>
        //{
        //    "//*[@id=\"reactmount\"]/div/div[1]/div/div[2]/div", // header
        //    "//*[@id=\"stage_copy\"]/div/div[2]/div[3]", //richiedi preventivo button
        //    "//*[@id=\"cartechnicaldatasect_1698030854\"]/div/div", //configura button
        //    "//*[@id=\"reactmount\"]/div/div[1]/main/div/div/div[2]/div/div/div/div/div/div/div[1]/div", //promozioni
        //    "//*[@id=\"sectiongroup_1037184_uspsection_194170196\"]/div[2]/div/div[4]/div[2]/div[3]", // scopri i sistemi di assistenza button
        //    "//*[@id=\"sectiongroup_1037184_uspsection_194170196\"]/div[2]/div/div[3]/div[5]", // scopri i fari button
        //    "//*[@id=\"sectiongroup_1037184_uspsection_194170196\"]/div[2]/div/div[6]/div/div[3]", // scopri di più cockpit button
        //    "//*[@id=\"sectiongroup_7104651_powerteasersection_c\"]/div/div/div[6]", // scopri i sistemi di assistenza button
        //    "//*[@id=\"reactmount\"]/div/div[1]/main/div/div/div[2]/div/div/div/div/div/div/div[8]", // Nuova Polo TGI a metano
        //    "//*[@id=\"reactmount\"]/div/div[1]/main/div/div/div[2]/div/div/div/div/div/div/div[9]", //  scegli la versione
        //    "//*[@id=\"reactmount\"]/div/div[1]/main/div/div/div[2]/div/div/div/div/div/div/div[10]", // mobilità metano
        //    "//*[@id=\"reactmount\"]/div/div[1]/main/div/div/div[2]/div/div/div/div/div/div/div[11]", // maggiori informazioni
        //    "//*[@id=\"reactmount\"]/div/div[1]/main/div/div/div[2]/div/div/div/div/div/div/div[12]", //app e accessori
        //    "//*[@id=\"sectiongroup_1597799_twocolumnssection\"]", // listino
        //    "//*[@id=\"firstlevelteasersect\"]", // scopri le nuove polo
        //    "//*[@id=\"firstlevelteasersect\"]", // modelli e nuova polo gti
        //    "//*[@id=\"editorialteasersecti_686435660\"]", // potrebbe interessarti
        //    //"//*[@id=\"reactmount\"]/div/div[2]/footer", // footer
        //    "//*[@id=\"versioni\"]", // versioni
        //    "//*[@id=\"newslettersignupsect\"]" //maggiori informazioni
        //};

        //List<string> PathToRemoveList = new List<string>
        //{
        //    "//*[@id=\"reactmount\"]/div/div[1]/div/div[2]/div", // header
        //    "//*[@id=\"stage_copy\"]/div/div[2]/div[3]", //richiedi preventivo button
        //    "//*[@id=\"cartechnicaldatasect_1698030854\"]/div/div", //configura button
        //    "//*[@id=\"reactmount\"]/div/div[1]/main/div/div/div[2]/div/div/div/div/div/div/div[1]/div", //promozioni
        //    "//*[@id=\"sectiongroup_1037184_uspsection_194170196\"]/div[2]/div/div[4]/div[2]/div[3]", // scopri i sistemi di assistenza button
        //    "//*[@id=\"sectiongroup_1037184_uspsection_194170196\"]/div[2]/div/div[3]/div[5]", // scopri i fari button
        //    "//*[@id=\"sectiongroup_1037184_uspsection_194170196\"]/div[2]/div/div[6]/div/div[3]", // scopri di più cockpit button
        //    "//*[@id=\"sectiongroup_7104651_powerteasersection_c\"]/div/div/div[6]", // scopri i sistemi di assistenza button
        //    "//*[@id=\"reactmount\"]/div/div[1]/main/div/div/div[2]/div/div/div/div/div/div/div[8]", // Nuova Polo TGI a metano
        //    "//*[@id=\"reactmount\"]/div/div[1]/main/div/div/div[2]/div/div/div/div/div/div/div[9]", //  scegli la versione
        //    "//*[@id=\"reactmount\"]/div/div[1]/main/div/div/div[2]/div/div/div/div/div/div/div[10]", // mobilità metano
        //    "//*[@id=\"reactmount\"]/div/div[1]/main/div/div/div[2]/div/div/div/div/div/div/div[11]", // maggiori informazioni
        //    "//*[@id=\"reactmount\"]/div/div[1]/main/div/div/div[2]/div/div/div/div/div/div/div[12]", //app e accessori
        //    "//*[@id=\"sectiongroup_1597799_twocolumnssection\"]", // listino
        //    "//*[@id=\"firstlevelteasersect\"]", // scopri le nuove polo
        //    "//*[@id=\"firstlevelteasersect\"]", // modelli e nuova polo gti
        //    "//*[@id=\"editorialteasersecti_686435660\"]", // potrebbe interessarti
        //    //"//*[@id=\"reactmount\"]/div/div[2]/footer", // footer
        //    "//*[@id=\"versioni\"]", // versioni
        //    "//*[@id=\"newslettersignupsect\"]" //maggiori informazioni
        //};

        //foreach (var item in divToRemoveList)
        //{
        //    RemoveDivByXPath(name, item);
        //}

        RemoveByXPath(name,"//div[@class='tag']");
        RemoveByXPath(name, "//div[@class='StyledEditableComponent-cfYJPD iBZONI linkElement']");
        RemoveByXPath(name, "//div[@class='StyledEditableComponent-cfYJPD iBZONI buttonsParsys']");
        RemoveByXPath(name, "//div[@class='StyledChildWrapper-sc-1d21nde iFakPP']");
        RemoveByXPath(name, "//div[@class='StyledLinkText-sc-12fkfup TpPRV']");
        RemoveByXPath(name, "//div[@class='StyledButtonWrapper-cZdQgL bPwnem']");
        RemoveByXPath(name, "//div[@class='StyledLinkWrapper-bEHVV kyDJWM']"); // freccia configuratore
        RemoveByXPath(name, "//div[@class='NewsletterSignupSectionWraper-faZEQE bOaAQm']"); // freccia configuratore
        RemoveByXPath(name, "//section[@id='versioni']"); // scegli la versione
        RemoveByXPath(name, "//section[@id='newslettersignupsect_1874992862']"); // preventivo
        RemoveByXPath(name, "//section[@id='promozioni']"); // promozioni
        RemoveByXPath(name, "//section[@id='editorialteasersecti']"); // potrebbe interessarti
        RemoveByXPath(name, "//section[@id='focusteasersection_1']"); // 
        RemoveByXPath(name, "//section[@id='sectiongroup_copy_193396639']"); // ID. Buzz – L'icona di una nuova era
        RemoveByXPath(name, "//section[@id='expandcollapsesectio']"); // le immagini coi bimbi delle elettriche
        RemoveByXPath(name, "//section[@id='focusteasersection']"); // scopri la gamma volkswagen
        RemoveByXPath(name, "//section[@id='sectiongroup_copy_1022890685']"); // manutenzione

        RemoveByXPath(name, "//section[@id='sectiongroup_copy_19']"); // ID Buzz - Nuova ID3
        
        RemoveByXPath(name, "//span[text()='Potrebbe interessarti anche questo:']"); // Potrebbe interessarti anche questo


        string htmlFilePath = $"../../../res/{name}/{name}.html";
        // Load the HTML file
        HtmlDocument doc = new HtmlDocument();
        doc.Load(htmlFilePath);


        HtmlNode versionDivNode = doc.DocumentNode.SelectSingleNode("//div[contains(., 'Scegli la versione')]");

        // Remove the following div elements
        if (versionDivNode != null)
        {
            HtmlNodeCollection followingDivs = versionDivNode.SelectNodes("following-sibling::div");
            if (followingDivs != null)
            {
                foreach (HtmlNode followingDiv in followingDivs)
                {
                    followingDiv.Remove();
                }
            }
        }
        
        doc.Save(htmlFilePath);



        //js clearance
        RemoveScriptTag(name);
        //RemoveScriptById(name, "spaModel");




        //css
        ExtractCssAndSave(name);
    }

    private static void RemoveDivByXPath(string name, string divClass)
    {
        HtmlDocument doc = new HtmlDocument();
        string filePath = $"../../../res/{name}/{name}.html";
        doc.Load(filePath);

        // Locate the button to remove
        HtmlNode divToRemove = doc.DocumentNode.SelectSingleNode(divClass);

        // Check if the div element exists
        if (divToRemove != null)
        {
            // Remove the div element from the document
            divToRemove.Remove();
        }
        else
            Console.WriteLine($"Couldn't find divClass= {divClass}");

        // Save the modified HTML document to a new file or overwrite the existing file
        doc.Save(filePath); // Replace with the desired file path
    }

    private static void RemoveByXPath(string name, string xPath)
    {
        string htmlFilePath = $"../../../res/{name}/{name}.html";

        // Load the HTML document from the file
        HtmlDocument htmlDoc = new HtmlDocument();
        htmlDoc.Load(htmlFilePath);

        var divList = htmlDoc.DocumentNode.SelectNodes(xPath);

        if (divList != null)
        {
            foreach (var divNode in divList.ToList())
            {
                divNode.Remove();
            }
        }

        htmlDoc.Save(htmlFilePath);
    }


    private static void ExtractCssAndSave(string name)
    {
        // saves css to styles.css
        // removes style tags
        // add single single link tag to link css file
        string htmlFilePath = $"../../../res/{name}/{name}.html";
        string cssFilePath = $"../../../res/{name}/styles.css";

        HtmlDocument htmlDocument = new HtmlDocument();
        htmlDocument.Load(htmlFilePath);

        foreach (HtmlNode styleNode in htmlDocument.DocumentNode.Descendants("style").ToList())
        {
            //Console.WriteLine(scriptNode.InnerHtml);

            //Console.WriteLine(styleNode.InnerHtml);

            File.AppendAllLines(cssFilePath, new string[] { styleNode.InnerHtml });

            styleNode.Remove();

            Console.WriteLine("\n\n\n\n\n\n");

            htmlDocument.Save(htmlFilePath);
        }


        HtmlNode linkNode = HtmlNode.CreateNode("<link rel=\"stylesheet\" type=\"text/css\" href=\"styles.css\" />");

        HtmlNode headNode = htmlDocument.DocumentNode.SelectSingleNode("//head");
        if (headNode != null)
        {
            headNode.AppendChild(linkNode);
        }
        else
        {
            htmlDocument.DocumentNode.AppendChild(linkNode);
        }

        // Save the modified HTML document
        htmlDocument.Save(htmlFilePath);
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

    static void RemoveScriptTag(string name)
    {
        string htmlFilePath = "../../../res/volkswagen/volkswagen.html";

        HtmlDocument htmlDocument = new HtmlDocument();
        htmlDocument.Load(htmlFilePath);

        foreach (HtmlNode scriptNode in htmlDocument.DocumentNode.Descendants("script").ToList())
        {
            //Console.WriteLine(scriptNode.InnerHtml);

            // Print the content of the src attribute
            string srcValue = scriptNode.GetAttributeValue("src", "");
            string scriptURL = null!;
            if (srcValue.StartsWith("//"))
            {
                scriptURL = $"https:{srcValue}";
            }
            else if (srcValue.StartsWith("/idhub"))
            {
                scriptURL = $"https://volkswagen.it{srcValue}";
            }
            else
            {
                Console.WriteLine("Skipping appending, already in html file");
                continue;
            }

            ReadAndAppendJavaScript(scriptURL);
            //Console.WriteLine($"src: {scriptURL}");

            // Remove the script tag from the HTML
            scriptNode.Remove();

            htmlDocument.Save(htmlFilePath);

            //Console.WriteLine("\n\n");
        }

    }


    static public void ReadAndAppendJavaScript(string url)
    
    {
        WebClient client = new WebClient();
        string outputFile = "../../../res/volkswagen/script.js";

        try
        {
            string javascriptCode = client.DownloadString(url);

            // Append the JavaScript code to the output file
            using (StreamWriter writer = File.AppendText(outputFile))
            {
                writer.WriteLine(javascriptCode);
            }

            //Console.WriteLine("JavaScript code appended to " + outputFile);
        }
        catch (Exception ex)
        {
            //Console.WriteLine("An error occurred while reading and appending the JavaScript code:");
            //Console.WriteLine(ex.Message);
        }
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
