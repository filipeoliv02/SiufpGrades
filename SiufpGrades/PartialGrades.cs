#region Using Directives
using OpenQA.Selenium;
using System;
using System.IO;
using System.Linq;
using System.Text;
#endregion

namespace SiufpGrades
{
    internal sealed class PartialGrades
    {
        #region Atributes
        private string className;
        private string gradeType;
        private string grade;
        #endregion

        #region Constructors
        public PartialGrades()
        {
        }
        public PartialGrades(string className, string gradeType, string grade)
        {
            this.className = className;
            this.gradeType = gradeType;
            this.grade = grade;
        }
        #endregion

        #region Paths
        private const string partialGrades = @"..\csv\partialGrades.csv";
        private const string partialGradesTemp = @"..\csv\partialGradesTemp.csv";
        #endregion

        #region Methods
        private static PartialGrades[] GetPartialGrades(IWebDriver driver)
        {
            driver.Navigate().GoToUrl("https://portal.ufp.pt/Notas/Parcial.aspx");
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

            PartialGrades[] allPartialGrades = new PartialGrades[rows.Count - 1];
            foreach (var row in rows.Skip(1))
            {
                PartialGrades currentRow = new();
                foreach (var entry in row.FindElements(By.TagName("td")))
                {
                    if (entry.Text != null && i == j)
                    {
                        currentRow.className = entry.Text;
                        j += 6;
                    }
                    if (entry.Text != null && i == k)
                    {
                        currentRow.gradeType = entry.Text;
                        k += 6;
                    }
                    if (entry.Text != null && i == l)
                    {
                        currentRow.grade = entry.Text;
                        l += 6;
                    }
                    i++;
                }
                allPartialGrades[h] = currentRow;
                h++;
            }
            return allPartialGrades;
        }

        private static void PrintPartialToFile(PartialGrades[] allGrades)
        {
            var csv = new StringBuilder();
            var header = string.Format("ClassName,GradeType,Grade");
            csv.AppendLine(header);
            if (allGrades != null)
            {
                foreach (var grade in allGrades)
                {
                    var newLine = string.Format("{0},{1},{2}", grade.className, grade.gradeType, grade.grade);
                    csv.AppendLine(newLine);
                }
            }
            File.WriteAllText(partialGradesTemp, csv.ToString(), Encoding.UTF8);
        }

        private static bool FileEquals()
        {
            byte[] fileOld;
            try
            {
                fileOld = File.ReadAllBytes(partialGrades);
            }
            catch (Exception)
            {
                return false;
            }
            byte[] fileNew = File.ReadAllBytes(partialGradesTemp);
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
            PrintPartialToFile(GetPartialGrades(driver));
            if (FileEquals())
            {
                File.Delete(partialGradesTemp);
                Console.WriteLine("Nothing New in Partial");
                return;
            }
            File.Delete(partialGrades);
            File.Move(partialGradesTemp, partialGrades);
            Console.WriteLine("Changes Detected in Partial!");
        }
        #endregion
    }
}
