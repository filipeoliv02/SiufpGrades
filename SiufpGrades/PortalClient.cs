using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SiufpGrades.Core;

namespace SiufpGrades;

/// <summary>Selenium-based access to portal.ufp.pt. Only responsible for scraping, not for diffing/storage.</summary>
internal sealed class PortalClient : IDisposable
{
    private static readonly Uri BaseUri = new("https://portal.ufp.pt/");
    private static readonly Uri LoginUri = new(BaseUri, "authentication.aspx");
    private static readonly Uri PartialGradesUri = new(BaseUri, "Notas/Parcial.aspx");
    private static readonly Uri FinalGradesUri = new(BaseUri, "Notas/FinalProv.aspx");

    private static readonly By UserField = By.Id("ctl00_ContentPlaceHolder1_AccordionPane1_content_txtLogin");
    private static readonly By PasswordField = By.Id("ctl00_ContentPlaceHolder1_AccordionPane1_content_txtPassword");
    private static readonly By LoginButton = By.Id("ctl00_ContentPlaceHolder1_AccordionPane1_content_Button1");
    private static readonly By ResultsTable = By.ClassName("tablewithresults");

    private readonly ChromeDriver _driver;
    private readonly WebDriverWait _wait;

    public PortalClient(bool headless, TimeSpan timeout)
    {
        var options = new ChromeOptions();
        if (headless)
        {
            options.AddArgument("--headless=new");
        }
        options.AddArgument("--window-size=1280,1024");
        options.AddArgument("--disable-extensions");

        // Selenium Manager (bundled with Selenium.WebDriver) locates or downloads a matching chromedriver.
        var service = ChromeDriverService.CreateDefaultService();
        service.SuppressInitialDiagnosticInformation = true;
        service.HideCommandPromptWindow = true;

        _driver = new ChromeDriver(service, options, timeout + TimeSpan.FromSeconds(30));
        _driver.Manage().Timeouts().PageLoad = timeout;
        _wait = new WebDriverWait(_driver, timeout);
        _wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));
    }

    public void Login(Credentials credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        _driver.Navigate().GoToUrl(LoginUri);

        var user = _wait.Until(d => d.FindElement(UserField));
        user.Clear();
        user.SendKeys(credentials.User);
        var password = _driver.FindElement(PasswordField);
        password.Clear();
        password.SendKeys(credentials.Password);
        _driver.FindElement(LoginButton).Click();

        try
        {
            // A successful login navigates away from the authentication page.
            _wait.Until(d => !IsOnLoginPage(d) && IsDocumentReady(d));
        }
        catch (WebDriverTimeoutException ex)
        {
            throw new PortalException("Login failed: still on the authentication page. Check your credentials.", ex);
        }
    }

    public IReadOnlyList<PartialGrade> GetPartialGrades() =>
        GradeTableParser.ParsePartial(ReadResultTable(PartialGradesUri));

    public IReadOnlyList<FinalGrade> GetFinalGrades() =>
        GradeTableParser.ParseFinal(ReadResultTable(FinalGradesUri));

    /// <returns>Cell texts of every data row (header row excluded). Empty when the page has no results table.</returns>
    private List<IReadOnlyList<string>> ReadResultTable(Uri page)
    {
        _driver.Navigate().GoToUrl(page);
        _wait.Until(IsDocumentReady);

        if (IsOnLoginPage(_driver))
        {
            throw new PortalException($"Redirected to the login page while opening {page.AbsolutePath}; session not authenticated.");
        }

        var table = _driver.FindElements(ResultsTable).FirstOrDefault();
        if (table is null)
        {
            return [];
        }

        return table.FindElements(By.TagName("tr"))
            .Skip(1) // header
            .Select(row => (IReadOnlyList<string>)row.FindElements(By.TagName("td")).Select(td => td.Text ?? string.Empty).ToList())
            .ToList();
    }

    private static bool IsOnLoginPage(IWebDriver driver) =>
        driver.Url.Contains("authentication.aspx", StringComparison.OrdinalIgnoreCase);

    private static bool IsDocumentReady(IWebDriver driver) =>
        driver is IJavaScriptExecutor js
        && "complete".Equals(js.ExecuteScript("return document.readyState") as string, StringComparison.Ordinal);

    public void Dispose()
    {
        // Quit() closes the browser and the chromedriver process; Dispose() alone may leave Chrome running.
        try
        {
            _driver.Quit();
        }
        finally
        {
            _driver.Dispose();
        }
    }
}

internal sealed class PortalException : Exception
{
    public PortalException(string message) : base(message) { }

    public PortalException(string message, Exception inner) : base(message, inner) { }
}
