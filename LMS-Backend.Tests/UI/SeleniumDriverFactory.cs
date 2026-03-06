using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;

namespace LMS_Backend.Tests.UI;

public static class SeleniumDriverFactory
{
    public static IWebDriver Create(string browserName)
    {
        return browserName.ToLowerInvariant() switch
        {
            "chrome" => CreateChromeDriver(),
            "edge" => CreateEdgeDriver(),
            _ => throw new ArgumentOutOfRangeException(nameof(browserName), browserName,
                "Supported browsers are 'chrome' and 'edge'.")
        };
    }

    private static IWebDriver CreateChromeDriver()
    {
        var options = new ChromeOptions();

        options.AddArgument("--headless=new");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--window-size=1920,1080");

        return new ChromeDriver(options);
    }

    private static IWebDriver CreateEdgeDriver()
    {
        var options = new EdgeOptions();

        options.AddArgument("--headless=new");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--window-size=1920,1080");

        return new EdgeDriver(options);
    }
}