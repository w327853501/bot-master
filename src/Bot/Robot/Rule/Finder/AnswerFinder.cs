using Bot.Robot.Rule.QaCiteTable;
using BotLib;
using DbEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotLib.Extensions;
using Bot.Common.Account;
using Bot.ChatRecord;

namespace Bot.Robot.Rule.Finder
{
    public class AnswerFinder
    {
		public static List<SmartAnswerMatchInfo> GetAnswersOfSeller(List<ChatMessage> qlist, string seller, int n)
		{
			var answers = new List<SmartAnswerMatchInfo>();
			try
			{
				if (!qlist.xIsNullOrEmpty())
				{
					qlist.ForEach(q=> {
						answers.xAddRange(CiteTableManager.GetAnswers(q.MessageText, AccountHelper.GetPubDbAccount(seller)));					
					});
				}
			}
			catch (Exception e)
			{
				Log.Exception(e);
			}
			return answers;
		}
	}
}
