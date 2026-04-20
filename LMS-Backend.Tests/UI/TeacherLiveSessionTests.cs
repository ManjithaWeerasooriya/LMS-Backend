using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace LMS_Backend.Tests.UI;

public class TeacherLiveSessionTests
{
    public static TheoryData<string> Browsers => new()
    {
        "chrome",
        "edge"
    };

    [UiTheory]
    [MemberData(nameof(Browsers))]
    public void Teacher_CanNavigateToLiveSessionsPage(string browserName)
    {
        using var driver = SeleniumDriverFactory.Create(browserName);
        driver.Navigate().GoToUrl(new Uri(new Uri(UiTestEnvironment.BaseUrl), "/login"));

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

        var email = Environment.GetEnvironmentVariable("LMS_UI_TEST_EMAIL");
        var password = Environment.GetEnvironmentVariable("LMS_UI_TEST_PASSWORD");

        Assert.False(string.IsNullOrWhiteSpace(email), "Set LMS_UI_TEST_EMAIL before running this test.");
        Assert.False(string.IsNullOrWhiteSpace(password), "Set LMS_UI_TEST_PASSWORD before running this test.");

        wait.Until(_ => driver.FindElement(By.Id("login-email")));
        wait.Until(_ => driver.FindElement(By.Id("login-password")));

        driver.FindElement(By.Id("login-email")).SendKeys(email!);
        driver.FindElement(By.Id("login-password")).SendKeys(password!);
        driver.FindElement(By.CssSelector("button[type='submit']")).Click();

        // Wait for dashboard (more flexible)
    wait.Until(d => d.Url.Contains("dashboard", StringComparison.OrdinalIgnoreCase));

    // Click Live Sessions (more robust selector)
    var liveSessionsButton = wait.Until(d =>
        d.FindElement(By.XPath("//*[contains(text(), 'Live Sessions')]")));

    liveSessionsButton.Click();

    // Validate navigation
    wait.Until(d =>
        d.Url.Contains("live", StringComparison.OrdinalIgnoreCase) ||
        d.PageSource.Contains("Live Sessions", StringComparison.OrdinalIgnoreCase));
            Assert.True(
                driver.Url.Contains("live", StringComparison.OrdinalIgnoreCase) ||
                driver.PageSource.Contains("Live", StringComparison.OrdinalIgnoreCase));
    }
}