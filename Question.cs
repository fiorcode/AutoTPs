using System;
using System.Collections.Generic;
using System.Text;

namespace AutoTPs
{
    class Question
    {
        public Question()
        {
            FullyLoaded = false;
            Resolved = false;
            Answers = new List<string>();
            WrongAnswers = new List<string>();
            CorrectAnswers = new List<string>();
        }

        public string Id { get; set; }

        public string Type { get; set; }

        public bool FullyLoaded { get; set; }

        public bool Resolved { get; set; }
        
        public List<string> Answers { get; set; }

        public List<string> WrongAnswers { get; set; }

        public List<string> CorrectAnswers { get; set; }

    }
}
