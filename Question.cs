using System;
using System.Collections.Generic;
using System.Text;

namespace AutoTPs
{
    class Question
    {
        public string Number { get; set; }

        public string Type { get; set; }
        
        public List<string> Answers { get; set; }

        public List<string> CorrectAnswers { get; set; }

    }
}
