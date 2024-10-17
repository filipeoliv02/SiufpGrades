using OpenQA.Selenium;

namespace SiufpGrades
{
    internal sealed class Login
    {
        public static void LoginSIUFP(IWebDriver driver, string id, string password)
        {
            driver.Navigate().GoToUrl("https://portal.ufp.pt/authentication.aspx");
            driver.FindElement(By.Id("ctl00_ContentPlaceHolder1_AccordionPane1_content_txtLogin")).SendKeys(id);
            driver.FindElement(By.Id("ctl00_ContentPlaceHolder1_AccordionPane1_content_txtPassword")).SendKeys(password);
            driver.FindElement(By.Id("ctl00_ContentPlaceHolder1_AccordionPane1_content_Button1")).Click();
        }

    }
}
