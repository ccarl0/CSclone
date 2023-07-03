using AngleSharp.Dom;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using PuppeteerSharp;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml.Linq;
using volvo_model_kb_grabber.Helper;


namespace volvo_model_kb_grabber;

internal class Program
{
    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    public const byte VK_CONTROL = 0x11;
    public const byte VK_S = 0x53;
    public const byte VK_RETURN = 0x0D;
    public const uint KEYEVENTF_KEYUP = 0x2;


    static LaunchOptions options = new LaunchOptions()
    {
        Headless = false,
        ExecutablePath = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe",
        Product = Product.Chrome,
        DefaultViewport = new ViewPortOptions()
        {
            Height = 1440,
            Width = 2160,
            IsLandscape = true,
            IsMobile = true,

        }
    };
    private static async Task Main(string[] args)
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


        foreach (var modelURI in modelsURIList)
        {
            Console.WriteLine($"{modelURI}");
            var browser = Puppeteer.LaunchAsync(options).Result;
            Thread.Sleep(2000);
            Thread.Sleep(2000);
            await Console.Out.WriteLineAsync(modelURI);
            await browser.PagesAsync().Result[0].GoToAsync("﻿https://www.google.com");
            Thread.Sleep(2000);


            Thread.Sleep(10000);
            // Simulate pressing Ctrl
            keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);

            // Simulate pressing S
            keybd_event(VK_S, 0, 0, UIntPtr.Zero);

            // Simulate releasing S
            keybd_event(VK_S, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            // Simulate releasing Ctrl
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            Thread.Sleep(200);
            // Simulate pressing Enter
            keybd_event(VK_RETURN, 0, 0, UIntPtr.Zero);

            // Simulate releasing Enter
            keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        //await Task.WhenAll(tasks);
        stopwatch.Stop();

        Console.WriteLine($"Elapsed: {stopwatch.Elapsed}");

    }
}
