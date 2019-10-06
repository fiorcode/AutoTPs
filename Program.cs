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

            //scrap it
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(driver.PageSource);

            //load questions
            LoadQuestions(doc, questions);

            Question questionTrueFalse = questions.Where(
                q => q.Resolved == false
                && q.Type == "multiple_choice_question"
                && q.Answers.Count == 2)
                .FirstOrDefault();

            string answerSelected = questionTrueFalse.Answers.FirstOrDefault();

            SeleniumMethods.Click(driver, answerSelected, "Id");

            SeleniumMethods.Click(driver, "submit_quiz_button", "Id");

            driver.SwitchTo().Alert().Accept();

            doc.LoadHtml(driver.PageSource);

            double nota = -1;

            foreach (var Nodo in doc.DocumentNode.CssSelect(".score_value"))
            {
                nota = Convert.ToDouble(Nodo.InnerHtml);
            }
            if (nota == 5)
            {
                questionTrueFalse.CorrectAnswers.Add(answerSelected);
                questionTrueFalse.WrongAnswers.Add(questionTrueFalse.Answers.Where(a => a != answerSelected).FirstOrDefault());
                questionTrueFalse.Resolved = true;
            }
            else
            {
                questionTrueFalse.CorrectAnswers.Add(questionTrueFalse.Answers.Where(a => a != answerSelected).FirstOrDefault());
                questionTrueFalse.WrongAnswers.Add(answerSelected);
                questionTrueFalse.Resolved = true;
            }

            driver.Close();
        }

        private static void LoadQuestions(HtmlDocument doc, List<Question> questions)
        {
            string idQuestionValue = string.Empty;

            foreach (var Nodo in doc.DocumentNode.SelectNodes("//input[@id]"))
            {
                HtmlAttributeCollection atts = Nodo.Attributes;
                string answerId = atts.Where(a => a.Name.ToLower() == "id").FirstOrDefault().Value;

                Regex regex = new Regex(@"(?<=question_)(.+?)(?=_)");
                idQuestionValue = regex.Match(answerId).Value;

                if (!questions.Any(q => q.Id == idQuestionValue))
                {
                    string answerType = atts.Where(a => a.Name.ToLower() == "type").FirstOrDefault().Value;
                    string type;
                    if (answerType == "checkbox") type = "multiple_answers_question";
                    else type = "multiple_choice_question";
                    Question question = new Question()
                    {
                        Id = idQuestionValue,
                        Type = type,
                        Resolved = false,
                    };
                    question.Answers.Add(answerId);
                    questions.Add(question);
                }
                else
                {
                    Question question = questions.Where(q => q.Id == idQuestionValue).FirstOrDefault();
                    question.Answers.Add(answerId);
                }
            }
        }

        /*private static void LoadQuestions(IWebDriver driver, HtmlDocument doc, List<Question> questions)
        {
            string idQuestionValue = string.Empty;
            int idQuestionValueTimes = 0;
            string idAnswerSelected = string.Empty;

            foreach (var Nodo in doc.DocumentNode.SelectNodes("//div[contains(@class, 'display_question')]").Descendants())
            {
                Question question = new Question() { Resolved = false};
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
        }*/

        private static void CleanAllAnswers(IWebDriver driver, HtmlDocument doc)
        {
            foreach (var Nodo in doc.DocumentNode.SelectNodes("//div[contains(@class, 'display_question')]").Descendants())
            {
                HtmlAttributeCollection atts = Nodo.Attributes;
                foreach (var att in atts)
                {
                    if (att.Name.ToLower() == "id")
                    {
                        if (att.Value.Contains("answer"))
                        {
                            ((IJavaScriptExecutor)driver).ExecuteScript($"document.getElementById('{att.Value}').checked = false");
                        }
                    }
                }
            }
        }
    }
}
