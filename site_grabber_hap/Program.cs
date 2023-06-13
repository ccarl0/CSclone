using System;
using System.Net.Http;
using HtmlAgilityPack;

public class Program
{
    public static void Main()
    {
        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("https://www.mercedes-benz.it");
        var document = new HtmlDocument();
        document.LoadHtml(httpClient.GetStringAsync("/passengercars/models").GetAwaiter().GetResult());
        var res = document.DocumentNode.SelectNodes("//div[contains(concat(' ', @class, ' '), ' aem-Grid aem-Grid--12 aem-Grid--default--12  ')]");

        if (res != null)
        {
            foreach (var node in res)
            {
                Console.WriteLine(node.InnerHtml);
            }
        }
        else
        {
            Console.WriteLine("NULL!");
        }
    }
}
