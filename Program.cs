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
                                foreach (Tuple<string, string> ans in q.CorrectAnswers) Methods.Click(ans.Item1, "Id");
                                tp.CurrentExpectedMark += 5;
                            }
                        }

                        //print the expected minimum mark
                        Console.WriteLine($"Expected mark: {tp.CurrentExpectedMark}");

                        //select an unresolved question
                        Question unresolvedQ = tp.CurrentQuestions.Where(q => q.Resolved == false).FirstOrDefault();

                        //select an answer
                        Tuple<string, string> answerSelected = unresolvedQ.Answers.FirstOrDefault();

                        if (unresolvedQ.Type == "matching_question")
                        {
                            Methods.SelectDropDown(answerSelected.Item1, "Id", answerSelected.Item2);
                        }
                        else Methods.Click(answerSelected.Item1, "Id");

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
                                unresolvedQ.Answers.Remove(answerSelected);
                                break;
                            case 5:
                                unresolvedQ.CorrectAnswers.Add(answerSelected);
                                unresolvedQ.Answers.Remove(answerSelected);
                                unresolvedQ.Resolved = true;
                                break;
                            default:
                                unresolvedQ.CorrectAnswers.Add(answerSelected);
                                if(unresolvedQ.Type == "matching_question")
                                {
                                    foreach(Tuple<string, string> ans in unresolvedQ.Answers.Where(a => a.Item1 == answerSelected.Item1))
                                    {
                                        unresolvedQ.Answers.Remove(ans);
                                    }
                                }
                                else unresolvedQ.Answers.Remove(answerSelected);
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

            //go for each answer
            foreach (var Node in doc.DocumentNode.SelectNodes("//input[@id]"))
            {
                //load answer <input> attributes
                HtmlAttributeCollection atts = Node.Attributes;
                //gets answer id
                string answerId = atts.Where(a => a.Name.ToLower() == "id").FirstOrDefault().Value;
                //gets question id and keeps only the number
                Regex regex = new Regex(@"(?<=question_)(.+?)(?=_)");
                string idQuestion = regex.Match(answerId).Value;
                //checks if it was previously loaded
                if (tp.Questions.Any(q => q.Id == idQuestion))
                {
                    tp.CurrentQuestions.Add(tp.Questions.Where(q => q.Id == idQuestion).FirstOrDefault());
                }
                else
                {
                    if (tp.CurrentQuestions.Any(q => q.Id == idQuestion))
                    {
                        tp.CurrentQuestions
                            .Where(q => q.Id == idQuestion)
                            .FirstOrDefault()
                            .Answers.Add(Tuple.Create(answerId, string.Empty));
                    }
                    else
                    {
                        //gets answer type, creates new question
                        string type;
                        string answerType = atts.Where(a => a.Name.ToLower() == "type").FirstOrDefault().Value;
                        if (answerType == "checkbox") type = "multiple_answers_question";
                        else type = "multiple_choice_question";
                        tp.CurrentQuestions.Add(new Question()
                        {
                            Id = idQuestion,
                            Type = type,
                            Answers = { Tuple.Create(answerId, string.Empty) }
                        });
                    }
                }
            }
            foreach (var Node in doc.DocumentNode.SelectNodes("//select[@id]"))
            {
                //load answer <input> attributes
                HtmlAttributeCollection atts = Node.Attributes;
                //gets answer id
                string answerId = atts.Where(a => a.Name.ToLower() == "id").FirstOrDefault().Value;
                //gets question id and keeps only the number
                Regex regex = new Regex(@"(?<=question_)(.+?)(?=_)");
                string idQuestion = regex.Match(answerId).Value;
                //checks if it was previously loaded
                if (tp.Questions.Any(q => q.Id == idQuestion))
                {
                    tp.CurrentQuestions.Add(tp.Questions.Where(q => q.Id == idQuestion).FirstOrDefault());
                }
                else
                {
                    if (tp.CurrentQuestions.Any(q => q.Id == idQuestion))
                    {
                        foreach (var SubNode in Node.Descendants("option"))
                        {
                            string option = SubNode.Attributes
                                .Where(a => a.Name.ToLower() == "value")
                                .FirstOrDefault().Value;
                            tp.CurrentQuestions
                                .Where(q => q.Id == idQuestion)
                                .FirstOrDefault()
                                .Answers.Add(Tuple.Create(answerId, option));
                        }
                    }
                    else
                    {
                        Question newQ = new Question()
                        {
                            Id = idQuestion,
                            Type = "matching_question"
                        };
                        foreach (var SubNode in Node.Descendants("option"))
                        {
                            string option = SubNode.Attributes
                                .Where(a => a.Name.ToLower() == "value")
                                .FirstOrDefault().Value;
                            newQ.Answers.Add(Tuple.Create(answerId, option));
                        }
                        tp.CurrentQuestions.Add(newQ);
                    }
                }
            }
            foreach (Question q in tp.CurrentQuestions)
            {
                if(!q.FullyLoaded) tp.Questions.Add(q);
                q.FullyLoaded = true;
            }
        }
    }
}
