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
            List<Question> questions = new List<Question>();
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

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(driver.PageSource);

            SolveTrueFalse(doc, driver, questions);

            driver.Close();
        }

        private static void SolveTrueFalse(HtmlDocument doc, IWebDriver driver, List<Question> questions)
        {
            string idQuestionValue = string.Empty;
            int idQuestionValueTimes = 0;
            string idAnswerSelected = string.Empty;

            foreach (var Nodo in doc.DocumentNode.SelectNodes("//div[contains(@class, 'display_question')]").Descendants())
            {
                Question question = new Question() { Resolved = false, Answers = new List<string>() };
                HtmlAttributeCollection atts = Nodo.Attributes;
                foreach (var att in atts)
                {
                    if (att.Name.ToLower() == "id")
                    {
                        if (att.Value.Contains("answer"))
                        {
                            Regex regex = new Regex(@"(?<=question_)(.+?)(?=_)");
                            Match idQuestion = regex.Match(att.Value);
                            if (idQuestionValue != idQuestion.Value)
                            {
                                if (idQuestionValueTimes == 2)
                                {
                                    if (questions.Any(q => q.Id == idQuestion.Value))
                                    {
                                        var correctAnswers = questions.Where(q => q.Id == idQuestion.Value).FirstOrDefault().CorrectAnswers;
                                        foreach (string ans in correctAnswers) SeleniumMethods.Click(driver, ans, "Id");
                                    }
                                    else
                                    {
                                        idAnswerSelected = att.Value;
                                        SeleniumMethods.Click(driver, att.Value, "Id");
                                        return;
                                    }
                                }
                                idQuestionValue = idQuestion.Value;
                                idQuestionValueTimes = 1;
                            }
                            else idQuestionValueTimes++;
                            question.Answers.Add(att.Value);
                        }
                    }
                }
                question.Id = idQuestionValue;
            }
        }
    }
}
