using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotLib;
using BotLib.Extensions;

namespace DbEntity
{
    public class QuestionPattern
    {
        public RuleKeyword[] Keywords;
        public RuleKeyword[] Except;
        public int QuestionMaxLength;
        public const string QuestioLengthInit = "问题长度<";
        public const string ExceptInit = "排除:";

        public static string ConvertPatternToString(QuestionPattern value)
        {
            var sb = new StringBuilder();
            if (value != null)
            {
                sb.Append(RuleKeyword.ConvertPatternToString(value.Keywords));
                if (!value.Except.xIsNullOrEmpty())
                {
                    sb.Append(" | ");
                    sb.Append("排除:");
                    sb.Append(RuleKeyword.ConvertPatternToString(value.Except));
                }
                if (value.QuestionMaxLength > 0)
                {
                    sb.Append(" | ");
                    sb.Append("问题长度<");
                    sb.Append(value.QuestionMaxLength);
                }
            }
            return sb.ToString();
        }

        public static QuestionPattern ConvertStringToPattern(string txt)
        {
            QuestionPattern qp = null;
            try
            {
                var arrTxt = txt.Split('|');
                var arrRulwWord = RuleKeyword.ConvertStringToPattern(arrTxt[0]);
                qp = new QuestionPattern
                {
                    Keywords = RuleKeyword.ConvertStringToPattern(arrTxt[0]),
                    Except = QuestionPattern.GetExecptKeywords(arrTxt),
                    QuestionMaxLength = QuestionPattern.GetQuestionMaxLength(arrTxt)
                };
            }
            catch (Exception ex)
            {
                string msg = string.Format("ConvertStringToPattern出错，err={0},txt={1}", ex.Message, txt);
                Log.Error(msg);
                throw ex;
            }
            return qp;
        }

        private static int GetQuestionMaxLength(string[] arr)
        {
            int len = 0;
            if (arr.Length > 2)
            {
                var text = arr[2].Trim();
                if (!text.StartsWith("问题长度<"))
                {
                    throw new Exception("未知的QuestionMaxLength格式,str=" + text);
                }
                text = text.Substring("问题长度<".Length);
                try
                {
                    len = Convert.ToInt32(text);
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("转换QuestionMaxLength成数字时出错，str={0},err={1}", text, ex.Message));
                }
            }
            return len;
        }

        private static RuleKeyword[] GetExecptKeywords(string[] arr)
        {
            RuleKeyword[] execptKeywords = null;
            if (arr.Length > 1)
            {
                string text = arr[1].Trim();
                if (!text.StartsWith("排除:"))
                {
                    throw new Exception(string.Format("GetExecptKeywords,未知格式，txt=" + text, new object[0]));
                }
                text = text.Substring("排除:".Length);
                execptKeywords = RuleKeyword.ConvertStringToPattern(text);
            }
            return execptKeywords;
        }
    }
}
