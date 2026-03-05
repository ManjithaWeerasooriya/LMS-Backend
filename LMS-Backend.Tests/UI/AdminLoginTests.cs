using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace LMS_Backend.Tests.UI;

public class AdminLoginTests
{
    public static TheoryData<string> Browsers => new()
    {
        "chrome",
        "edge"
    };

    [Theory]
    [MemberData(nameof(Browsers))]
    public void AdminUser_CanLogin_AndSeeAdminPanel(string browserName)
    {
        using var driver = SeleniumDriverFactory.Create(browserName);

        var baseUrl = Environment.GetEnvironmentVariable("LMS_UI_BASE_URL")
                      ?? "http://localhost:3000";

        driver.Navigate().GoToUrl(new Uri(new Uri(baseUrl), "/login"));

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

        // Fill in admin credentials
        wait.Until(_ => driver.FindElement(By.Id("login-email")));
        wait.Until(_ => driver.FindElement(By.Id("login-password")));

        driver.FindElement(By.Id("login-email")).SendKeys("admin@lms.local");
        driver.FindElement(By.Id("login-password")).SendKeys("Admin123!");

        driver.FindElement(By.CssSelector("button[type='submit']")).Click();

        // Wait until redirected to the admin dashboard and the admin layout is visible
        wait.Until(d => d.Url.Contains("/dashboard/admin", StringComparison.OrdinalIgnoreCase));
        wait.Until(d => d.FindElement(By.XPath("//h1[contains(normalize-space(.), 'Admin Panel')]")));

        Assert.Contains("/dashboard/admin", driver.Url, StringComparison.OrdinalIgnoreCase);
    }
}

