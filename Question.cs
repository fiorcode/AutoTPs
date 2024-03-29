﻿using System;
using System.Collections.Generic;

namespace AutoTPs
{
    class Question
    {
        public Question()
        {
            FullyLoaded = false;
            Resolved = false;
            Printed = false;
            Answers = new List<Tuple<string, string>>();
            CorrectAnswers = new List<Tuple<string, string>>();
        }

        public string Id { get; set; }

        public string Type { get; set; }

        public bool FullyLoaded { get; set; }

        public bool Printed { get; set; }

        public bool Resolved { get; set; }
        
        public List<Tuple<string, string>> Answers { get; set; }

        public List<Tuple<string, string>> CorrectAnswers { get; set; }

    }
}
