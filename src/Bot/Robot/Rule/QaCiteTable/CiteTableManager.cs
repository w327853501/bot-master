using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BotLib;
using BotLib.Extensions;
using Bot.Common.Account;
using Bot.AssistWindow.Widget.Bottom;
using DbEntity;
using BotLib.Collection;

namespace Bot.Robot.Rule.QaCiteTable
{
	public class CiteTableManager
	{
        private static ConcurrentDictionary<string, CiteTable> _dict;
        private static object _syn4CreateTable;

		static CiteTableManager()
		{
			_dict = new ConcurrentDictionary<string, CiteTable>();
			_syn4CreateTable = new object();
		}

        public static List<CtlAnswer.Item4Show> GetInputSugestion(string input, string dbAccount, Dictionary<long, double> contextNumiid = null, int count = 5)
		{
            var sugs = new List<CtlAnswer.Item4Show>();
			try
			{
                sugs = GetCiteTable(dbAccount).GetInputSugestion(input, contextNumiid, count);
			}
			catch (Exception e)
			{
				Log.Exception(e);
			}
            return sugs;
		}

        public static List<SmartAnswerMatchInfo> GetAnswers(string question, string dbAccount)
        {
            List<SmartAnswerMatchInfo> answers = null;
            try
            {
                answers = GetCiteTable(dbAccount).GetAnswers(question);
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
            return answers;
        }

        public static List<SmartAnswerMatchInfo> GetTestModeAnswers(string question, string dbAccount)
        {
            List<SmartAnswerMatchInfo> answers = null;
            try
            {
                answers = GetCiteTable(dbAccount).GetAnswers(question);
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
            return answers;
        }

        public static HashSet<RobotRuleEntity> GetMatchedRule(string question, string dbAccount)
        {
            HashSet<RobotRuleEntity> hashSet = null;
            try
            {
                question = RobotRuleEntity.NormalizeQuestionString(question);
                hashSet = GetCiteTable(dbAccount).GetRules(question);
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
            return hashSet;
        }

        public static void AddOrUpdateShortcutToInputPromptWordCite(ShortcutEntity shortcut)
        {
            var ctv = GetCiteTable(shortcut.DbAccount);
            if (ctv != null)
            {
                ctv.AddOrUpdateInputPromptWordCite(shortcut);
            }
        }

        private static CiteTable GetCiteTable(string dbAccount)
		{
			var citeTb = _dict.xTryGetValue(dbAccount, null);
			if (citeTb == null)
			{
                lock (_syn4CreateTable)
				{
					citeTb = _dict.xTryGetValue(dbAccount, null);
					if (citeTb == null)
					{
						citeTb = new CiteTable(dbAccount);
						_dict[dbAccount] = citeTb;
					}
				}
			}
			return citeTb;
		}

        public static void InitCiteTables(string dbAccount)
		{
            var citeTb = GetCiteTable(dbAccount);
			citeTb.ReadFromDb(false);
		}

        public static void ReInitCiteTables()
        {
            try
            {
                var dict = _dict;
                _dict = new ConcurrentDictionary<string, CiteTable>();
                foreach (string dbAccount in dict.Keys)
                {
                    var citeTb = GetCiteTable(dbAccount);
                    citeTb.ReadFromDb(true);
                }
                GC.Collect();
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        public static void AddOrUpdateRule(RobotRuleEntity rule)
        {
            GetCiteTable(rule.DbAccount).AddOrUpdateRule(rule);
        }

        public static void RemoveRule(RobotRuleEntity rule)
        {
            GetCiteTable(rule.DbAccount).RemoveRule(rule);
        }
    }
}
