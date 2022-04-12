using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bot.AI.WordSplitterNs;
using BotLib;
using BotLib.Collection;
using BotLib.Extensions;
using DbEntity;

namespace Bot.Robot.Rule.QaCiteTable
{
    public class RuleCiteHelper
	{
		private CiteTable _citeTable;
		private ConcurrentHashSet<string> _ruleSet;

		public RuleCiteHelper(CiteTable citeTable)
		{
			_citeTable = citeTable;
			_ruleSet = new ConcurrentHashSet<string>();
		}

		public List<SmartAnswerMatchInfo> GetAnswers(string question)
		{
			var answers = new List<SmartAnswerMatchInfo>();
			answers.xAddRange(GetMatchSmartAnswer(question));
			answers.xAddRange(GetPatternMatchAllAnswer(question));
			var rules = new HashSet<RobotRuleEntity>();
			var mchAnswers = new List<SmartAnswerMatchInfo>();
			foreach (var ans in answers)
			{
				if (!rules.Contains(ans.Rule))
				{
					rules.Add(ans.Rule);
					mchAnswers.Add(ans);
				}
			}
			return mchAnswers;
		}

		private List<SmartAnswerMatchInfo> GetPatternMatchAllAnswer(string q)
		{
			var matchAnswers = new List<SmartAnswerMatchInfo>();
			var rules = RobotRuleEntityEx.GetRobotRules(_citeTable.DbAccount);
			if (rules == null || rules.Count < 1) return matchAnswers;
			var matchRules = rules.Where((r) =>
			{
				bool result = false;
				var pts = r.GetPatternsV3();
				foreach (var pt in pts.xSafeForEach())
				{
					if (result = pt.IsMatchRuleQuestionPattern(q, TbNickHelper.GetWwMainNickFromPubDbAccount(r.DbAccount)))
					{
						break;
					}
				}
				return result;
			}).ToList();
			foreach (var rule in matchRules.xSafeForEach())
			{
				try
				{
					matchAnswers.Add(new SmartAnswerMatchInfo
					{
						AnswerWithImg = rule.GetAnswersV3()[0],
						GoodsMatchScore = 1.0,
						QuestionMatchScore = 1.0,
						Rule = rule,
						IsRuleFullMatched = true
					});
				}
				catch (Exception e)
				{
					Log.Exception(e);
				}
			}
			return matchAnswers;
		}

		public void AddOrUpdateRule(RobotRuleEntity r)
		{
			try
			{
				if (r.HasContent())
				{
					if (_ruleSet.Contains(r.EntityId))
					{
						RemoveRule(r.EntityId);
					}
					_ruleSet.Add(r.EntityId);
					var questions = r.GetQuestions();
					foreach (string q in questions.xSafeForEach())
					{
						var wd = GetWordCiteData(q, true);
						wd.Add(r);
						AddSplitWordToWordCiteData(q, r);
					}
					AddKeywordToWordCiteData(r);
					var answers = r.GetAnswersV3();
					foreach (var awi in answers.xSafeForEach())
					{
						AddSplitWordToWordCiteData(awi.Answer, r);
					}
				}
			}
			catch (Exception e)
			{
				Log.Exception(e);
			}
		}

		public void RemoveRule(string entityId)
		{
			try
			{
				var wdkeys = new List<string>();
				foreach (var cite in _citeTable.CiteDict)
				{
					if (cite.Value.Rule != null && !cite.Value.Rule.Items.xIsNullOrEmpty())
					{
						var oldRules = cite.Value.Rule.Items.Where(k=>k.EntityId == entityId).ToList();
						cite.Value.Rule.Items.xRemove(oldRules);
						if (cite.Value.Rule.Items.Count == 0)
						{
							cite.Value.Rule = null;
							if (cite.Value.Prompt == null)
							{
								wdkeys.Add(cite.Key);
							}
						}
					}
				}
				foreach (var wdkey in wdkeys.xSafeForEach())
				{
					_citeTable.CiteDict.xTryRemove(wdkey);
				}
			}
			catch (Exception e)
			{
				Log.Exception(e);
			}
		}

		public List<SmartAnswerMatchInfo> GetMatchSmartAnswer(string q)
		{
			var smartAnswers = new List<SmartAnswerMatchInfo>();
			var wcd = GetWordCiteData(q, false);
			if (wcd != null)
			{
				var rules = wcd.Items.Where(k=> k.GetQuestions()!=null && k.GetQuestions().Contains(q)).Distinct();
				foreach (var r in rules.xSafeForEach())
				{
					smartAnswers.Add(new SmartAnswerMatchInfo
					{
						AnswerWithImg = r.xGetOneRandomAnswer(),
						GoodsMatchScore = 1.0,
						QuestionMatchScore = 1.0,
						Rule = r,
						IsQuestionFullMatched = true,
						IsRuleFullMatched = true
					});
				}
			}
			return smartAnswers;
		}

		private void AddKeywordToWordCiteData(RobotRuleEntity r)
		{
			var kws = GetRuleKeywords(r);
			foreach (var kw in kws.xSafeForEach())
			{
				var wcd = GetWordCiteData(kw, true);
				wcd.Add(r);
			}
		}

		private HashSet<string> GetRuleKeywords(RobotRuleEntity r)
		{
			var kws = new HashSet<string>();
			var pts = r.GetPatternsV3();
			foreach (var pt in pts.xSafeForEach())
			{
				foreach (var rk in pt.Keywords.xSafeForEach())
				{
					kws.xAddRange(rk.GetWords());
				}
			}
			return kws;
		}


		private void AddSplitWordToWordCiteData(string q, RobotRuleEntity r)
		{
			var wordDict = WordSpliter.Split(q, true);
			if (wordDict != null && wordDict.Count > 0)
			{
				foreach (var word in wordDict.Keys)
				{
					var wcd = GetWordCiteData(word, true);
					wcd.Add(r);
				}
			}
		}

		private RuleWordCiteData GetWordCiteData(string wdkey, bool createNew = false)
		{
			var wcd = _citeTable.TryGetWordCiteData(wdkey);
			if (wcd.Rule == null && createNew)
			{
				wcd.Rule = new RuleWordCiteData();
			}
			return (wcd != null) ? wcd.Rule : null;
		}
	}
}
