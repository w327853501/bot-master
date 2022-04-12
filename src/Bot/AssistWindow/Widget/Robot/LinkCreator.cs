using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Bot.Automation.ChatDeskNs;
using DbEntity;
using BotLib.Wpf.Extensions;
using Bot.Robot;

namespace Bot.AssistWindow.Widget.Robot
{
	public static class LinkCreator
	{
		public static Hyperlink CreateEditLink(RobotRuleEntity r, CtlRobotQA ctlRobotQa)
		{
			var hylink = HyperLinkEx.Create("编辑规则", null, null);
			hylink.Tag = new RuleEditorTag(ctlRobotQa, null, r);
			hylink.Click += (sender, e) => 
			{
				var tag = (hylink.Tag as RuleEditorTag);
				tag.RuleEditor.EditRule(r, () => {
					tag.Rule = RobotRuleEntityEx.FindOne(r.EntityId, r.DbAccount);
				});
			};
			return hylink;
		}

		public static Hyperlink CreateNewLink(string question, CtlRobotQA ctlRobotQa)
		{
			var hylink = HyperLinkEx.Create("新建规则", null, null);
			hylink.Tag = new RuleEditorTag(ctlRobotQa, question, null);
			hylink.Click += (object sender, RoutedEventArgs e) =>
			{
				var tag = hylink.Tag as RuleEditorTag;
				tag.RuleEditor.CreateRule(tag.Question, ()=>
				{
				});
			};
			return hylink;
		}

		public static Hyperlink CreateAppendLink(string question, CtlRobotQA ctlRobotQa)
		{
			var hylink = HyperLinkEx.Create("附加到规则");
			hylink.Tag = new RuleEditorTag(ctlRobotQa, question);
			hylink.Click += (sender, e) =>
			{
				var tag = hylink.Tag as RuleEditorTag;
				tag.RuleEditor.CreateRule(tag.Question, () =>
                {
                });
            };
			return hylink;
		}

		public static void CreateAnswerLink(TextBlock answertbox, string text, ChatDesk desk)
		{
			var sendlnk = CreateSendLink(text, desk);
			var quotelnk = CreateQuoteLink(text, desk);
			answertbox.xAppendLinksToTextBlock(new Hyperlink[]
			{
				sendlnk,
				quotelnk
			});
		}

		private static Hyperlink CreateQuoteLink(string text, ChatDesk desk)
		{
			var hylink = HyperLinkEx.Create("引用", null, null);
			hylink.Tag = new SendOrSetTextTag(text, desk);
			hylink.Click += (sender,e)=> {				
				var sendOrSetTextTag = hylink.Tag as SendOrSetTextTag;
				sendOrSetTextTag.Desk.Editor.SetPlainTextAsync(sendOrSetTextTag.Text);
			};
			return hylink;
		}
		private static Hyperlink CreateSendLink(string text, ChatDesk desk)
		{
			var hylink = HyperLinkEx.Create("发送");
			hylink.Tag = new SendOrSetTextTag(text, desk);
			hylink.Click += (sender,e) => {
				var sendOrSetTextTag = hylink.Tag as SendOrSetTextTag;
				sendOrSetTextTag.Desk.Editor.SendPlainTextAsync(sendOrSetTextTag.Text);
			};
			return hylink;
		}

		private class RuleEditorTag
		{
			public readonly RuleEditor RuleEditor;
			public readonly CtlRobotQA Widget;
			public readonly string Question;
			public RobotRuleEntity Rule;
			public RuleEditorTag(CtlRobotQA qa, string question, RobotRuleEntity rule = null)
			{
				RuleEditor = ((qa != null) ? qa.RuleEditor : null);
				Widget = qa;
				Question = question;
				Rule = rule;
			}
		}

		private class SendOrSetTextTag
		{
			public string Text;
			public ChatDesk Desk;
			public SendOrSetTextTag(string text, ChatDesk desk)
			{
				Text = text;
				Desk = desk;
			}
		}
    }
}
