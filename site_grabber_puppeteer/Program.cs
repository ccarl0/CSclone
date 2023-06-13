using PuppeteerSharp;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var fullUrl = "https://www.mercedes-benz.it/passengercars/models.html?group=all";

        List<string> carList = new List<string>();

        var options = new LaunchOptions()
        {
            Headless = true,
            ExecutablePath = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe"
        };

        var browser = await Puppeteer.LaunchAsync(options, null);
        var page = await browser.NewPageAsync();
        await page.GoToAsync(fullUrl);
        await Console.Out.WriteLineAsync(page.ToString());
    }
}