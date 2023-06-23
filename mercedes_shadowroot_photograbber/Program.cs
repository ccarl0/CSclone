using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager;
using AngleSharp.Attributes;
using HtmlAgilityPack;
using OpenQA.Selenium.Support.UI;
using System.Net;

internal class Program
{
    private static void Main(string[] args)
    {

        DomTest();
    }

    static public void DomTest()
    {
        new DriverManager().SetUpDriver(new ChromeConfig());
        ChromeDriver driver = new ChromeDriver();

        driver.Navigate().GoToUrl("https://www.mercedes-benz.it/passengercars/models.html?group=all&subgroup=all.saloon&view=BODYTYPE");
        Thread.Sleep(5000); // Adjust the delay as needed

        ((IJavaScriptExecutor)driver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight)");
        Thread.Sleep(5000);

        IWebElement shadowHost = driver.FindElement(By.Id("first-web-component"));
        //Console.WriteLine(shadowHost.GetAttribute("class"));

        var element = shadowHost.GetShadowRoot()
            .FindElement(By.CssSelector("section.wb-grid-col-mq1-12.wb-grid-col-offset-mq1-0.wb-grid-col-mq5-9.wb-grid-col-offset-mq5-0"));

        Thread.Sleep(3000);

        var imgElements = element.FindElements(By.TagName("img"));

        List<string> imgUrList = new();

        int i = 0;
        foreach (var imgElement in imgElements)
        {
            string src = imgElement.GetAttribute("src");
            src = src.Split("rect=")[0];
            src = $"{src}rect=(0,0,2000,2000)";
            imgUrList.Add(src);
            Console.WriteLine(src);
            DownloadImage(src, i);
            i++;
        }

        File.WriteAllLines("../../../res/url.txt", imgUrList);
    }

    private static void DownloadImage(string imageUrl, int i)
    {
        using (WebClient webClient = new WebClient())
        {
            webClient.DownloadFile(imageUrl, $"../../../res/{i.ToString()}.png");
        }
    }
}