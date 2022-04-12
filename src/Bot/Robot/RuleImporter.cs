using Bot.Common;
using Bot.Common.Db;
using Bot.Common.ImageHelper;
using Bot.Common.TreeviewHelper;
using Bot.Robot.Rule.QaCiteTable;
using BotLib;
using BotLib.Extensions;
using BotLib.Misc;
using DbEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Robot
{
    public class RuleImporter
    {
		public static int Import(string fn, string dbAccount)
		{
			var importNum = 0;
            if (!string.IsNullOrEmpty(fn))
            {
                DbSyner.Syn();
                var importItemDict = GetImportItem(fn);
                var importRuleDict = ConvertToImportRobotRuleEntity(importItemDict);
                var ruleDict = ConvertToRobotRuleEntity(importRuleDict, dbAccount);
				importNum = ruleDict.SelectMany(k=>k.Value).Count(); 
				if (importNum > 0)
                {
                    var dbAccessor = new TreeDbAccessor(typeof(RobotRuleCatalogEntity), typeof(RobotRuleEntity), dbAccount);
                    var importRootCat = CreateImportRootCata(dbAccessor);
                    foreach (var rd in ruleDict)
                    {
                        if (rd.Value.Count > 0)
                        {
                            CreateRule(rd.Key, rd.Value, importRootCat, dbAccessor);
                        }
                    }
                    DbSyner.Syn();
                }
            }
            return importNum;
		}

		private static void CreateRule(string catName, List<RobotRuleEntity> rules, RobotRuleCatalogEntity cat, TreeDbAccessor dbAccessor)
		{
			cat = CreateCata(dbAccessor, cat.EntityId, catName);
			CreateRule(rules, cat);
		}

		private static void CreateRule(List<RobotRuleEntity> rules, RobotRuleCatalogEntity cat)
		{
			if (rules.Count > 0)
			{
				rules[0].PrevId = null;
				rules[0].NextId = ((rules.Count > 1) ? rules[1].EntityId : null);
				rules[0].ParentId = cat.EntityId;
			}
			if (rules.Count > 1)
			{
				rules[rules.Count - 1].PrevId = rules[rules.Count - 2].EntityId;
				rules[rules.Count - 1].NextId = null;
				rules[rules.Count - 1].ParentId = cat.EntityId;
			}
			for (int i = 1; i < rules.Count - 1; i++)
			{
				var prevRule = rules[i - 1];
				var rule = rules[i];
				var nextRule = rules[i + 1];
				rule.PrevId = prevRule.EntityId;
				rule.NextId = nextRule.EntityId;
				rule.ParentId = cat.EntityId;
			}
			foreach (var rule in rules)
			{
				CiteTableManager.AddOrUpdateRule(rule);
			}
			DbHelper.BatchSaveOrUpdateToDb(rules.ToArray());
		}
		private static Dictionary<string, List<RobotRuleEntity>> ConvertToRobotRuleEntity(Dictionary<string, List<ImportRobotRuleEntity>> importRuleDict, string dbAccount)
		{
			var dict = new Dictionary<string, List<RobotRuleEntity>>();
			foreach (var rd in importRuleDict)
			{
				var rules = ConvertToRobotRuleEntity(rd.Value, dbAccount);
				if (rules.Count > 0)
				{
					dict[rd.Key] = rules;
				}
			}
			return dict;
		}

		private static List<RobotRuleEntity> ConvertToRobotRuleEntity(List<ImportRobotRuleEntity> importRules, string dbAccount)
		{
			var rules = new List<RobotRuleEntity>();
			foreach (var importRule in importRules)
			{
				var rule = importRule.CreateRule(dbAccount);
				if (!HasSameRobotRule(rule, dbAccount))
				{
					rules.Add(rule);
				}
			}
			return rules;
		}

		private static bool HasSameRobotRule(RobotRuleEntity r, string dbAccount)
		{
			bool rt = false;
			var sameIntentionRules = RobotRuleEntityEx.GetRobotRules(r.Intention, dbAccount);
			if (!sameIntentionRules.xIsNullOrEmpty())
			{
				foreach (var sr in sameIntentionRules)
				{
					if (ImportRobotRuleEntity.IsRobotRuleEqual(r, sr))
					{
						rt = true;
						break;
					}
				}
			}
			return rt;
		}

		private static RobotRuleCatalogEntity CreateImportRootCata(TreeDbAccessor dbAccessor)
		{
			return CreateCata(dbAccessor, dbAccessor.Root.EntityId, "导入的规则");
		}

		private static RobotRuleCatalogEntity CreateCata(TreeDbAccessor dbAccessor, string entityId, string catName)
		{
			var catEt = dbAccessor.ReadChildCatalogById(entityId).ConvertAll(k => (RobotRuleCatalogEntity)k).FirstOrDefault(k=>k.Name == catName);
			if (catEt == null)
			{
				catEt = EntityHelper.Create<RobotRuleCatalogEntity>(dbAccessor.DbAccount);
				catEt.Name = catName;
				dbAccessor.SaveToDb(catEt, entityId);
			}
			return catEt;
		}

		private static Dictionary<string, List<ImportRobotRuleEntity>> ConvertToImportRobotRuleEntity(Dictionary<string, List<ImportItem>> importItemDict)
		{
			var importRuleDict = new Dictionary<string, List<ImportRobotRuleEntity>>();
			foreach (var importItem in importItemDict)
			{
				importRuleDict[importItem.Key] = ConvertToImportRobotRuleEntity(importItem.Value);
			}
			return importRuleDict;
		}

		private static List<ImportRobotRuleEntity> ConvertToImportRobotRuleEntity(List<ImportItem> importItems)
		{
			var importItemDict = new Dictionary<string, List<ImportItem>>();
			foreach (var importItem in importItems)
			{
				if (!importItemDict.ContainsKey(importItem.Intension))
				{
					importItemDict[importItem.Intension] = new List<ImportItem>();
				}
				importItemDict[importItem.Intension].Add(importItem);
			}
			var importRules = new List<ImportRobotRuleEntity>();
			foreach (var importItem in importItemDict)
			{
				importRules.Add(new ImportRobotRuleEntity(importItem.Value));
			}
			return importRules;
		}

		private static Dictionary<string, List<ImportItem>> GetImportItem(string fn)
		{
			var lines = CsvFileHelper.ReadCsvFile(fn, -1);
			AssertHeaderOK(lines[0]);
			var dictionary = new Dictionary<string, List<ImportItem>>();
			for (int i = 1; i < lines.Count; i++)
			{
				try
				{
					List<string> list2 = lines[i];
					string key = list2[1];
					var item = new ImportItem(list2);
					if (!dictionary.ContainsKey(key))
					{
						dictionary[key] = new List<ImportItem>();
					}
					dictionary[key].Add(item);
				}
				catch (Exception ex)
				{
					var errmsg = string.Format("导入文件第【{0}】行出错：{1}", i + 1, ex.Message);
					MsgBox.ShowErrDialog(errmsg);
					throw new Exception(errmsg);
				}
			}
			return dictionary;
		}

		private static void AssertHeaderOK(List<string> headerCols)
		{
			Util.Assert(headerCols.Count > 8 && headerCols[0] == "意图" && headerCols[1] == "分组" && headerCols[2] == "匹配类型" 
				&& headerCols[3] == "问题" && headerCols[4] == "答案1" && headerCols[5] == "答案2" && headerCols[6] == "答案3" 
				&& headerCols[7] == "答案4" && headerCols[8] == "答案5");
		}

		private class ImportItem
		{
			public string Intension;
			public string Catalog;
			public string Question;
			public bool IsKeyMatch;
			public List<string> Answers;
			public ImportItem(List<string> linerule)
			{
				Intension = linerule[0].Trim();
				Util.Assert(!string.IsNullOrEmpty(Intension), "意图能不为空");
				Catalog = linerule[1].Trim();
				Util.Assert(!string.IsNullOrEmpty(Catalog), "分组不能留空");
				var keyMatch = linerule[2].Trim();
				if (keyMatch == "关键字匹配")
				{
					IsKeyMatch = true;
				}
				else if (keyMatch == "完全匹配")
				{
					IsKeyMatch = false;
				}
				else
				{
					Util.Assert(false, "匹配类型只能设置为，关键字匹配，或，完全匹配");
				}
				Question = linerule[3].Trim();
				Answers = new List<string>();
				for (int i = 4; i < linerule.Count; i++)
				{
					keyMatch = linerule[i].Trim();
					if (!string.IsNullOrEmpty(keyMatch))
					{
						Answers.Add(keyMatch);
					}
				}
				Util.Assert(Answers.Count > 0, "至少要设置一个问题");
			}
		}

		private class ImportRobotRuleEntity
		{
			public string Intention;
			public string Catalog;
			public HashSet<string> FullMatchQuestions;
			public HashSet<string> KeyPatterns;
			public HashSet<string> Answers;

			public ImportRobotRuleEntity(List<ImportItem> importItems)
			{
				FullMatchQuestions = new HashSet<string>();
				KeyPatterns = new HashSet<string>();
				Answers = new HashSet<string>();
				Util.Assert(importItems.Count > 0);
				Intention = importItems[0].Intension;
				Catalog = importItems[0].Catalog;
				if (importItems[0].IsKeyMatch)
				{
					KeyPatterns.Add(importItems[0].Question);
				}
				else
				{
					FullMatchQuestions.Add(importItems[0].Question);
				}
				Answers.xAddRange(importItems[0].Answers);
				for (int i = 1; i < importItems.Count; i++)
				{
					ImportItem importItem = importItems[i];
					Util.Assert(importItem.Intension == Intention && importItem.Catalog == Catalog);
					if (importItem.IsKeyMatch)
					{
						KeyPatterns.Add(importItem.Question);
					}
					else
					{
						FullMatchQuestions.Add(importItem.Question);
					}
					Answers.xAddRange(importItem.Answers);
				}
			}

			public static bool IsRobotRuleEqual(RobotRuleEntity r1, RobotRuleEntity r2)
			{
				if (r1.Intention != r2.Intention) return false;
				
				var ra = r1.GetAnswersV3();
				var rnum = (ra != null) ? ra.Count : 0;
				var sra = r2.GetAnswersV3();
				var srnum = (sra != null) ? sra.Count : 0;
				if (rnum != srnum) return false;
				
				var rpts = r1.GetPatternsV3();
				srnum = rpts != null ? rpts.Length : 0;
				var srpts = r2.GetPatternsV3();
				rnum = srpts != null ? srpts.Length : 0;
				if (srnum != rnum) return false;
				
				var rqs = r1.GetQuestions();
				rnum = rqs != null ? rqs.Count : 0;
				var srqs = r2.GetQuestions();
				srnum = srqs != null ? srqs.Count : 0;
				if (srnum != rnum) return false;

				if (!IsAnswersEqual(r1.GetAnswersV3(), r2.GetAnswersV3())) return false;
				if (!IsPatternsEqual(r1.GetPatternsV3(), r2.GetPatternsV3())) return false;
				if (!IsListEqual(r1.GetQuestions(), r2.GetQuestions())) return false;	
				
				return true;
			}  

			private static bool IsAnswersEqual(List<AnswerWithImage> awis1, List<AnswerWithImage> awis2)
			{
				var answers1 = awis1.Select(k => k.Answer).ToList();
				var answers2 = awis2.Select(k => k.Answer).ToList();
				if (!IsListEqual(answers1, answers2)) return false;

				var images1 = awis1.Select(k => k.ImageName).ToArray();
				var images2 = awis2.Select(k => k.ImageName).ToArray();
				return IsImageEqual(images1, images2);
			}

			private static bool IsImageEqual(string[] imagesPath1, string[] imagesPath2)
			{
				if (imagesPath1.Length == imagesPath2.Length) return false;
				var rt = true;				
				for (int i = 0; i < imagesPath1.Length; i++)
				{
					if (!RuleAnswerImageHelper.IsTwoImageEqual(imagesPath1[i], imagesPath2[i]))
					{
						rt = false;
						break;
					}
				}				
				return rt;
			}

			private static bool IsPatternsEqual(QuestionPattern[] patterns1, QuestionPattern[] patterns2)
			{
				var keywords1 = patterns1.Select(k=>k.Keywords).ToArray();
				var keywords2 = patterns2.Select(k =>k.Keywords).ToArray();
				if(!IsKeywordsEqual(keywords1, keywords2)) return false;

				var excepts1 = patterns1.Select(k => k.Except).ToArray();
				var excepts2 = patterns2.Select(k => k.Except).ToArray();
				if(!IsKeywordsEqual(excepts1, excepts2)) return false;

				var qml1 = patterns1.Select(k => k.QuestionMaxLength).ToArray();
				var qml2 = patterns2.Select(k => k.QuestionMaxLength).ToArray();
				return IsListEqual(qml1, qml2);
			}

			private static bool IsListEqual(int[] qmls1, int[] qmls2)
			{
				if (qmls1.Length != qmls2.Length) return false;

				var rt = true; ;
				for (int i = 0; i < qmls1.Length; i++)
				{
					if (qmls1[i] != qmls2[i])
					{
						rt = false;
						break;
					}
				}
				return rt;
			}

			private static bool IsListEqual(List<string> answers1, List<string> answers2)
			{
				if (answers1 != answers2) return false;
				if (answers1.xCount() != answers2.xCount()) return false;

				var rt = true;
				foreach (var ans in answers1.xSafeForEach())
				{
					if (answers2 != null && !answers2.Contains(ans))
					{
						rt = false;
						break;
					}
				}				
				return rt;
			}

			private static bool IsKeywordsEqual(RuleKeyword[][] keywordsArr1, RuleKeyword[][] keywordsArr2)
			{
				if (keywordsArr1 == null || keywordsArr2 == null) return false;
				if (keywordsArr1 != keywordsArr2) return false;
				if (keywordsArr1.Length != keywordsArr2.Length) return false;				
				var rt = true;
				var keywords1 = GetKeywords(keywordsArr1);
				var keywords2 = GetKeywords(keywordsArr2);
				foreach (var kwd in keywords1)
				{
					if (!keywords2.Contains(kwd))
					{
						rt = false;
						break;
					}
				}				
				return rt;
			}

			private static HashSet<string> GetKeywords(RuleKeyword[][] keywordsArr)
			{
				var keywords = new HashSet<string>();
				foreach (var rulekwds in keywordsArr)
				{
					keywords.Add(RuleKeyword.ConvertPatternToString(rulekwds));
				}
				return keywords;
			}

			public RobotRuleEntity CreateRule(string dbAccount)
			{
				var ruleEt = EntityHelper.Create<RobotRuleEntity>(dbAccount);
				ruleEt.Intention = Intention;
				ruleEt.SetAnswersV3(Answers.ToList().ConvertAll(ans => new AnswerWithImage
				{
					Answer = ans
				}));
				var questions = new List<string>();
				if (FullMatchQuestions != null)
				{ 
					questions = FullMatchQuestions.ToList();
				}
				ruleEt.SetQuestions(questions);
				ruleEt.SetPatternsV3(GetPatterns());
				return ruleEt;
			}

			private QuestionPattern[] GetPatterns()
			{
				var patterns = new List<QuestionPattern>();
				if (!KeyPatterns.xIsNullOrEmpty())
				{
					foreach (var s in KeyPatterns)
					{
						patterns.Add(new QuestionPattern
						{
							Keywords = RuleKeyword.ConvertStringToPattern(s)
						});
					}
				}
				return patterns.ToArray();
			}
		}
	}
}
