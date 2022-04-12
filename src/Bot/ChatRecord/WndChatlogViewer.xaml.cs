using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using Bot.Common.Windows;
using Bot.Common;
using DbEntity;
using BotLib.Extensions;
using Bot.Automation.ChatDeskNs;
using System.Windows.Controls;
using System.Data;

namespace Bot.ChatRecord
{
	public partial class WndChatlogViewer : EtWindow
	{
		public WndChatlogViewer()
		{
			InitializeComponent();
            Loaded += WndChatlogViewer_Loaded;
		}

        private void WndChatlogViewer_Loaded(object sender, RoutedEventArgs e)
		{
			dpFrom.SelectedDate = BatTime.Now.AddDays(-7).Date;
			dpTo.SelectedDate = BatTime.Now.Date;
		}

        public static void MyShow(string seller)
		{
            ShowSameNickOneInstance(seller, () => {
                var wnd = new WndChatlogViewer();
                wnd.Seller = seller;
                return wnd;
            });
		}

		public string SellerMainNick
		{
			get
			{
				return TbNickHelper.GetMainPart(base.Seller);
			}
		}

		private async void btnSearch_Click(object sender, RoutedEventArgs e)
		{
			var btime = dpFrom.SelectedDate.Value.xToTimeStamp();
			var etime = dpTo.SelectedDate.Value.xToTimeStamp();
			var desk = ChatDesk.GetDeskFromCache(Seller);
			var recentMsgs = await desk.GetRecentNoReplyMessages(btime,etime);
			dgMain.ItemsSource = recentMsgs;
		}

		private void btnReset_Click(object sender, RoutedEventArgs e)
		{
			dpFrom.SelectedDate = BatTime.Now.AddDays(-7).Date;
			dpTo.SelectedDate = BatTime.Now.Date;
		}

		private void cboxLastDays_Click(object sender, RoutedEventArgs e)
		{
			var isChecked = cboxLastDays.IsChecked;
			if (isChecked.HasValue && isChecked.Value)
			{
				dpFrom.SelectedDate = BatTime.Now.AddDays(-3).Date;
				dpTo.SelectedDate = BatTime.Now.Date;
			}
		}

		private void btnOpenChat_Click(object sender, RoutedEventArgs e)
		{
			var desk = ChatDesk.GetDeskFromCache(Seller); 
			var buyerNick = (sender as Button).Tag.ToString();
			desk.OpenChat(buyerNick);
		}

		private async void btnTansferContact_Click(object sender, RoutedEventArgs e)
		{
			var desk = ChatDesk.GetDeskFromCache(Seller);
			var employees = await desk.GetEmployees();
			WndSelectEmployee.MyShow(desk.Seller,employees, subNick => {
				var buyerNick = (sender as Button).Tag.ToString();
				desk.TransferContact(buyerNick,subNick);
			});
		}
	}
}
