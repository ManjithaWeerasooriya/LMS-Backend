using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace LMS_Backend.Tests.UI;

public class TeacherLiveBroadcastTests
{
    public static TheoryData<string> Browsers => new()
    {
        "chrome",
        "edge"
    };

    [UiTheory]
    [MemberData(nameof(Browsers))]
    public void Teacher_CanLogin_AndReachDashboard(string browserName)
    {
        using var driver = SeleniumDriverFactory.Create(browserName);
        driver.Navigate().GoToUrl(new Uri(new Uri(UiTestEnvironment.BaseUrl), "/login"));

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

        var email = Environment.GetEnvironmentVariable("LMS_UI_TEST_EMAIL");
        var password = Environment.GetEnvironmentVariable("LMS_UI_TEST_PASSWORD");

        Assert.False(string.IsNullOrWhiteSpace(email));
        Assert.False(string.IsNullOrWhiteSpace(password));

        wait.Until(_ => driver.FindElement(By.Id("login-email")));

        driver.FindElement(By.Id("login-email")).SendKeys(email!);
        driver.FindElement(By.Id("login-password")).SendKeys(password!);
        driver.FindElement(By.CssSelector("button[type='submit']")).Click();

        wait.Until(d => d.Url.Contains("dashboard", StringComparison.OrdinalIgnoreCase));

        Assert.Contains("dashboard", driver.Url, StringComparison.OrdinalIgnoreCase);
    }

    [UiTheory]
    [MemberData(nameof(Browsers))]
    public void Teacher_CanSeeSidebarOptions(string browserName)
    {
        using var driver = SeleniumDriverFactory.Create(browserName);
        driver.Navigate().GoToUrl(new Uri(new Uri(UiTestEnvironment.BaseUrl), "/login"));

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

        var email = Environment.GetEnvironmentVariable("LMS_UI_TEST_EMAIL");
        var password = Environment.GetEnvironmentVariable("LMS_UI_TEST_PASSWORD");

        Assert.False(string.IsNullOrWhiteSpace(email));
        Assert.False(string.IsNullOrWhiteSpace(password));

        wait.Until(_ => driver.FindElement(By.Id("login-email")));

        driver.FindElement(By.Id("login-email")).SendKeys(email!);
        driver.FindElement(By.Id("login-password")).SendKeys(password!);
        driver.FindElement(By.CssSelector("button[type='submit']")).Click();

        wait.Until(d => d.Url.Contains("dashboard", StringComparison.OrdinalIgnoreCase));

        // Check sidebar items that actually exist
        Assert.Contains("Dashboard", driver.PageSource);
        Assert.True(
        driver.PageSource.Contains("Courses", StringComparison.OrdinalIgnoreCase) ||
        driver.PageSource.Contains("My Courses", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("Quizzes", driver.PageSource);
    }
}