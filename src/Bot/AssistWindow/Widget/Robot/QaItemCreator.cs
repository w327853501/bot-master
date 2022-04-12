using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Bot.Common.Account;
using Bot.Common.TreeviewHelper;
using Bot.Automation.ChatDeskNs;
using Bot.Robot;
using DbEntity;
using BotLib.Extensions;
using BotLib.Misc;
using BotLib.Wpf.Extensions;
using BotLib;
using Bot.Robot.Rule.Finder;
using Bot.Robot.Testor;
using Bot.Robot.Rule.QaCiteTable;
using Bot.Common.ImageHelper;

namespace Bot.AssistWindow.Widget.Robot
{
	public class QaItemCreator
	{
		private ChatDesk _desk;
		private CtlRobotQA _ctlRobotQa;
		private string _pubDbAccountDontUse;

		public string PubDbAccount
		{
			get
			{
				if (_pubDbAccountDontUse == null)
				{
					_pubDbAccountDontUse = AccountHelper.GetPubDbAccount(_desk.Seller);
				}
				return _pubDbAccountDontUse;
			}
		}

		public QaItemCreator(ChatDesk desk, CtlRobotQA qa)
		{
			_desk = desk;
			_ctlRobotQa = qa;
		}


		private void AddAnswerItem(TreeViewItem it, HashSet<RobotRuleEntity> rules, string msg)
		{
			var btfMessage = GetBeautifyMessage(msg);
			if (rules.xIsNullOrEmpty())
			{				
				AddMissedItem(it, btfMessage);
				return;
			}
			var ansNo = 0;
			foreach (var r in rules.xSafeForEach())
			{
				AddHitAnswerItem(it, r, btfMessage, ref ansNo);
			}			
		}

		private void AddHitAnswerItem(TreeViewItem it, RobotRuleEntity r, string msg, ref int n)
		{
			var answers = r.GetAnswersV3();
			for (int i = 0; i < answers.Count; i++)
			{
				n++;
				AddHitAnswerItem(it, answers[i], n, r, msg);
			}
		}

		private void AddHitAnswerItem(TreeViewItem it, AnswerWithImage awi, int n, RobotRuleEntity r, string msg)
		{
			var answerText = string.Format("答{0}：{1}", n, awi.Answer);
			var ctlLeaf = new CtlLeaf(answerText, null, null, awi.ImageName, RuleAnswerImageHelper.UseImage);
			ctlLeaf.ToolTip = "单击引用，双击发送";
			if (_desk != null)
			{
				ctlLeaf.Tag = awi;
				ctlLeaf.MouseDown += OnMouseDown;
			}
			var ansIt = it.xAppend(ctlLeaf);
			var testResult = r.GetMatchResult(msg);
			var tboxintention = TextBlockEx.Create("规则意图：", new object[0]);
			tboxintention.xAppendText(r.Intention);
			var editHylink = LinkCreator.CreateEditLink(r, _ctlRobotQa);
			tboxintention.xAppendLinksToTextBlock(editHylink);
			ansIt.xAppend(tboxintention);
			ansIt.xAppend("是否命中【完全匹配】的问题：" + (testResult.IsMatchRuleQuestion ? "是" : "否"), null);
			if (!testResult.PatternMatchDescs.xIsNullOrEmpty())
			{
				ansIt = ansIt.xAppend("命中【关键字匹配】的模式：");
				ansIt.IsExpanded = true;
				foreach (var header in testResult.PatternMatchDescs)
				{
					ansIt.xAppend(header);
				}
			}
		}

		public TreeViewItem Create(string msg, int n)
		{			
			var qaHeader = TextBlockEx.CreateWithColor(string.Format("问{0}：", n), Brushes.LightGray);
			qaHeader.xAppendText(GetBeautifyMessage(msg));
			var it = TreeViewItemEx.xCreateByHeader(qaHeader);
			it.Tag = msg;
			it.Margin = new Thickness(0.0, 10.0, 0.0, 0.0);
			it.VerticalAlignment = VerticalAlignment.Top;
			it.xAppend("*");
			it.Expanded += OnAnswerItemExpanded;
			it.ContextMenu = _ctlRobotQa.MenuQuestion;
			return it;
		}

		public string GetBeautifyMessage(string msg)
		{
			var rt = "";
			try
			{
				if (!string.IsNullOrEmpty(msg))
				{
					rt = msg.Replace("\r\n", " ").Replace("\n", " ");
				}
			}
			catch (Exception e)
			{
				Log.Exception(e);
			}
			return rt;
		}

		private async void OnAnswerItemExpanded(object sender, RoutedEventArgs e)
		{
			if (sender != e.OriginalSource)
			{
				e.Handled = true;
			}
			else
			{
				var item = sender as TreeViewItem;
				item.Items.Clear();
				item.xAppend("正在搜索...", null);
				var stm = (item.Tag as string);
				var rset = await Task.Factory.StartNew(()=> {
					return CiteTableManager.GetMatchedRule(stm,AccountHelper.GetPubDbAccount(_desk.Seller));
				});
				item.Items.Clear();
				AddAnswerItem(item, rset, stm);
			}
		}

		private void AddMissedItem(TreeViewItem it, string msg)
		{
			var tboxheader = new TextBlock();
			tboxheader.xAppendText("没有命中规则（");
			var appendIt = LinkCreator.CreateAppendLink(msg, _ctlRobotQa);
			tboxheader.Inlines.Add(appendIt);
			tboxheader.xAppendText("，");
			appendIt = LinkCreator.CreateNewLink(msg, _ctlRobotQa);
			tboxheader.Inlines.Add(appendIt);
			tboxheader.xAppendText("）");
			it.xAppend(tboxheader);
		}

		private void OnMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				if (e.ClickCount == 1)
				{
					DelayCaller.CallAfterDelayInUIThread(() => {
						e.Handled = true;
						SetOrSendAnswer(sender, false);
					}, 80);
				}
				else if (e.ClickCount == 2)
				{
					e.Handled = true;
					SetOrSendAnswer(sender, true);
				}
			}
		}

		private void SetOrSendAnswer(object ctl, bool isSend)
		{
			var ctlLeaf = ctl as CtlLeaf;
			if (ctlLeaf != null)
			{
				var awi = ctlLeaf.Tag as AnswerWithImage;
				if (awi != null)
				{
					if (isSend)
					{
						_desk.Editor.SendAnswerAsync(awi,null);
					}
					else
					{
						_desk.Editor.SetAnswerAsync(awi);
					}
				}
			}
		}

	}
}
