using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

        foreach (var url in modelsURIList)
        {
            Task task = Task.Run(async () =>
            {
                await RunMainLoopAsync(url);
            });
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        Console.WriteLine("Downloaded all models");
        Console.WriteLine($"Total execution time: {stopwatch.Elapsed}");
    }

    private static async Task RunMainLoopAsync(string url)
    {
        string name = url.Split('/').Last().Split(".").First();
        //await Console.Out.WriteLineAsync($"URL: {url}");
        //await Console.Out.WriteLineAsync($"\n\nName: {name}");

        // downlaod html + stuff
        //await Console.Out.WriteLineAsync($"\n\nGetting HTML CSS and JS");
        await DownloadPageAsync(url, name);



        // get models url
        //await Console.Out.WriteLineAsync("\n\nGetting models href");
        await GetModels(name);


        // get images
        //await GetAllImages(url, name);

        //var urlsList = await GetAllImagesURLs(name);
        //await GetAllImagesNames(name);
        //int counter = 0;
        //foreach (var item in urlsList)
        //{
        //    //await Console.Out.WriteLineAsync($"Downlaoding {item}");
        //    await DownloadImage(item, name, counter);
        //    counter++;
        //}

        //clean html
        // remove: header, footer, configuration button, nav bar
        // rewrites srcs
        CleanHtml(name);





        //await DownloadJavaScriptAndRemoveTagAsync(url);

        AddScriptTag(name);
    }

    private static void AddScriptTag(string name)
    {
        //Console.WriteLine("Adding script tag");
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

    //static async Task<List<string>> GetAllImagesURLs(string name)
    //{
    //    List<string> imagesUrlsList = new List<string>();

    //    string htmlFilePath = $"../../../res/{name}/{name}.html";

    //    HtmlDocument document = new HtmlDocument();
    //    document.Load(htmlFilePath);

    //    // For every tag in the HTML containing the node img.
    //    foreach (var link in document.DocumentNode.Descendants("img")
    //        .Select(i => i.Attributes["src"]))
    //    {
    //        var linkString = link.Value.ToString();
    //        if (linkString.StartsWith("data:image"))
    //        {
    //            //skipping 64-encoded images
    //            continue;
    //        }
    //        else if (linkString.StartsWith("/idhub"))
    //        {
    //            var imagePath = linkString;
    //            linkString = $"https://www.volkswagen.it{imagePath}";
    //            imagesUrlsList.Add(linkString);
    //        }
    //        else if(linkString.StartsWith("https:"))
    //        {
    //            //await Console.Out.WriteLineAsync(linkString);
    //            imagesUrlsList.Add(linkString);
    //        }
    //    }
    //    string filePath = $"../../../res/{name}/images_urls.txt";
    //    File.WriteAllLines(filePath, imagesUrlsList);

    //    return imagesUrlsList;
    //}

    //static async Task<List<string>> GetAllImagesNames(string name)
    //{
    //    string filePath = $"../../../res/{name}/{name}.html";
    //    List<string> namesList = new List<string>();

    //    HtmlDocument document = new HtmlDocument();
    //    document.Load(filePath);

    //    // For every tag in the HTML containing the node img.
    //    foreach (var alt in document.DocumentNode.Descendants("img")
    //        .Where(i => !i.Ancestors("noscript").Any())
    //        .Select(i => i.Attributes["alt"]))
    //    {
    //        var altString = alt?.Value?.ToString();

    //        if (altString != null)
    //        {
    //            string legalString = ReplaceIllegalChar(altString);
    //            namesList.Add(legalString);
    //        }
    //    }

    //    string outputFilePath = $"../../../res/{name}/names.txt";
    //    File.WriteAllLines(outputFilePath, namesList);

    //    return namesList;
    //}



    //static async Task DownloadImage(string imageUrl, string name,int counter)
    //{
    //    string directoryPath = $"../../../res/{name}/img/";
    //    string nameFilePath = $"../../../res/{name}/names.txt";

    //    string[] lines = File.ReadAllLines(nameFilePath);
    //    List<string> namesList = new List<string>(lines);

    //    if (!Directory.Exists(directoryPath))
    //    {
    //        Directory.CreateDirectory(directoryPath);
    //        //Console.WriteLine("Directory created: " + directoryPath);
    //    }

    //    using (HttpClient client = new HttpClient())
    //    {
    //        try
    //        {
    //            using (HttpResponseMessage response = await client.GetAsync(imageUrl))
    //            {
    //                if (response.IsSuccessStatusCode)
    //                {
    //                    using (Stream stream = await response.Content.ReadAsStreamAsync())
    //                    using (FileStream fileStream = new FileStream($"../../../res/{name}/img/{namesList[counter]}.png", FileMode.Create))
    //                    {
    //                        await stream.CopyToAsync(fileStream);
    //                    }
    //                }
    //                else
    //                {
    //                    //Console.WriteLine($"Failed to download image. Status code: {response.StatusCode}");
    //                }
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            //Console.WriteLine($"An error occurred while downloading the image: {ex.Message}");
    //        }
    //    }
    //}

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
        await Console.Out.WriteLineAsync(url);
        var response = await client.SendAsync(request);
        await Console.Out.WriteLineAsync($"Done! {url}");
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
        //var cssContent = await DownloadCssAsync(htmlContent, url);
        var jsContent = await DownloadJavaScriptAsync(htmlContent, url);

        //await Console.Out.WriteLineAsync("Done!");


        string htmlFilePath = $"../../../res/{name}/{name}.html";
        //string cssFilePath = $"../../../res/{name}/styles.css";
        string jsFilePath = $"../../../res/{name}/script.js";

        Directory.CreateDirectory($"../../../res/{name}");

        File.WriteAllText(htmlFilePath, htmlContent);
        //File.WriteAllText(cssFilePath, cssContent);
        //Console.WriteLine(cssContent);
        //Console.WriteLine("THIS CSSSSSSSSSSSSSSSSSSSSS");
        //Console.ReadLine();
        File.WriteAllText(jsFilePath, jsContent);
    }


    static void CleanHtml(string name)
    {
        RemoveHeader(name);
        RemoveFooter(name);
        RemoveConfiguratorButton(name);
        RemoveNav(name);
        //RewriteHtmlImgTags(name);

        

        RemoveByXPath(name,"//div[@class='tag']");
        RemoveByXPath(name, "//div[@class='StyledEditorialTeaserWrapper-hJLpMs uJNJF']"); // potrebbe interessarti anche questo
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
        RemoveByXPath(name, "//section[@id='sectiongroup_1597799_1691029286_twocolumnssection']"); // manutenzione
        RemoveByXPath(name, "//section[@id='download']"); // listino e dimensioni up
        RemoveByXPath(name, "//section[@id='editorialteasersecti_686435660']"); // listino e dimensioni Polo TGI 
        RemoveByXPath(name, "//section[@id='twocolumnssection_co']"); // scopri le up in pronta consegna 
        RemoveByXPath(name, "//section[@id='highlightfeaturesect']"); // accessori originiali up
        RemoveByXPath(name, "//section[@id='app-accessori']"); // accessori originiali polo
        RemoveByXPath(name, "//section[@id='twocolumnssection_co_965766936']"); // scopri le nuove polo in pronta consegna
        RemoveByXPath(name, "//section[@id='firstlevelteasersect']"); // nuova polo GRI - nouva polo
        RemoveByXPath(name, "//section[@id='twocolumnssection_co_1473804984']"); // scopri le nuove polo GTI in pronta consegna
        RemoveByXPath(name, "//section[@id='sectiongroup_160092087_twocolumnssection']"); // pianificatore e route - nuova id3
        RemoveByXPath(name, "//section[@id='sectiongroup_copy_10']"); // manutenzione - nuova id3

        RemoveByXPath(name, "//section[@id='powerteasersection']"); // Sei pronto per la mobilità elettrica - id3

        RemoveByXPath(name, "//section[@id='sectiongroup_copy_19']"); // ID Buzz - Nuova ID3
        RemoveByXPath(name, "//section[@id='twocolumnssection']"); // Passa alla mobilità elettrica - Nuova ID3
        RemoveByXPath(name, "//section[@id='twocolumnssection_co_288746464']"); // scopri id3 in pronta consegna - Nuova ID3

        RemoveByXPath(name, "//section[@id='twocolumnssection_co_1151710981']"); // Scopri le Golf 8 in pronta consegna! - golf 8

        RemoveByXPath(name, "//section[@id='twocolumnssection_co_2089911995']"); // attiva weconnect - golf 8 eHybrid
        RemoveByXPath(name, "//section[@id='twocolumnssection_co_613358248']"); // Scopri le Golf 8 eHybrid in pronta consegna! - golf 8 eHybrid
        RemoveByXPath(name, "//section[@id='gamma']"); // Scopri gli altri modelli della gamma Golf 8 - golf 8 eHybrid

        RemoveByXPath(name, "//section[@id='twocolumnssection_co_1898709743']"); // Scopri le Golf 8 GTI in pronta consegna! - golf 8 GTI

        RemoveByXPath(name, "//section[@id='twocolumnssection_co_1730437561']"); // Scopri le Golf 8 GTD in pronta consegna! - golf 8 GTD

        RemoveByXPath(name, "//section[@id='twocolumnssection_co_152972141']"); // Scopri le Golf 8 GTE in pronta consegna! - golf 8 GTE

        RemoveByXPath(name, "//section[@id='twocolumnssection_co_954301293']"); // Scopri le Golf 8 R in pronta consegna! - golf 8 R
        RemoveByXPath(name, "//section[@id='sectiongroup_1595352']"); // Potrebbe interessarti anche questo: - golf 8 R

        RemoveByXPath(name, "//section[@id='twocolumnssection_co_478381832']"); // Scopri le T‑Cross in pronta consegna! - T‑Cross

        RemoveByXPath(name, "//section[@id='twocolumnssection_co_540207698']"); // Scopri le Taigo in pronta consegna! - Taigo
        RemoveByXPath(name, "//section[@id='twocolumnssection_co_1846930536']"); // Accessori Originali - Taigo

        RemoveByXPath(name, "//section[@id='accessori']"); // Accessori Originali - nuovo T-Roc

        RemoveByXPath(name, "//section[@id='twocolumnssection_co_1265106997']"); // attiva weconnect - Tiguan
        RemoveByXPath(name, "//section[@id='twocolumnssection_co_945866001']"); // Scopri le Tiguan in pronta consegna! - Tiguan

        RemoveByXPath(name, "//a[@href='https://vw.elli.eco/it-IT/shop/id-charger' and @target='_self' and contains(@class, 'StyledLink-sc-afbv6g')]"); // ID. Charger Online Shop - Tiguan eHybrid
        RemoveByXPath(name, "//section[@id='sectiongroup_1863522997']"); // Rivoluzionaria per natura - Tiguan eHybrid
        RemoveByXPath(name, "//section[@id='twocolumnssection_co_785219142']"); // We Connect - Tiguan eHybrid
        RemoveByXPath(name, "//section[@id='twocolumnssection_co_1477678683']"); // Scopri le Tiguan eHybrid in pronta consegna!  - Tiguan eHybrid

        RemoveByXPath(name, "//section[@id='twocolumnssection_co_1325733698']"); // Scopri le Tiguan Allspace in pronta consegna!  - Tiguan Allspace

        RemoveByXPath(name, "//section[@id='powerteasersection_c']"); // Scopri We Connect Start - ID.4 
        RemoveByXPath(name, "//section[@id='sectiongroup_copy']"); // idbuzz - ID.4 
        RemoveByXPath(name, "//section[@id='twocolumnssection_co_1576032507']"); // scopri le cose in pronta consegna - ID.4 
        RemoveByXPath(name, "//section[@id='VW-Servizi-Finanziari']"); // servizi finanziari - ID.4 
        RemoveByXPath(name, "//section[@id='sectiongroup']"); // manutenzione - ID.4 
        RemoveByXPath(name, "//span[contains(concat(' ', normalize-space(@class), ' '), ' StyledRichtextComponent-jWRnMY jlOXFF ')]"); // Pensata in grande: - ID.4 
        RemoveByXPath(name, "//a[@title='Richiedi preventivo' and @href='https://www.volkswagen.it/app/dccforms/vw-it/preventivo/it/modelselector/31200/+/+/+/+/+/carline/+/+/+/+/+/+/+' and @target='_self' and contains(@class, 'StyledButton-sc-1208ax7') and contains(@class, 'iVMxmz')]"); // Richiedi preventivo: - ID.4 

        RemoveByXPath(name, "//section[@id='twocolumnssection_co_476344241']"); // passa alla mobilità elettrica - ID.4 GTX 4MOTION
        RemoveByXPath(name, "//section[@id='sectiongroup_copy_1604267514']"); // id coso - ID.4 GTX 4MOTION
        RemoveByXPath(name, "//section[@id='sectiongroup_copy_199596178']"); // manutenzione - ID.4 GTX 4MOTION
        RemoveByXPath(name, "//section[@id='editorialteasersecti_1974484785']"); // area clienti - ID.4 GTX 4MOTION

        RemoveByXPath(name, "//section[@id='sectiongroup_copy_959684686']"); // id coso - ID.5
        RemoveByXPath(name, "//section[@id='featureclustersectio']"); // modelli della gamma - ID.5
        RemoveByXPath(name, "//section[@id='twocolumnssection_co_1413467696']"); // accessori originali - ID.5

        RemoveByXPath(name, "//section[@id='sectiongroup_copy_384789379']"); // id coso - ID.5
        RemoveByXPath(name, "//section[@id='twocolumnssection_co_1945863602']"); // accessori - ID.5

        RemoveByXPath(name, "//section[@id='twocolumnssection_co_1707616415']"); // Scopri le Golf 8 Variant Alltrack in pronta consegna! - Golf 8 variant Alltrack

        RemoveByXPath(name, "//section[@id='twocolumnssection_co_1306978135']"); // Scopri le Golf 8 Variant in pronta consegna! - Golf 8 variant

        RemoveByXPath(name, "//section[@id='twocolumnssection_co_1451156338']"); // Scopri le Golf 8 Variant eTSI in pronta consegna! - Golf 8 Variant eTSI

        RemoveByXPath(name, "//section[@id='twocolumnssection_co_1373176180']"); // We Connect - Passat Variant
        RemoveByXPath(name, "//section[@id='twocolumnssection_co_1893547274']"); // Scopri le Passat Variant in pronta consegna! - Passat Variant

        RemoveByXPath(name, "//section[@id='twocolumnssection_co_1809743779']"); // Scopri le Passat Alltrack in pronta consegna! - Passat Alltrack

        RemoveByXPath(name, "//section[@id='twocolumnssection_co_1159158138']"); // Scopri le Touran in pronta consegna! - Touran

        // inutile ID Buzz
        RemoveByXPath(name, "//section[@id='sectiongroup_copy_co_358748358_powerteasersection_c']"); // Scegli la promozione perfetta per te. - ID. Buzz
        RemoveByXPath(name, "//section[@id='scegli-la-versione']"); // Scegli una versione - ID. Buzz
        RemoveByXPath(name, "//section[@id='sectiongroup_copy_co']"); // È tempo per una nuova epoca - ID. Buzz
        RemoveByXPath(name, "//section[@id='highlightteasersecti']"); // richiedi preventivo - ID. Buzz

        //commerciali
        RemoveByXPath(name, "//section[@id='sectiongroup_copy_co_933231009']"); // promozioni cargo - caddy cargo

        RemoveByXPath(name, "//section[@id='sectiongroup_copy_co_1293155987']"); // promozioni - Transporter

        RemoveByXPath(name, "//section[@id='chargeyourbrand']"); // charge your brand - buzz cargo

        RemoveByXPath(name, "//section[@id='sectiongroup_copy_co_1592285793']"); // promozioni - crafter

        RemoveByXPath(name, "//section[@id='sectiongroup_copy_co_1589095349']"); // promozioni - kombi

        RemoveByXPath(name, "//section[@id='sectiongroup_copy_co_1456400187_powerteasersection_c']"); // promozioni - caddy

        RemoveByXPath(name, "//section[@id='sectiongroup_copy_co']"); // promozioni - caravelle

        RemoveByXPath(name, "//section[@id='sectiongroup_5370189']"); // di più - multivan
        RemoveByXPath(name, "//section[@id='focusteasersection_c']"); // Benvenuti nel mondo di Multivan - multivan
        RemoveByXPath(name, "//p[contains(text(), 'Fonte')]"); // fonte - multivan

        RemoveByXPath(name, "//section[@id='singlecolumnsection' and descendant::a]"); // benvenuto in famiglia - caddy

        RemoveByXPath(name, "//section[@id='headingsection_copy']"); // Promozioni - California

        RemoveByXPath(name, "//section[@id='focusteasersection_7903804']"); // Porta tutto sulla strada - transporter camioncino

        RemoveByXPath(name, "//section[@id='textonlyteasersectio']"); // Di più - Amarok

        RemoveByXPath(name, "//section[@id='scegli-la-versione']"); // Scegli una versione - Grand California
        RemoveByXPath(name, "//section[@id='sectiongroup_copy_co_headingsection']"); // promozioni - Grand California




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
            //Console.WriteLine($"Couldn't find divClass= {divClass}");

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

            //Console.WriteLine("\n\n\n\n\n\n");

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
        string htmlFilePath = $"../../../res/{name}/{name}.html";

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
