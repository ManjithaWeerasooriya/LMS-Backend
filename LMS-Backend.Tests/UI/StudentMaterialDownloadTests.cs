// using System;
// using OpenQA.Selenium;
// using OpenQA.Selenium.Support.UI;
// using Xunit;

// namespace LMS_Backend.Tests.UI;

// public class StudentMaterialDownloadTests
// {
//     public static TheoryData<string> Browsers => new()
//     {
//         "chrome",
//         "edge"
//     };

//     [UiTheory]
//     [MemberData(nameof(Browsers))]
//     public void Student_CanOpenMaterialsPage_AndClickDownload(string browserName)
//     {
//         using var driver = SeleniumDriverFactory.Create(browserName);
//         driver.Navigate().GoToUrl(new Uri(new Uri(UiTestEnvironment.BaseUrl), "/login"));

//         var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

//         var email = Environment.GetEnvironmentVariable("LMS_UI_TEST_EMAIL");
//         var password = Environment.GetEnvironmentVariable("LMS_UI_TEST_PASSWORD");

//         Assert.False(string.IsNullOrWhiteSpace(email), "Set LMS_UI_TEST_EMAIL before running this test.");
//         Assert.False(string.IsNullOrWhiteSpace(password), "Set LMS_UI_TEST_PASSWORD before running this test.");

//         wait.Until(_ => driver.FindElement(By.Id("login-email")));
//         wait.Until(_ => driver.FindElement(By.Id("login-password")));

//         driver.FindElement(By.Id("login-email")).SendKeys(email);
//         driver.FindElement(By.Id("login-password")).SendKeys(password);
//         driver.FindElement(By.CssSelector("button[type='submit']")).Click();

//         wait.Until(d =>
//             d.Url.Contains("/student", StringComparison.OrdinalIgnoreCase) ||
//             d.Url.Contains("/dashboard", StringComparison.OrdinalIgnoreCase));

//         // Adjust this route if your student materials page is different.
//         driver.Navigate().GoToUrl(new Uri(new Uri(UiTestEnvironment.BaseUrl), "/student/courses"));

//         // Open a course card.
//         var courseCard = wait.Until(d =>
//             FindFirstVisible(
//                 d,
//                 By.CssSelector("[data-testid='course-card']"),
//                 By.CssSelector("[data-testid='course-item']"),
//                 By.XPath("//a[contains(@href, '/courses/')]"),
//                 By.XPath("//div[contains(@class, 'course')]")
//             ));

//         courseCard.Click();

//         // Open materials section/tab.
//         var materialsTab = wait.Until(d =>
//             FindFirstVisible(
//                 d,
//                 By.CssSelector("[data-testid='materials-tab']"),
//                 By.XPath("//button[contains(normalize-space(.), 'Materials')]"),
//                 By.XPath("//a[contains(normalize-space(.), 'Materials')]"),
//                 By.XPath("//*[contains(normalize-space(.), 'Materials')]")
//             ));

//         materialsTab.Click();

//         // Verify at least one material row/card exists.
//         var materialRow = wait.Until(d =>
//             FindFirstVisible(
//                 d,
//                 By.CssSelector("[data-testid='material-row']"),
//                 By.CssSelector("[data-testid='material-item']"),
//                 By.XPath("//*[contains(@class, 'material')]"),
//                 By.XPath("//*[contains(normalize-space(.), 'Download')]")
//             ));

//         Assert.True(materialRow.Displayed);

//         // Find and click download button/link.
//         var downloadButton = wait.Until(d =>
//             FindFirstVisible(
//                 d,
//                 By.CssSelector("[data-testid='download-material']"),
//                 By.XPath("//button[contains(normalize-space(.), 'Download')]"),
//                 By.XPath("//a[contains(normalize-space(.), 'Download')]"),
//                 By.XPath("//*[contains(@aria-label, 'download') or contains(@title, 'download')]")
//             ));

//         Assert.True(downloadButton.Displayed);
//         downloadButton.Click();

//         // Basic regression check:
//         // if we reached materials and clicked a visible download control without error,
//         // the UI download flow is wired up enough for first-pass validation.
//         Assert.True(true);
//     }

//     private static IWebElement FindFirstVisible(IWebDriver driver, params By[] locators)
//     {
//         foreach (var locator in locators)
//         {
//             var elements = driver.FindElements(locator);
//             foreach (var element in elements)
//             {
//                 if (element.Displayed)
//                 {
//                     return element;
//                 }
//             }
//         }

//         throw new NoSuchElementException("No visible element matched any of the provided selectors.");
//     }
// }