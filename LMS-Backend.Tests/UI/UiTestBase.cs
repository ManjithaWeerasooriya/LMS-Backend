using System;
using OpenQA.Selenium;

namespace LMS_Backend.Tests.UI;

public abstract class UiTestBase : IDisposable
{
    protected readonly IWebDriver Driver;
    protected readonly string BaseUrl;

    protected UiTestBase(string browserName)
    {
        Driver = SeleniumDriverFactory.Create(browserName);

        BaseUrl = Environment.GetEnvironmentVariable("LMS_UI_BASE_URL")
                  ?? "http://localhost:3000";
    }

    public void Dispose()
    {
        Driver.Quit();
        Driver.Dispose();
    }
}

