#region Using Directives
using OpenQA.Selenium;
using System;
using System.IO;
using System.Linq;
using System.Text;
#endregion

namespace SiufpGrades
{
    internal sealed class FinalGrades
    {
        #region Atributes
        private string className;
        private string gradeType;
        private string oral;
        private string written;
        private string final;
        private string registDate;
        #endregion

        #region Constructors
        public FinalGrades()
        {
        }

        public FinalGrades(string className, string gradeType, string oral, string written, string final, string registDate)
        {
            this.className = className;
            this.gradeType = gradeType;
            this.oral = oral;
            this.written = written;
            this.final = final;
            this.registDate = registDate;
        }
        #endregion

        #region Paths
        private const string finalGrades = @"..\csv\finalGrades.csv";
        private const string finalGradesTemp = @"..\csv\finalGradesTemp.csv";
        #endregion

        #region Methods
        private static FinalGrades[] GetFinalGrades(IWebDriver driver)
        {
            driver.Navigate().GoToUrl("https://portal.ufp.pt/Notas/FinalProv.aspx");
            IWebElement table;
            try
            {
                table = driver.FindElement(By.ClassName("tablewithresults"));
            }
            catch (Exception)
            {
                return null;
            }
            var rows = table.FindElements(By.TagName("tr"));

            var h = 0;
            var i = 0;
            var j = 1;
            var k = 2;
            var l = 3;
            var m = 4;
            var n = 5;
            var o = 8;

            FinalGrades[] allFinalGrades = new FinalGrades[rows.Count - 1];
            foreach (var row in rows.Skip(1))
            {
                FinalGrades currentRow = new();
                foreach (var entry in row.FindElements(By.TagName("td")))
                {
                    if (i == j)
                    {
                        currentRow.className = entry.Text;
                        j += 10;
                    }
                    if (i == k)
                    {
                        currentRow.gradeType = entry.Text;
                        k += 10;
                    }
                    if (i == l)
                    {
                        currentRow.oral = entry.Text;
                        l += 10;
                    }
                    if (row.FindElements(By.TagName("td")).Count == 10 && i == m)
                    {
                        currentRow.written = entry.Text;
                        m += 10;
                    }

                    if (row.FindElements(By.TagName("td")).Count == 10 && i == n)
                    {
                        currentRow.final = entry.Text;
                        n += 10;
                    }
                    if (row.FindElements(By.TagName("td")).Count == 10 && i == o)
                    {
                        currentRow.registDate = entry.Text;
                        o += 10;
                    }
                    i++;
                }
                allFinalGrades[h] = currentRow;
                h++;
            }
            return allFinalGrades;
        }

        private static void PrintFinalToFile(FinalGrades[] allGrades)
        {
            var csv = new StringBuilder();
            var header = string.Format("Class Name,Grade Type,Oral Grade,Written Grade,Final Grade,Regist Date");
            csv.AppendLine(header);
            if (allGrades != null)
            {
                foreach (var grade in allGrades)
                {
                    var newLine = string.Format("{0},{1},{2},{3},{4},{5}", grade.className, grade.gradeType, grade.oral, grade.written, grade.final, grade.registDate);
                    csv.AppendLine(newLine);
                }
            }
            File.WriteAllText(finalGradesTemp, csv.ToString(), Encoding.UTF8);
        }

        private static bool FileEquals()
        {
            byte[] fileOld;
            try
            {
                fileOld = File.ReadAllBytes(finalGrades);
            }
            catch (Exception)
            {
                return false;
            }
            byte[] fileNew = File.ReadAllBytes(finalGradesTemp);
            if (fileOld.Length == fileNew.Length)
            {
                for (int i = 0; i < fileOld.Length; i++)
                {
                    if (fileOld[i] != fileNew[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public static void FileManagement(IWebDriver driver)
        {
            PrintFinalToFile(GetFinalGrades(driver));
            if (FileEquals())
            {
                File.Delete(finalGradesTemp);
                Console.WriteLine("Nothing New in Final");
                return;
            }
            File.Delete(finalGrades);
            File.Move(finalGradesTemp, finalGrades);
            Console.WriteLine("Changes Detected in Final!");
        }
        #endregion
    }
}
