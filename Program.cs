using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using ScrapySharp.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AutoTPs
{
    class Program
    {
        static void Main(string[] args)
        {
            IWebDriver driver = new ChromeDriver(".");

            //login
            driver.Navigate().GoToUrl("https://siglo21.instructure.com/login/canvas");
            SeleniumMethods.EnterText(driver, "pseudonym_session[unique_id]", "lperez23", "Name");
            SeleniumMethods.EnterText(driver, "pseudonym_session[password]", "fumigaRola70.", "Name");
            SeleniumMethods.Click(driver, "Button--login", "ClassName");

            //select GL
            //SeleniumMethods.Click(driver, "[href*='/courses/5379']", "HRef");

            //go to TP1
            //SeleniumMethods.Click(driver, "[href*='/courses/5379/modules/items/100155']", "HRef");
            driver.Navigate().GoToUrl("https://siglo21.instructure.com/courses/5379/quizzes/19372?module_item_id=100155");
        
            //take it
            SeleniumMethods.Click(driver, "[href*='/courses/5379/quizzes/19372/take?user_id=90628']", "HRef");

            List<Question> questions = new List<Question>();
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(driver.PageSource);
            string idAnswerValue = string.Empty;
            int idAnswerValueTimes = 0;
            foreach (var Nodo in doc.DocumentNode.SelectNodes("//div[contains(@class, 'display_question')]").Descendants())
            {
                Question question = new Question();
                HtmlAttributeCollection atts = Nodo.Attributes;
                foreach (var att in atts)
                {
                    if (att.Name.ToLower() == "id")
                    {
                        if (att.Value.Contains("answer"))
                        {
                            Regex regex = new Regex(@"(?<=question_)(.+?)(?=_)");
                            Match idAnswer = regex.Match(att.Value);

                            if (idAnswerValue != idAnswer.Value)
                            {
                                if (idAnswerValueTimes > 1 ) Console.WriteLine($"{idAnswerValue} aparece {idAnswerValueTimes} veces");
                                if (idAnswerValueTimes == 2)
                                {

                                }
                                idAnswerValue = idAnswer.Value;
                                idAnswerValueTimes = 1;
                            }
                            else idAnswerValueTimes++;
                        }
                    }
                }
            }
            driver.Close();
        }
    }
}
