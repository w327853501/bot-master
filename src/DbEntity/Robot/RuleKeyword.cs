using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotLib;
using BotLib.Extensions;

namespace DbEntity
{
    public class RuleKeyword
    {
        private string _keyword;
        private string _synonymCsv;
        private HashSet<string> _words;
        public string Keyword
        {
            get
            {
                return this._keyword;
            }
            set
            {
                this._keyword = value;
                this._words = null;
            }
        }

        public string SynonymCsv
        {
            get
            {
                return this._synonymCsv;
            }
            set
            {
                this._synonymCsv = value;
                this._words = null;
            }
        }

        public HashSet<string> GetWords()
        {
            if (this._words == null)
            {
                this._words = new HashSet<string>();
                this._words.Add(this.Keyword);
                if (!string.IsNullOrEmpty(this.SynonymCsv))
                {
                    this._words.xAddRange(this.SynonymCsv.xSplitByComma(StringSplitOptions.RemoveEmptyEntries));
                }
                this._words.RemoveWhere(x => x.xIsNullOrEmptyOrSpace());
            }
            return this._words;
        }

        public static string ConvertPatternToString(IEnumerable<RuleKeyword> keywords)
        {
            var rt = string.Empty;
            if ( !keywords.xIsNullOrEmpty())
            {
                var sb = new StringBuilder();
                var theFirstOne = true;
                foreach (var k in keywords)
                {
                    if (!theFirstOne)
                    {
                        sb.Append(" + ");
                    }
                    theFirstOne = false;
                    RuleKeyword.AppendKeyword(sb, k);
                }
                rt = sb.ToString();
            }
            return rt;
        }

        private static void AppendKeyword(StringBuilder sb, RuleKeyword k)
        {
            Util.Assert(!k.Keyword.xIsNullOrEmptyOrSpace());
            sb.Append(k.Keyword);
            if (!k.SynonymCsv.xIsNullOrEmptyOrSpace())
            {
                sb.Append("(");
                sb.Append(k.SynonymCsv);
                sb.Append(")");
            }
        }

        public static RuleKeyword[] ConvertStringToPattern(string s)
        {
            s = s.xToBanJiaoAndToLowerAndSymplifiedAndTrim();
           var arrTxt = s.Split('+');
            var ruleWords = new List<RuleKeyword>();
            foreach (var txt in arrTxt)
            {
                if (!string.IsNullOrEmpty(txt.Trim()))
                {
                    ruleWords.Add(RuleKeyword.ConvertStringToRuleKeyword(txt.Trim()));
                }
            }
            return ruleWords.ToArray();
        }

        private static RuleKeyword ConvertStringToRuleKeyword(string rk)
        {
            RuleKeyword rt;
            if (rk.EndsWith(")"))
            {
                int idx = rk.IndexOf("(");
                Util.Assert(idx > 0);
                string keyword = rk.Substring(0, idx);
                string synonymCsv = rk.Substring(idx + 1, rk.Length - idx - 2);
                rt = new RuleKeyword
                {
                    Keyword = keyword,
                    SynonymCsv = synonymCsv
                };
            }
            else
            {
                rt = new RuleKeyword
                {
                    Keyword = rk
                };
            }
            return rt;
        }
    }
}
