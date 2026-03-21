using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace LMS_Backend.Tests.UI;

public class LoginPageTests
{
    public static TheoryData<string> Browsers => new()
    {
        "chrome",
        "edge"
    };

    [UiTheory]
    [MemberData(nameof(Browsers))]
    public void LoginPage_ShowsValidationErrors_ForEmptyFields(string browserName)
    {
        using var driver = SeleniumDriverFactory.Create(browserName);
        driver.Navigate().GoToUrl(new Uri(new Uri(UiTestEnvironment.BaseUrl), "/login"));

        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

        wait.Until(_ => driver.FindElement(By.Id("login-email")));
        wait.Until(_ => driver.FindElement(By.Id("login-password")));

        var submitButton = driver.FindElement(By.CssSelector("button[type='submit']"));
        submitButton.Click();

        wait.Until(_ => driver.FindElement(By.Id("login-email-error")));
        wait.Until(_ => driver.FindElement(By.Id("login-password-error")));

        var emailError = driver.FindElement(By.Id("login-email-error"));
        var passwordError = driver.FindElement(By.Id("login-password-error"));

        Assert.Contains("Email is required", emailError.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Password is required", passwordError.Text, StringComparison.OrdinalIgnoreCase);
    }
}
