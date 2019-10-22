﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
                        //clear current questions list and initialize expected mark
                        tp.CurrentQuestions.Clear();
                        tp.CurrentExpectedMark = 0;

                        //take it
                        Methods.Click("take_quiz_link", "Id");

                        //load questions
                        LoadQuestions(tp);

                        //complete with resolved questions
                        foreach (Question q in tp.CurrentQuestions)
                        {
                            if (q.Resolved == true)
                            {
                                if (q.Type == "matching_question")
                                {
                                    foreach (Tuple<string, string> ans in q.CorrectAnswers)
                                    {
                                        Methods.SelectDropDown(ans.Item1, "Id", ans.Item2);
                                    }
                                }
                                else foreach (Tuple<string, string> ans in q.CorrectAnswers) Methods.Click(ans.Item1, "Id");
                                tp.CurrentExpectedMark += 5;
                            }
                        }

                        //print the expected minimum mark
                        Console.WriteLine($"Minimum expected mark: {tp.CurrentExpectedMark}");

                        //select an unresolved question
                        Question unresolvedQ = tp.CurrentQuestions.Where(q => q.Resolved == false).FirstOrDefault();

                        //select an answer
                        Tuple<string, string> answerSelected = new Tuple<string, string>("","");
                        if (unresolvedQ.Answers.Count == 0)
                        {
                            if(unresolvedQ.Type == "multiple_answers_question")
                            {
                                foreach(Tuple<string, string> a in unresolvedQ.CorrectAnswers) Methods.Click(a.Item1, "Id");
                            }
                            else
                            {
                                foreach (Tuple<string, string> a in unresolvedQ.CorrectAnswers)
                                {
                                    Methods.SelectDropDown(a.Item1, "Id", a.Item2);
                                }
                            }
                        }
                        else
                        {
                            answerSelected = unresolvedQ.Answers.FirstOrDefault();
                            if (unresolvedQ.Type == "multiple_choice_question") Methods.Click(answerSelected.Item1, "Id");
                            else
                            {
                                if (unresolvedQ.Type == "matching_question")
                                {
                                    Methods.SelectDropDown(answerSelected.Item1, "Id", answerSelected.Item2);
                                }
                                else Methods.Click(answerSelected.Item1, "Id");
                            }
                        }

                        //submit
                        Methods.Click("submit_quiz_button", "Id");

                        //check if exist an alert of incomplete answers and accept it
                        if(tp.CurrentExpectedMark < 95) Driver.GetInstance.WebDrive.SwitchTo().Alert().Accept();
                        //if (isAlertPresent()) Driver.GetInstance.WebDrive.SwitchTo().Alert().Accept();

                        //scarp results page
                        HtmlDocument doc = new HtmlDocument();
                        doc.LoadHtml(Driver.GetInstance.WebDrive.PageSource);

                        //save mark
                        tp.LastMark = Convert.ToDouble(doc
                            .DocumentNode
                            .CssSelect(".score_value")
                            .FirstOrDefault()
                            .InnerHtml);
                        Console.WriteLine($"Mark: {tp.LastMark}");

                        //verify answer
                        if (answerSelected != null)
                        {
                            switch (tp.LastMark - tp.CurrentExpectedMark)
                            {
                                case 0:
                                    unresolvedQ.Answers.Remove(answerSelected);
                                    if(unresolvedQ.Answers.Count == 1)
                                    {
                                        unresolvedQ.CorrectAnswers.Add(unresolvedQ.Answers.FirstOrDefault());
                                        unresolvedQ.Resolved = true;
                                    }
                                    break;
                                case 5:
                                    unresolvedQ.CorrectAnswers.Add(answerSelected);
                                    unresolvedQ.Answers.Remove(answerSelected);
                                    unresolvedQ.Resolved = true;
                                    break;
                                default:
                                    unresolvedQ.CorrectAnswers.Add(answerSelected);
                                    if (unresolvedQ.Type == "matching_question")
                                    {
                                        List<Tuple<string, string>> toRemove = new List<Tuple<string, string>>();
                                        foreach (Tuple<string, string> ans in unresolvedQ.Answers.Where(
                                            a => a.Item1 == answerSelected.Item1 ||
                                            a.Item2 == answerSelected.Item2))
                                        {
                                            toRemove.Add(ans);
                                        }
                                        foreach (Tuple<string, string> tuple in toRemove)
                                        {
                                            unresolvedQ.Answers.Remove(tuple);
                                        }
                                    }
                                    else unresolvedQ.Answers.Remove(answerSelected);
                                    break;
                            }
                        }
                        else
                        {
                            unresolvedQ.Resolved = true;
                        }
                        Console.WriteLine($"Total of Q / Resolved: {tp.Questions.Count()}/{tp.Questions.Where(q => q.Resolved == true).Count()}");
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
            //go for each select answer
            HtmlNodeCollection selectNodes = doc.DocumentNode.SelectNodes("//select[@id]");
            if(selectNodes != null)
            {
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
                        if (!tp.CurrentQuestions.Any(q => q.Id == idQuestion))
                        {
                            tp.CurrentQuestions.Add(tp.Questions.Where(q => q.Id == idQuestion).FirstOrDefault());
                        }
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
                                if (!string.IsNullOrEmpty(option))
                                {
                                    tp.CurrentQuestions
                                    .Where(q => q.Id == idQuestion)
                                    .FirstOrDefault()
                                    .Answers.Add(Tuple.Create(answerId, option));
                                }
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
                                if (!string.IsNullOrEmpty(option))
                                {
                                    newQ.Answers.Add(Tuple.Create(answerId, option));
                                }
                            }
                            tp.CurrentQuestions.Add(newQ);
                        }
                    }
                }
            }
            //go for each input answer
            HtmlNodeCollection inputNodes = doc.DocumentNode.SelectNodes("//input[@id]");
            if(inputNodes != null)
            {
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
                        if (!tp.CurrentQuestions.Any(q => q.Id == idQuestion))
                        {
                            tp.CurrentQuestions.Add(tp.Questions.Where(q => q.Id == idQuestion).FirstOrDefault());
                        }
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
            }
            foreach (Question q in tp.CurrentQuestions)
            {
                if(!q.FullyLoaded) tp.Questions.Add(q);
                q.FullyLoaded = true;
            }
        }

        /*private static bool isAlertPresent()
        {
            try
            {
                Driver.GetInstance.WebDrive.SwitchTo().Alert();
                return true;
            }   // try 
            catch (NoAlertPresentException Ex)
            {
                return false;
            }   // catch 
        }   // isAlertPresent()*/
    }
}
