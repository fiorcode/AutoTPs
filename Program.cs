using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using ScrapySharp.Extensions;

namespace AutoTPs
{
    class Program
    {
        static void Main(string[] args)
        {
            DoLogin("https://siglo21.instructure.com");

            GetCourses();

            //select GL
            //SeleniumMethods.Click(driver, "[href*='/courses/5379']", "HRef");

            //go to TP1
            //SeleniumMethods.Click(driver, "[href*='/courses/5379/modules/items/100155']", "HRef");
            Driver.GetInstance.WebDrive.Navigate().GoToUrl("https://siglo21.instructure.com/courses/5379/quizzes/19372?module_item_id=100155");

            //initialization
            List<Question> questions = new List<Question>();
            double mark = 0;

            while (mark < 100)
            {
                List<Question> questionsCurrentTest = new List<Question>();
                double expectedMark = 0;

                //take a breath
                Task.Delay(1000);

                //take it
                Methods.Click("[href*='/courses/5379/quizzes/19372/take?user_id=90628']", "HRef");

                //scrap test page
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(Driver.GetInstance.WebDrive.PageSource);

                //load questions
                LoadQuestions(doc, questions, questionsCurrentTest);

                //complete with resolved questions
                foreach(Question q in questionsCurrentTest)
                {
                    if(q.Resolved == true)
                    {
                        foreach (string ans in q.CorrectAnswers) Methods.Click(ans, "Id");
                        expectedMark += 5;
                    }
                }
                Console.WriteLine($"Expected mark: {expectedMark}");

                //select an unresolved question
                Question unresolvedQ = questionsCurrentTest.Where(q => q.Resolved == false).FirstOrDefault();

                //select an answer
                string answerSelected = unresolvedQ.NoAttemptsAnswers.FirstOrDefault();

                Methods.Click(answerSelected, "Id");

                //take a breath
                Task.Delay(1000);

                //submit
                Methods.Click("submit_quiz_button", "Id");

                //accept the alert of incomplete answers
                Driver.GetInstance.WebDrive.SwitchTo().Alert().Accept();

                //take a breath
                Task.Delay(1000);

                //scarp results page
                doc.LoadHtml(Driver.GetInstance.WebDrive.PageSource);

                //save mark
                foreach (var Nodo in doc.DocumentNode.CssSelect(".score_value"))
                {
                    mark = Convert.ToDouble(Nodo.InnerHtml);
                }
                Console.WriteLine($"Mark: {mark}");

                //verify answer
                switch (mark - expectedMark)
                {
                    case 0:
                        unresolvedQ.NoAttemptsAnswers.Remove(answerSelected);
                        break;
                    case 5:
                        unresolvedQ.CorrectAnswers.Add(answerSelected);
                        unresolvedQ.NoAttemptsAnswers.Remove(answerSelected);
                        unresolvedQ.Resolved = true;
                        break;
                    default:
                        unresolvedQ.CorrectAnswers.Add(answerSelected);
                        unresolvedQ.NoAttemptsAnswers.Remove(answerSelected);
                        break;
                }
            }

            Driver.GetInstance.WebDrive.Close();
        }

        private static void DoLogin(string loginUrl)
        {
            Driver.GetInstance.WebDrive.Navigate().GoToUrl(loginUrl);
            Methods.EnterText("pseudonym_session[unique_id]", "lperez23", "Name");
            Methods.EnterText("pseudonym_session[password]", "fumigaRola70.", "Name");
            Methods.Click("Button--login", "ClassName");
        }

        private static void GetCourses()
        {
            HtmlDocument boardHtml = new HtmlDocument();
            boardHtml.LoadHtml(Driver.GetInstance.WebDrive.PageSource);
        }

        private static void LoadQuestions(HtmlDocument doc, List<Question> questions, List<Question> questionsCurrentTest)
        {
            string idQuestionValue = string.Empty;
            List<Question> newQuestions = new List<Question>();
            foreach (var Nodo in doc.DocumentNode.SelectNodes("//input[@id]"))
            {
                HtmlAttributeCollection atts = Nodo.Attributes;
                string answerId = atts.Where(a => a.Name.ToLower() == "id").FirstOrDefault().Value;
                Regex regex = new Regex(@"(?<=question_)(.+?)(?=_)");
                idQuestionValue = regex.Match(answerId).Value;
                if (!questions.Any(q => q.Id == idQuestionValue))
                {
                    if(!newQuestions.Any(q => q.Id == idQuestionValue))
                    {
                        string answerType = atts.Where(a => a.Name.ToLower() == "type").FirstOrDefault().Value;
                        string type;
                        if (answerType == "checkbox") type = "multiple_answers_question";
                        else type = "multiple_choice_question";
                        Question question = new Question()
                        {
                            Id = idQuestionValue,
                            Type = type,
                            Answers = { answerId }
                        };
                        newQuestions.Add(question);
                    }
                    else
                    {
                        Question question = newQuestions.Where(q => q.Id == idQuestionValue).FirstOrDefault();
                        question.Answers.Add(answerId);
                    }
                }
                else
                {
                    if(questionsCurrentTest.Where(q => q.Id == idQuestionValue).FirstOrDefault() == null)
                    {
                        Question question = questions.Where(q => q.Id == idQuestionValue).FirstOrDefault();
                        questionsCurrentTest.Add(question);
                    }
                }
            }
            foreach(Question q in newQuestions)
            {
                q.FullyLoaded = true;
                q.NoAttemptsAnswers = q.Answers;
                if (q.Answers.Count == 2) q.Type = "true_false_question";
                questionsCurrentTest.Add(q);
                questions.Add(q);
            }
        }
    }
}
