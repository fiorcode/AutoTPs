using System;
using System.Collections.Generic;
using System.Text;

namespace AutoTPs
{
    class TP
    {
        public TP()
        {
            Questions = new List<Question>();
            CurrentQuestions = new List<Question>();
            LastMark = 0;
            CurrentExpectedMark = 0;
        }

        public string Id { get; set; }

        public List<Question> Questions { get; set; }

        public List<Question> CurrentQuestions { get; set; }

        public double LastMark { get; set; }

        public double CurrentExpectedMark { get; set; }

    }
}
