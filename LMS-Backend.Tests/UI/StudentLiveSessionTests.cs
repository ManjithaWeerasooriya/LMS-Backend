using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace LMS_Backend.Tests.UI;

public class StudentLiveSessionTests
{
    public static TheoryData<string> Browsers => new()
    {
        "chrome",
        "edge"
    };

    [UiTheory]
    [MemberData(nameof(Browsers))]
    public void Student_CanLogin_AndReachDashboard(string browserName)
    {
        using var driver = SeleniumDriverFactory.Create(browserName);
        driver.Navigate().GoToUrl(new Uri(new Uri(UiTestEnvironment.BaseUrl), "/login"));

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

        var email = Environment.GetEnvironmentVariable("LMS_UI_TEST_STUDENT_EMAIL");
        var password = Environment.GetEnvironmentVariable("LMS_UI_TEST_STUDENT_PASSWORD");

        Assert.False(string.IsNullOrWhiteSpace(email), "Set LMS_UI_TEST_STUDENT_EMAIL before running this test.");
        Assert.False(string.IsNullOrWhiteSpace(password), "Set LMS_UI_TEST_STUDENT_PASSWORD before running this test.");

        wait.Until(_ => driver.FindElement(By.Id("login-email")));
        wait.Until(_ => driver.FindElement(By.Id("login-password")));

        driver.FindElement(By.Id("login-email")).SendKeys(email!);
        driver.FindElement(By.Id("login-password")).SendKeys(password!);
        driver.FindElement(By.CssSelector("button[type='submit']")).Click();

        wait.Until(d =>
            d.Url.Contains("/student/dashboard", StringComparison.OrdinalIgnoreCase) ||
            d.Url.Contains("/dashboard", StringComparison.OrdinalIgnoreCase));

        Assert.True(
            driver.Url.Contains("/student/dashboard", StringComparison.OrdinalIgnoreCase) ||
            driver.Url.Contains("/dashboard", StringComparison.OrdinalIgnoreCase));
    }
}