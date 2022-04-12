using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using Bot.Common;
using Bot.Common.TreeviewHelper;
using Bot.Robot.Rule.QaCiteTable;
using Bot.Robot.RuleEditor;
using DbEntity;

namespace Bot.Robot
{
	public partial class CtlRuleViewer : UserControl
	{
		private string _seller;
		private string _dbAccount;
		public CtlRuleViewer()
		{
			InitializeComponent();
		}

		public void Init(string seller, string dbAccount, bool isShowRulePattern = false)
		{
			_seller = seller;
			_dbAccount = dbAccount;
			tvRule.Init(new RuleTreeViewController(_dbAccount, _seller), null, null, false, false, null, null, ()=> { return QnHelper.Auth.CanEditRobotRule(_seller); });
		}

		private void btnEdit_Click(object sender, RoutedEventArgs e)
		{
			if (QnHelper.Auth.CanEditRobotRule(_seller))
			{
				tvRule.EditAsync();
			}
		}

		private void btnDelete_Click(object sender, RoutedEventArgs e)
		{
			if (QnHelper.Auth.CanEditRobotRule(_seller))
			{
				var n = tvRule.Delete();
				if (n != null)
				{
					var rule = n as RobotRuleEntity;
					if (rule != null)
					{
						CiteTableManager.RemoveRule(rule);
					}
				}
			}
		}

		private void btnNewCata_Click(object sender, RoutedEventArgs e)
		{
			if (QnHelper.Auth.CanEditRobotRule(_seller))
			{
				tvRule.CreateCatalog();
			}
		}

		private void btnNew_Click(object sender, RoutedEventArgs e)
		{
			if (QnHelper.Auth.CanEditRobotRule(_seller))
			{
				tvRule.Create();
			}
		}

		private void tvRule_EvDoubleClickLeafNode(object sender, MouseButtonEventArgs e)
		{
			if (QnHelper.Auth.CanEditRobotRule(_seller))
			{
				tvRule.EditAsync();
			}
		}
	}
}
