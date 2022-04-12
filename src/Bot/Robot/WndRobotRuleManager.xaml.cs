using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using Bot.Common;
using Bot.Common.Account;
using Bot.Common.Db;
using Bot.Common.TreeviewHelper;
using Bot.Common.Windows;
using Bot.Help;
using Bot.Robot.Rule.QaCiteTable;
using DbEntity;
using BotLib;
using BotLib.Extensions;
using BotLib.Wpf.Extensions;
using Bot.Robot.Rule.Importer.Legacy;

namespace Bot.Robot
{
	public partial class WndRobotRuleManager : EtWindow
	{
		public WndRobotRuleManager(string seller)
		{
			InitializeComponent();
			Seller = seller;
			DbSyner.EvHasRobotRuleDowned += OnEvHasRobotRuleDowned;
		}

		private void OnEvHasRobotRuleDowned(object sender, EventArgs e)
		{
			DispatcherEx.xInovkeLowestPriority(()=> {
				if (IsLoaded)
				{
					UpdateUI();
				}
			});
		}

		public static WndRobotRuleManager MyShow(string seller, Window owner)
		{
			return ShowSameShopOneInstance(seller,
				()=> {
					return new WndRobotRuleManager(seller);
				}, owner);
		}

		private void EtWindow_Loaded(object sender, RoutedEventArgs e)
		{
			UpdateUI();
		}

		private void btnNewCata_Click(object sender, RoutedEventArgs e)
		{
			GetShowingTreeview().CreateCatalog();
		}

		private CtlTreeView GetShowingTreeview()
		{
			return ctlPubRule.tvRule;
		}

		private void btnNew_Click(object sender, RoutedEventArgs e)
		{
			GetShowingTreeview().Create();
		}

		private void btnEdit_Click(object sender, RoutedEventArgs e)
		{
			GetShowingTreeview().EditAsync();
		}

		private void btnDelete_Click(object sender, RoutedEventArgs e)
		{
			if (QnHelper.Auth.CanEditRobotRule(Seller))
			{
				var n = GetShowingTreeview().Delete();
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

		private async void btnSyn1_Click(object sender, RoutedEventArgs e)
		{
			btnSyn1.IsEnabled = false;
			pbar.Visibility = Visibility.Visible;
			await Task.Factory.StartNew(()=>
			{
				DbSyner.Syn();
			}, TaskCreationOptions.LongRunning);
			btnSyn1.IsEnabled = true;
			pbar.Visibility = Visibility.Collapsed;
			UpdateUI();
		}

		public static void UpdateRobotManagerUI(string dbAccount)
		{
			var rm = GetRobotRuleManager(dbAccount);
			if (rm != null)
			{
				rm.UpdateUI();
			}
		}

		private static WndRobotRuleManager GetRobotRuleManager(string dbAccount)
		{
			WndRobotRuleManager robotRuleManager = null;
			foreach (var rrm in WindowEx.GetAppWindows<WndRobotRuleManager>())
			{
				if (AccountHelper.GetPubDbAccount(rrm.Seller) == dbAccount)
				{
					robotRuleManager = rrm;
					break;
				}
			}
			return robotRuleManager;
		}

		private void UpdateUI()
		{
			string dbAccount = AccountHelper.GetPubDbAccount(Seller);
			ctlPubRule.Init(Seller, dbAccount, false);
		}

		private void Command_Delete(object sender, ExecutedRoutedEventArgs e)
		{
			btnDelete_Click(sender, e);
		}

		private void sbtTest_Click(object sender, RoutedEventArgs e)
		{
            WndRuleTestor.MyShowDialog(Seller, this);
        }

		private void btnHelp_Click(object sender, RoutedEventArgs e)
		{
		}
		private void EtWindow_Closed(object sender, EventArgs e)
		{
			if (!WindowEx.HasShowingWindowOtherThan<WndRobotRuleManager>(this))
			{
				DbSyner.SynData();
			}
		}

		private void btnChatRecordMgr_Click(object sender, RoutedEventArgs e)
		{
			//ShowSameShopOneInstance<WndHistoryRecordViewer>(Seller, null, null);
		}

		private void btnImport_Click(object sender, RoutedEventArgs e)
		{
			Import();
		}

		private async void Import()
		{
			var fn = OpenFileDialogEx.GetOpenFileName("导入自动回复规则", "自动回复规则文件(*.csv)|*.csv", null);
			if (!string.IsNullOrEmpty(fn))
			{
				IsEnabled = false;
				pbar.Visibility = Visibility.Visible;
				try
				{
					var dbAccount = AccountHelper.GetPubDbAccount(Seller);
					var importNum = await Task.Factory.StartNew<int>(()=> { 
						return RuleImporter.Import(fn, dbAccount);
					}, TaskCreationOptions.LongRunning);
					MsgBox.ShowNotTipDialog(string.Format("共导入【{0}】条规则\r\n\r\n注：导入的数据已上传到服务器，其它电脑10分钟内即可同步到。", importNum), null, null, null);
					ctlPubRule.Init(Seller, dbAccount, false);
				}
				catch (Exception exp)
				{
					Log.Exception(exp);
					MsgBox.ShowErrDialog(exp.Message);
				}
				IsEnabled = true;
				pbar.Visibility = Visibility.Collapsed;
			}
		}
	}
}
