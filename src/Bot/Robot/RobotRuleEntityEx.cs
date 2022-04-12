using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Linq;
using System.Threading;
using Bot.Common;
using Bot.Common.Account;
using Bot.Common.TreeviewHelper;
using DbEntity;
using BotLib;
using BotLib.Extensions;

namespace Bot.Robot
{
    public static class RobotRuleEntityEx
	{
		private static EventHandler<RobotRuleChangedEventArgs> EvRuleChanged;
		private const string RulePathSeperator = ">";
		private static Dictionary<string, TreeDbAccessor> _accDict;

		static RobotRuleEntityEx()
		{
			_accDict = new Dictionary<string, TreeDbAccessor>();
			DbHelper.EvSaveRecord += DbHelper_EvSaveRecord;
		}

		public static AnswerWithImage xGetOneRandomAnswer(this RobotRuleEntity r)
		{
			AnswerWithImage awi = null;
			if (r != null)
			{
				var answers = r.GetAnswersV3();
				Util.Assert(answers.Count > 0);
				int index = 0;
				if (answers.Count > 1)
				{
					index = RandomEx.Rand.Next(answers.Count);
				}
				awi = answers[index];
				Util.Assert(awi != null);
			}
			return awi;
		}

		public static bool HasContent(this RobotRuleEntity ent)
		{
			return !string.IsNullOrEmpty(ent.Intention) && !string.IsNullOrEmpty(ent.AnswersJsonV3) && ent.AnswersJsonV3.ToLower() != "null" && (!string.IsNullOrEmpty(ent.QuestionsJson) || (!string.IsNullOrEmpty(ent.PatternsJsonV3) && ent.PatternsJsonV3.ToLower() != "null"));
		}

		public static void Append(this RobotRuleEntity ent, string parentId)
		{
			GetDbAccessor(ent).AppendNode(ent, parentId);
		}

		public static void Delete(this RobotRuleEntity ent)
		{
            GetDbAccessor(ent).DeleteNode(ent);
		}

		public static void Save(this RobotRuleEntity ent)
		{
            GetDbAccessor(ent).SaveNode(ent);
		}

		public static RobotRuleEntity FindOne(string id, string dbAccount = null)
		{
			return DbHelper.FirstOrDefault<RobotRuleEntity>(id, dbAccount);
		}

		public static RobotRuleEntity GetRobotRule(string meaning, string dbAccount)
		{
			return DbHelper.FirstOrDefault<RobotRuleEntity>(dbAccount, rule => rule.Intention == meaning);
		}

		public static List<RobotRuleEntity> GetRobotRules(string meaning, string dbAccount)
		{
			return DbHelper.Fetch<RobotRuleEntity>(dbAccount, rule => rule.Intention == meaning);
		}

		public static ConcurrentBag<RobotRuleEntity> GetRobotRules(string dbAccount)
		{
			return AccountRuleCache.GetCache(dbAccount);
		}

		public static RobotRuleEntity GetRobotRuleById(string id, string seller)
		{
			var dbAccount = AccountHelper.GetPubDbAccount(seller);
			var r = FindOne(id, dbAccount);
			if (r != null) return r;
			dbAccount = AccountHelper.GetPrvDbAccount(seller);
			r = FindOne(id, dbAccount);
			return r;
		}

		private static TreeDbAccessor GetDbAccessor(string dbAccount)
		{
			return _accDict.xTryGetValueAndCreateIfNotExist(dbAccount, dbAcc =>{
                return new TreeDbAccessor(typeof(RobotRuleCatalogEntity), typeof(RobotRuleEntity),dbAcc);
            });
		}

		private static TreeDbAccessor GetDbAccessor(RobotRuleEntity r)
		{
			return GetDbAccessor(r.DbAccount);
		}

		private static void DbHelper_EvSaveRecord(object sender, DbRecordChangedEventArgs e)
		{
			if (e.Entity is RobotRuleEntity)
			{
				var r = e.Entity as RobotRuleEntity;
				if (EvRuleChanged != null)
				{
					EvRuleChanged(sender, new RobotRuleChangedEventArgs(e));
				}
			}
		}

		public static TestResult GetMatchResult(this RobotRuleEntity r, string q)
		{
			q = RobotRuleEntity.NormalizeQuestionString(q);
			var isMatch = IsMatchQuestion(r, q);
			var qpts = GetRuleQuestionPattern(r, q);
			return new TestResult
			{
				IsMatchRuleQuestion = isMatch,
				PatternMatchDescs = ToPatternMatchDescs(qpts)
			};
		}

		private static List<string> ToPatternMatchDescs(List<QuestionPattern> pts)
		{
			var templates = new List<string>();
			for (int i = 0; i < pts.Count; i++)
			{
				var item = string.Format("模板{0}:{1}", i + 1, QuestionPattern.ConvertPatternToString(pts[i]));
				templates.Add(item);
			}
			return templates;
		}

		private static List<QuestionPattern> GetRuleQuestionPattern(RobotRuleEntity r, string question)
		{
			var qps = new List<QuestionPattern>();
			var pts = r.GetPatternsV3();
			foreach (var pt in pts.xSafeForEach())
			{
				if (pt.IsMatchRuleQuestionPattern(question, TbNickHelper.GetWwMainNickFromPubDbAccount(r.DbAccount)))
				{
					qps.Add(pt);
				}
			}
			return qps;
		}

		public static bool IsMatchRuleQuestionPattern(this QuestionPattern pattern, string question, string dbAccount)
		{
			return IsEffectiveQuestion(question, pattern) && IsMatchRuleKeywords(question, pattern.Keywords) && (!Params.Robot.GetRuleIncludeExcept(dbAccount) || !IsMatchRuleKeywords(question, pattern.Except));
		}

		private static bool IsMatchRuleKeywords(string question, RuleKeyword[] keywords)
		{
			bool rt = false;
			if (keywords.xIsNullOrEmpty()) return rt;
			{
				foreach (var rk in keywords)
				{
					if (!(rt = IsMatch(question, rk)))
					{
						break;
					}
				}
			}
			return rt;
		}

		private static bool IsEffectiveQuestion(string question, QuestionPattern pattern)
		{
			return pattern.QuestionMaxLength <= 0 || (pattern.QuestionMaxLength > 0 && question.Length <= pattern.QuestionMaxLength);
		}

		private static bool IsMatch(string q, RuleKeyword rk)
		{
			try
			{
				var words = rk.GetWords();
				foreach (string value in words)
				{
					if (q.Contains(value))
					{
						return true;
					}
				}
			}
			catch (Exception e)
			{
				Log.Exception(e);
			}
			return false;
		}

		private static bool IsMatchQuestion(RobotRuleEntity r, string question)
		{
			var questions = r.GetQuestions();
			foreach (var q in questions.xSafeForEach())
			{
				if (q == question)
				{
					return true;
				}
			}
			return false;
		}

		private static class AccountRuleCache
		{
			private static ConcurrentDictionary<string, ConcurrentBag<RobotRuleEntity>> _cache;
			static AccountRuleCache()
			{
				_cache = new ConcurrentDictionary<string, ConcurrentBag<RobotRuleEntity>>();
				EvRuleChanged += RobotRuleEntityV2Ex_EvRuleChanged;
			}

			public static ConcurrentBag<RobotRuleEntity> GetCache(string dbAccount)
			{
				if (!_cache.ContainsKey(dbAccount))
				{
					var dbAccessor = new TreeDbAccessor(typeof(RobotRuleCatalogEntity), typeof(RobotRuleEntity), dbAccount);
					var collection = dbAccessor.ReadNodeInTheTree().ConvertAll(k => k as RobotRuleEntity);
					collection = collection.Where(k => k != null).ToList();
					_cache[dbAccount] = new ConcurrentBag<RobotRuleEntity>(collection);
				}
				return _cache[dbAccount];
			}

			private static void RobotRuleEntityV2Ex_EvRuleChanged(object sender, RobotRuleChangedEventArgs e)
			{
                var rre = e.Entity as RobotRuleEntity;
				if (_cache.ContainsKey(rre.DbAccount))
				{
					var bag = GetCache(rre.DbAccount);
					bag.xReplaceMatch(rre, ent => ent.EntityId == rre.EntityId , true);
				}
			}

		}

		public class TestResult
		{
			public bool IsMatchRuleQuestion;
			public List<string> PatternMatchDescs;
		}
    }
}
