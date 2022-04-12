using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Bot.AssistWindow;
using Bot.Help;
using DbEntity;
using BotLib;
using BotLib.Extensions;
using BotLib.Wpf.Extensions;

namespace Bot.Options
{
	public partial class CtlPanelOptions : UserControl, IOptions
	{
		private string _seller;
		private string _sellerMain;
		public OptionEnum OptionType
		{
			get
			{
				return OptionEnum.Panel;
			}
		}

		public CtlPanelOptions(string seller)
		{
			InitializeComponent();
			InitUI(seller);
		}

		public HelpData GetHelpData()
		{
			return null;
		}

		public void InitUI(string seller)
		{
			_seller = seller;
			_sellerMain = TbNickHelper.GetMainPart(seller);
			panel.Children.Clear();
			var tabCsv = Params.Panel.GetRightPanelCompOrderCsv(_seller);
			var tabs = tabCsv.Split(',');
			foreach (var tabName in tabs)
			{
				ShowCtlOption(tabName);
			}
		}


		private void ShowCtlOption(string tabName)
		{
			var ctlComponentOption = new CtlComponentOption();
			if (tabName == "订单")
			{
				ctlComponentOption.IsCompVisible = Params.Panel.GetOrderIsVisible(_seller);
				ctlComponentOption.ComponentName = "订单";
			}
			if (tabName == "商品")
			{
				ctlComponentOption.IsCompVisible = Params.Panel.GetGoodsKnowledgeIsVisible(_seller);
				ctlComponentOption.ComponentName = "商品";
			}
			else if (tabName == "话术")
			{
				ctlComponentOption.IsCompVisible = Params.Panel.GetShortcutIsVisible(_seller);
				ctlComponentOption.ComponentName = "话术";
			}
			if (tabName == "机器人")
			{
				ctlComponentOption.IsCompVisible = Params.Panel.GetRobotIsVisible(_seller);
				ctlComponentOption.ComponentName = "机器人";
			}
			if (tabName == "优惠券")
			{
				ctlComponentOption.IsCompVisible = Params.Panel.GetCouponIsVisible(_seller);
				ctlComponentOption.ComponentName = "优惠券";
			}
			ctlComponentOption.btnMoveLeft.Click -= btnMoveLeft_Click; 
			ctlComponentOption.btnMoveLeft.Click += btnMoveLeft_Click;
			ctlComponentOption.btnMoveRight.Click -= btnMoveRight_Click; 
			ctlComponentOption.btnMoveRight.Click += btnMoveRight_Click;
			ctlComponentOption.Margin = new Thickness(5.0);
			panel.Children.Add(ctlComponentOption);
		}

		private void btnMoveRight_Click(object sender, RoutedEventArgs e)
		{
			var source = sender as Button;
			var element = source.xFindAncestor<CtlComponentOption>();
			int idx = panel.Children.IndexOf(element);
			if (idx < panel.Children.Count - 1)
			{
				panel.Children.RemoveAt(idx);
				panel.Children.Insert(idx + 1, element);
			}
			else
			{
				Util.Beep();
			}
		}

		private void btnMoveLeft_Click(object sender, RoutedEventArgs e)
		{
			var source = sender as Button;
			var element = source.xFindAncestor<CtlComponentOption>();
			int idx = panel.Children.IndexOf(element);
			if (idx > 0)
			{
				panel.Children.RemoveAt(idx);
				panel.Children.Insert(idx - 1, element);
			}
			else
			{
				Util.Beep();
			}
		}

		public void NavHelp()
		{

		}

		public void RestoreDefault()
		{
			Params.Panel.SetGoodsKnowledgeIsVisible(_seller, true);
			Params.Panel.SetOrderIsVisible(_seller, true);
			Params.Panel.SetRightPanelCompOrderCsv(_seller, "话术,商品,机器人,订单,优惠券");
			Params.Panel.SetRobotIsVisible(_seller, true);
			Params.Panel.SetShortcutIsVisible(_seller, true);
			InitUI(_seller);
			ReShowAfterChangePanelOption();
		}

		public void Save(string seller)
		{
			var tabset = new HashSet<string>();
			var isChanged = false;
			foreach (var ctl in panel.Children)
			{
				var ctlComponentOption = (CtlComponentOption)ctl;
				var tabName = ctlComponentOption.ComponentName;
				tabset.Add(tabName);
				if (tabName == "话术")
				{
					if (Params.Panel.GetShortcutIsVisible(seller) != ctlComponentOption.IsCompVisible)
					{
						Params.Panel.SetShortcutIsVisible(seller, ctlComponentOption.IsCompVisible);
						isChanged = true;
					}
				}
				if (tabName == "商品")
				{
					if (Params.Panel.GetGoodsKnowledgeIsVisible(seller) != ctlComponentOption.IsCompVisible)
					{
						Params.Panel.SetGoodsKnowledgeIsVisible(seller, ctlComponentOption.IsCompVisible);
						isChanged = true;
					}
				}
				if (tabName == "机器人")
				{
					if (Params.Panel.GetRobotIsVisible(seller) != ctlComponentOption.IsCompVisible)
					{
						Params.Panel.SetRobotIsVisible(seller, ctlComponentOption.IsCompVisible);
						isChanged = true;
					}
				}
				if (tabName == "订单")
				{
					if (Params.Panel.GetOrderIsVisible(seller) != ctlComponentOption.IsCompVisible)
					{
						Params.Panel.SetOrderIsVisible(seller, ctlComponentOption.IsCompVisible);
						isChanged = true;
					}
				}
				if (tabName == "优惠券")
				{
					if (Params.Panel.GetOrderIsVisible(seller) != ctlComponentOption.IsCompVisible)
					{
						Params.Panel.SetOrderIsVisible(seller, ctlComponentOption.IsCompVisible);
						isChanged = true;
					}
				}
			}
			var tabcsv = tabset.xToString(",", true);
			if (Params.Panel.GetRightPanelCompOrderCsv(seller) != tabcsv)
			{
				Params.Panel.SetRightPanelCompOrderCsv(seller, tabcsv);
				isChanged = true;
			}
			if (isChanged)
			{
				ReShowAfterChangePanelOption();
			}
		}

		private void ReShowAfterChangePanelOption()
		{
			var wndAssist = WndAssist.FindWndAssist(_seller);
			wndAssist.ctlRightPanel.ReShowAfterChangePanelOption();
		}
	}
}
