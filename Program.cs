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
        private static string baseUrl = "https://siglo21.instructure.com";
        static void Main(string[] args)
        {
            DoLogin(baseUrl);

            List<string> Courses = GetCourses();
            
            foreach(string c in Courses)
            {
                Driver.GetInstance.WebDrive.Navigate().GoToUrl($"{baseUrl}{c}");
                List<string> tpLinks = GetTPLinks();
                foreach(string t in tpLinks)
                {
                    Driver.GetInstance.WebDrive.Navigate().GoToUrl($"{baseUrl}{t}");
                    TP tp = new TP();

                    while (tp.LastMark < 100)
                    {
                        //take it
                        Methods.Click("[href*='/courses/5379/quizzes/19372/take?user_id=90628']", "HRef");

                        //load questions
                        LoadQuestions(tp);

                        //complete with resolved questions
                        foreach (Question q in tp.CurrentQuestions)
                        {
                            if (q.Resolved == true)
                            {
                                foreach (string ans in q.CorrectAnswers) Methods.Click(ans, "Id");
                                tp.CurrentExpectedMark += 5;
                            }
                        }
                        Console.WriteLine($"Expected mark: {tp.CurrentExpectedMark}");

                        //select an unresolved question
                        Question unresolvedQ = tp.CurrentQuestions.Where(q => q.Resolved == false).FirstOrDefault();

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
                        HtmlDocument doc = new HtmlDocument();
                        doc.LoadHtml(Driver.GetInstance.WebDrive.PageSource);

                        //save mark
                        /*foreach (var Nodo in doc.DocumentNode.CssSelect(".score_value"))
                        {
                            mark = Convert.ToDouble(Nodo.InnerHtml);
                        }*/
                        double mark = Convert.ToDouble(doc
                            .DocumentNode
                            .CssSelect(".score_value")
                            .FirstOrDefault()
                            .InnerHtml);
                        Console.WriteLine($"Mark: {mark}");

                        //verify answer
                        switch (mark - tp.CurrentExpectedMark)
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

        private static List<string> GetCourses()
        {
            HtmlDocument boardHtml = new HtmlDocument();
            boardHtml.LoadHtml(Driver.GetInstance.WebDrive.PageSource);
            List<string> Courses = new List<string>();
            foreach (var Nodo in boardHtml.DocumentNode.CssSelect(".ic-DashboardCard__link"))
            {
                HtmlAttributeCollection atts = Nodo.Attributes;
                Courses.Add(atts.Where(a => a.Name.ToLower() == "href").FirstOrDefault().Value);
            }
            return Courses;
        }

        private static List<string> GetTPLinks()
        {
            HtmlDocument boardHtml = new HtmlDocument();
            boardHtml.LoadHtml(Driver.GetInstance.WebDrive.PageSource);
            List<string> TPLinks = new List<string>();
            foreach (var Nodo in boardHtml.DocumentNode.CssSelect(".ig-title.title.item_link"))
            {
                HtmlAttributeCollection atts = Nodo.Attributes;
                string title = atts.Where(a => a.Name.ToLower() == "title").FirstOrDefault().Value;
                if (title.Contains("Trabajo"))
                {
                    TPLinks.Add(atts.Where(a => a.Name.ToLower() == "href").FirstOrDefault().Value);
                }
            }
            return TPLinks;
        }

        private static void LoadQuestions(TP tp)
        {
            //scrap TP page
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(Driver.GetInstance.WebDrive.PageSource);

            //initialize
            tp.CurrentQuestions = new List<Question>();
            List<Question> newQuestions = new List<Question>();

            //load each answer
            foreach (var Nodo in doc.DocumentNode.SelectNodes("//input[@id]"))
            {
                //load answer <input> attributes
                HtmlAttributeCollection atts = Nodo.Attributes;
                //gets answer id
                string answerId = atts.Where(a => a.Name.ToLower() == "id").FirstOrDefault().Value;
                //gets question id and keeps only the number
                Regex regex = new Regex(@"(?<=question_)(.+?)(?=_)");
                string idQuestionValue = regex.Match(answerId).Value;
                //checks if it was previously loaded
                if (!tp.Questions.Any(q => q.Id == idQuestionValue))
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
                    if(tp.CurrentQuestions.Where(q => q.Id == idQuestionValue).FirstOrDefault() == null)
                    {
                        Question question = tp.Questions.Where(q => q.Id == idQuestionValue).FirstOrDefault();
                        tp.CurrentQuestions.Add(question);
                    }
                }
            }
            foreach(Question q in newQuestions)
            {
                q.FullyLoaded = true;
                q.NoAttemptsAnswers = q.Answers;
                if (q.Answers.Count == 2) q.Type = "true_false_question";
                tp.CurrentQuestions.Add(q);
                tp.Questions.Add(q);
            }
        }
    }
}
