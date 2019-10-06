using System;
using System.Collections.Generic;
using System.Text;

namespace AutoTPs
{
    class Question
    {
        public string Id { get; set; }

        public bool Resolved { get; set; }
        
        public List<string> Answers { get; set; }

        public List<string> CorrectAnswers { get; set; }

    }
}
