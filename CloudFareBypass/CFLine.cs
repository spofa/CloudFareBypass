using CloudFareBypass.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudFareBypass
{
    public enum CFDateType { String, Int };
    public enum Methods { One, ToString, PlusOne };
    public enum LineOperator { Multiply, Minus, Plus, Equals };

    public class CFLine
    {
        public CFDateType Type { get; private set; }
        public string Line { get; private set; }
        public LineOperator Operator { get; private set; }
        public List<CFLine> InternalLines { get; private set; }
        public int Value { get; private set; }

        private Dictionary<string, Methods> Keys = new Dictionary<string, Methods>
        {
            { "!+[]",  Methods.One },
            { "+!![]", Methods.PlusOne },
            { "+[]", Methods.ToString }
        };

        public CFLine(string line)
        {
            Operator = LineOperator.Equals;

            Line = line;
            InternalLines = new List<CFLine>();
            Type = CFDateType.String;
            Value = 0;

            CalculateOperation();
            ParseLine();
            CalculateValue();
        }

        private void CalculateOperation()
        {
            string[] lineParts = Line.Split('=');
            string operatorSide = lineParts[0];

            if (Line.StartsWith("+"))
            {
                Type = CFDateType.Int;
            }

            if (operatorSide.EndsWith("*"))
            {
                Operator = LineOperator.Multiply;
                Type = CFDateType.Int;
            }
            else if (operatorSide.EndsWith("-"))
            {
                Operator = LineOperator.Minus;
                Type = CFDateType.Int;
            }
            else if (operatorSide.EndsWith("+"))
            {
                Operator = LineOperator.Plus;
                Type = CFDateType.Int;
            }
        }

        private void ParseLine()
        {
            string regex = @"\((?<value>[\[\]\+\!]+)\)";

            List<string> internalLines = RegexScraper.ScrapeFromString<string>(Line, regex);

            foreach (string iLine in internalLines)
            {
                CFLine challenge = new CFLine(iLine);

                InternalLines.Add(challenge);
            }

            //Patch to fix lines without ( or ).
            if (internalLines.Count == 0)
            {
                string[] parts = Line.Split(':');

                if (parts.Length == 2)
                {
                    InternalLines.Add(new CFLine(parts[1]));
                }
                else
                {
                    parts = Line.Split('=');

                    if (parts.Length == 2)
                    {
                        InternalLines.Add(new CFLine(parts[1]));
                    }
                }
            }
        }

        private void CalculateValue()
        {
            if (InternalLines.Count > 0)
            {
                CFLine previousChallenge = null;

                foreach (CFLine challenge in InternalLines)
                {
                    if (challenge.Type == CFDateType.String || (previousChallenge != null && previousChallenge.Type == CFDateType.String))
                    {
                        string val = Value.ToString() + challenge.Value.ToString();

                        Value = Int32.Parse(val);
                    }
                    else if (challenge.Type == CFDateType.Int)
                    {
                        Value += challenge.Value;
                    }

                    previousChallenge = challenge;
                }
            }

            StringBuilder sBuilder = new StringBuilder();

            foreach (char c in Line)
            {
                sBuilder.Append(c);

                Methods method;

                if (Keys.TryGetValue(sBuilder.ToString(), out method))
                {
                    switch (method)
                    {
                        case Methods.One:
                            Value = 1;
                            break;
                        case Methods.PlusOne:
                            Value++;
                            break;
                        case Methods.ToString:
                            Type = CFDateType.String;
                            break;
                    }

                    sBuilder.Clear();
                }
            }
        }
    }
}
