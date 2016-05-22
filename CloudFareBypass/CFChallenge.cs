using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CloudFareBypass
{
    public class CFChallenge
    {
        private string _script = String.Empty;
        private Uri _url;
        private List<CFLine> _lines = new List<CFLine>();

        public CFChallenge(string script, string url)
        {
            _script = script;
            _url = new Uri(url);

            ParseValidLines();
        }

        public CFChallenge(string script, Uri url)
        {
            _script = script;
            _url = url;

            ParseValidLines();
        }

        private void ParseValidLines()
        {
            string[] pieces = _script.Split(';');
            string regexString = @"(=|\*|-)?[:=][+!]";

            Regex regex = new Regex(regexString);

            foreach (string piece in pieces)
            {
                if (regex.IsMatch(piece))
                {
                    _lines.Add(new CFLine(piece));
                }
            }
        }

        public int SolveChallenge()
        {
            int finalValue = 0;

            foreach(CFLine line in _lines)
            {
                if(line.Operator == LineOperator.Equals)
                {
                    finalValue = line.Value;
                }
                else if (line.Operator == LineOperator.Minus)
                {
                    finalValue -= line.Value;
                }
                else if (line.Operator == LineOperator.Multiply)
                {
                    finalValue *= line.Value;
                }
                else
                {
                    finalValue += line.Value;
                }
            }

            //Increase by url size
            int urlLength = _url.Host.Length;

            finalValue += urlLength;

            return finalValue;
        }
    }
}
