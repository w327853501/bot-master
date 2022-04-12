using BotLib.Db.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotLib;
using BotLib.Extensions;
using Newtonsoft.Json;

namespace DbEntity
{
    public class RobotRuleEntity : TreeNode
    {
        private string _intention;
        private string _goodsNumiidsCsv;
        private HashSet<long> _goodsNumiids;
        private string _goodsCatIdsCsv;
        private HashSet<long> _goodsCatIds;
        private string _questionsJson;
        private List<string> _questions;
        private string _patternsJson;
        private RuleKeyword[][] _patterns;
        private string _answersJson;
        private List<string> _answers;
        private string _patternsJsonV3;
        private QuestionPattern[] _patternsV3;
        private string _answersJsonV3;
        private List<AnswerWithImage> _answersV3;
        private List<AnswerWithImage> _normAnswers;
        private int? _tfKey = null;

        public string Intention
        {
            get
            {
                return this._intention;
            }
            set
            {
                this.ClearTfKey();
                base.SetValue<string>(ref this._intention, value);
            }
        }

        public string GoodsNumiidsCsv
        {
            get
            {
                return this._goodsNumiidsCsv;
            }
            set
            {
                this._goodsNumiids = null;
                base.SetValue<string>(ref this._goodsNumiidsCsv, value);
            }
        }

        public HashSet<long> GetGoodsNumiids()
        {
            if (this._goodsNumiids == null)
            {
                this._goodsNumiids = this.ConvertCsvToLongSet(this.GoodsNumiidsCsv);
            }
            return this._goodsNumiids;
        }

        private HashSet<long> ConvertCsvToLongSet(string csv)
        {
            var set = new HashSet<long>();
            if (!string.IsNullOrEmpty(csv))
            {
                var source = csv.xSplitByComma(StringSplitOptions.RemoveEmptyEntries);
                foreach (var value in source.xSafeForEach())
                {
                    try
                    {
                        var iid = Convert.ToInt64(value);
                        set.Add(iid);
                    }
                    catch (Exception e)
                    {
                        Log.Exception(e);
                    }
                }
            }
            return set;
        }

        public string GoodsCatIdsCsv
        {
            get
            {
                return this._goodsCatIdsCsv;
            }
            set
            {
                this._goodsCatIds = null;
                base.SetValue<string>(ref this._goodsCatIdsCsv, value);
            }
        }

        public HashSet<long> GetGoodsCatIds()
        {
            if (this._goodsCatIds == null)
            {
                this._goodsCatIds = this.ConvertCsvToLongSet(this.GoodsCatIdsCsv);
            }
            return this._goodsCatIds;
        }

        public string QuestionsJson
        {
            get
            {
                return this._questionsJson;
            }
            set
            {
                this._questions = null;
                this.ClearTfKey();
                base.SetValue<string>(ref this._questionsJson, value);
            }
        }

        public List<string> GetQuestions()
        {
            if (string.IsNullOrEmpty(this.QuestionsJson)) return null;
            if (this._questions == null)
            {
                this._questions = this.RemoveEmptyItem(JsonConvert.DeserializeObject<string[]>(this.QuestionsJson));
                if (_questions != null)
                {
                    _questions = (from q in _questions
                                  select q.xToBanJiaoAndRemoveEndingPunctuationsSpaces()).ToList();
                }
            }
            return _questions;
        }

        public static string NormalizeQuestionString(string q)
        {
            return NormalizeString(q);
        }

        public void SetQuestions(List<string> slist)
        {
            slist = NormalizeStringList(slist);
            this.QuestionsJson = JsonConvert.SerializeObject(slist.ToArray());
        }

        public string PatternsJson
        {
            get
            {
                return this._patternsJson;
            }
            set
            {
                this._patterns = null;
                this.ClearTfKey();
                base.SetValue<string>(ref this._patternsJson, value);
            }
        }

        public RuleKeyword[][] GetPatterns()
        {
            if (string.IsNullOrEmpty(this.PatternsJson)) return null;
            if (this._patterns == null)
            {
                this._patterns = JsonConvert.DeserializeObject<RuleKeyword[][]>(this.PatternsJson);
            }
            return _patterns;
        }

        public void SetPatterns(RuleKeyword[][] slist)
        {
            this.PatternsJson = JsonConvert.SerializeObject(slist);
        }

        public string AnswersJson
        {
            get
            {
                return this._answersJson;
            }
            set
            {
                this._answers = null;
                this.ClearTfKey();
                base.SetValue<string>(ref this._answersJson, value);
            }
        }

        public List<string> GetAnswers()
        {
            if (string.IsNullOrEmpty(this.AnswersJson)) return null;
            if (this._answers == null)
            {
                this._answers = this.RemoveEmptyItem(JsonConvert.DeserializeObject<string[]>(this.AnswersJson));
            }
            return _answers;
        }

        public string PatternsJsonV3
        {
            get
            {
                return this._patternsJsonV3;
            }
            set
            {
                this._patternsV3 = null;
                this.ClearTfKey();
                base.SetValue<string>(ref this._patternsJsonV3, value);
            }
        }

        public QuestionPattern[] GetPatternsV3()
        {
            if (string.IsNullOrEmpty(this.PatternsJsonV3)) return null;
            if (_patternsV3 == null)
            {
                this._patternsV3 = JsonConvert.DeserializeObject<QuestionPattern[]>(this.PatternsJsonV3);
            }
            return _patternsV3;
        }

        public void SetPatternsV3(QuestionPattern[] slist)
        {
            this.PatternsJsonV3 = JsonConvert.SerializeObject(slist);
        }

        public string AnswersJsonV3
        {
            get
            {
                return this._answersJsonV3;
            }
            set
            {
                this._answersV3 = null;
                this._normAnswers = null;
                this.ClearTfKey();
                base.SetValue<string>(ref this._answersJsonV3, value);
            }
        }

        public List<AnswerWithImage> GetAnswersV3()
        {
            if (string.IsNullOrEmpty(this.AnswersJsonV3)) return null;
            if (_answersV3 == null)
            {
                this._answersV3 = this.RemoveEmptyItem(JsonConvert.DeserializeObject<AnswerWithImage[]>(this.AnswersJsonV3));
            }
            return _answersV3;
        }

        public List<AnswerWithImage> GetAnswerV3WithNormalize()
        {
            throw new NotImplementedException();
        }

        public void SetAnswersV3(List<AnswerWithImage> slist)
        {
            this.AnswersJsonV3 = JsonConvert.SerializeObject((slist != null) ? slist.ToArray() : null);
        }

        private List<AnswerWithImage> NormalizeStringList(List<AnswerWithImage> slist)
        {
            if (slist == null) return slist;
            if (slist != null)
            {
                slist.ForEach(x =>
                {
                    x.Answer = RobotRuleEntity.NormalizeString(x.Answer);
                });
                slist = (from y in slist
                         where !string.IsNullOrEmpty(y.Answer)
                         select y).ToList();
            }
            return slist;
        }

        private List<AnswerWithImage> RemoveEmptyItem(AnswerWithImage[] answerWithImage)
        {
            if (answerWithImage == null) return null;
            return (from x in answerWithImage
                    where !string.IsNullOrEmpty(x.Answer) || !string.IsNullOrEmpty(x.ImageName)
                    select x).ToList();
        }

        private List<string> RemoveEmptyItem(string[] slist)
        {
            if (slist == null || slist.Length == 0) return null;
            var lst = new List<string>();
            foreach (string txt in slist)
            {
                if (!txt.xIsNullOrEmptyOrSpace())
                {
                    lst.Add(txt);
                }
            }
            if (lst.Count == 0)
            {
                lst = null;
            }
            return lst;
        }

        public static List<string> NormalizeStringList(List<string> slist)
        {
            slist = (from x in slist
                     select RobotRuleEntity.NormalizeString(x)).ToList<string>();
            slist = (from x in slist
                     where !string.IsNullOrEmpty(x)
                     select x).ToList();
            return slist;
        }

        public static string NormalizeString(string txt)
        {
            return txt.xToBanJiaoAndToLowerAndSymplifiedAndTrim();
        }

        public int GetTfKey()
        {
            if (this._tfKey == null)
            {
                var tfKey = new StringBuilder();
                tfKey.Append(this.Intention);
                tfKey.Append("!@#");
                if (!string.IsNullOrEmpty(this.QuestionsJson))
                {
                    tfKey.Append(this.QuestionsJson);
                }
                tfKey.Append("!@#");
                if (!string.IsNullOrEmpty(this.AnswersJsonV3))
                {
                    tfKey.Append(this.AnswersJsonV3);
                }
                tfKey.Append("!@#");
                if (!string.IsNullOrEmpty(this.PatternsJsonV3))
                {
                    tfKey.Append(this.PatternsJsonV3);
                }
                this._tfKey = new int?(tfKey.ToString().GetHashCode());
            }
            return this._tfKey.Value;
        }

        private void ClearTfKey()
        {
            this._tfKey = null;
        }

    }
}
