using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Bot.Common.Account;
using Bot.Common.Windows;
using Bot.Robot.Rule.QaCiteTable;
using DbEntity;
using BotLib.Extensions;
using BotLib.Wpf.Extensions;
using System.Media;
using static Bot.Robot.RobotRuleEntityEx;

namespace Bot.Robot
{
	public partial class WndRuleTestor : EtWindow
	{
		private string _dbAccount;
		public WndRuleTestor(string seller)
		{
			InitializeComponent();
			_dbAccount = AccountHelper.GetPubDbAccount(seller);
		}

		public static void MyShowDialog(string seller, WndRobotRuleManager wndRobotRuleManagerV2)
		{
			ShowSameNickOneInstance(seller, ()=> { return new WndRuleTestor(seller); }, wndRobotRuleManagerV2, false);
		}

		private void btnTest(object sender, RoutedEventArgs e)
		{
			DoTest();
		}

		private void DoTest()
		{
			var q = tboxQuestion.Text.Trim();
			if (string.IsNullOrEmpty(q))
			{
				SystemSounds.Beep.Play();
				tboxQuestion.Focus();
			}
			else
			{
				var smartAnswers = CiteTableManager.GetTestModeAnswers(q, _dbAccount);
				ShowMatchAnswers(smartAnswers,q);
			}
		}

		private void ShowMatchAnswers(List<SmartAnswerMatchInfo> answers, string q)
		{
			tvResult.Items.Clear();
			if (answers.xIsNullOrEmpty())
			{
				tvResult.xAppend("找到不答案", null);
				return;
			}
			if (answers.Count > 0)
			{
				var matchRule = tvResult.xAppend(string.Format("匹配命中的规则({0})", answers.Count), null);
				for (int i = 0; i < answers.Count; i++)
				{
					ShowMatchAnswers(answers[i], matchRule, q, i + 1);
				}
			}
		}

		private void ShowMatchAnswers(SmartAnswerMatchInfo answer, TreeViewItem ruleItem, string question, int no)
		{
			string format = "答案{0}: {1}";
			object arg = no;
			var answerText = "图片";
			if (answer != null && answer.AnswerWithImg != null)
			{
				answerText = answer.AnswerWithImg.Answer;
			}
			var subItem = ruleItem.xAppend(string.Format(format, arg, answerText));
			subItem.IsExpanded = true;
			var testResult = answer.Rule.GetMatchResult(question);
			subItem.xAppend("规则意图：" + answer.Rule.Intention, null);
			subItem.xAppend("是否命中【完全匹配】中的模板：" + (testResult.IsMatchRuleQuestion ? "是" : "否"));
			if (!testResult.PatternMatchDescs.xIsNullOrEmpty())
			{
				subItem = subItem.xAppend("命中【关键字匹配】中的模板：");
				subItem.IsExpanded = true;
				foreach (var header in testResult.PatternMatchDescs)
				{
					subItem.xAppend(header);
				}
			}
		}

	}
}
