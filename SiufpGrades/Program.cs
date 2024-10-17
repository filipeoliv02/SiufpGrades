#region Using Directives
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
#endregion

namespace SiufpGrades
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            #region Login Details
            if (args.Length != 2)
            {
                System.Console.WriteLine("Wrong Syntax!");
                System.Console.WriteLine("siufpgrades <login id> <login password>");
                return -1;
            }
            //Insert login details here
            string login = args[0];
            string pass = args[1];
            #endregion

            #region Initialize the Web Driver
            IWebDriver driver = new ChromeDriver();
            #endregion

            #region Login
            Login.LoginSIUFP(driver, login, pass);
            #endregion

            #region Program Itself
            PartialGrades.FileManagement(driver);
            FinalGrades.FileManagement(driver);
            return 0;
            #endregion
        }
    }
}
