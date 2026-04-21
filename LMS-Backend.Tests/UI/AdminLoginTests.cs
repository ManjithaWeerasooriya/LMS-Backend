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

    [UiTheory]
    [MemberData(nameof(Browsers))]
    public void AdminUser_CanLogin_AndSeeAdminPanel(string browserName)
    {
        using var driver = SeleniumDriverFactory.Create(browserName);
        driver.Navigate().GoToUrl(new Uri(new Uri(UiTestEnvironment.BaseUrl), "/login"));

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

        wait.Until(_ => driver.FindElement(By.Id("login-email")));
        wait.Until(_ => driver.FindElement(By.Id("login-password")));

        driver.FindElement(By.Id("login-email")).SendKeys("admin@lms.local");
        driver.FindElement(By.Id("login-password")).SendKeys("Admin123!");
        driver.FindElement(By.CssSelector("button[type='submit']")).Click();

        wait.Until(d => d.Url.Contains("/teacher/dashboard", StringComparison.OrdinalIgnoreCase));

        wait.Until(d =>
            d.FindElement(By.XPath("//*[contains(normalize-space(.), 'System Teacher')]")));

        Assert.Contains("/teacher/dashboard", driver.Url, StringComparison.OrdinalIgnoreCase);
    }
}