using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using Bot.Common;
using Bot.Automation.ChatDeskNs;
using Bot.Help;
using Bot.Options;
using BotLib;
using BotLib.Extensions;
using DbEntity;
using BotLib.Wpf.Extensions;

namespace Bot.AssistWindow.Widget.Right.CouponNs
{
	public partial class CtlCouponPanel : UserControl
	{
		private ChatDesk _desk;
		private string _seller;
		private string _sellerMain;

		public CtlCouponPanel(ChatDesk desk)
		{
			InitializeComponent();
			_desk = desk;
			_seller = desk.Seller;
			_sellerMain = desk.SellerMainNick;
            _desk.EvChromeConnected += _desk_EvChromeConnected; 
		}

		private void _desk_EvChromeConnected(object sender, ChatDeskEventArgs e)
		{
			DispatcherEx.xInvoke(UpdateUI);
		}

        public async void UpdateUI()
		{
			var coupons = await _desk.GetCoupons();
			ShowCoupon(coupons);
		}

		private void ClearCoupon()
		{
			spnContent.Children.Clear();
			if (wpnBottom.Children.Count > 3)
			{
				wpnBottom.Children.RemoveRange(3, wpnBottom.Children.Count - 3);
			}
		}

		private void ShowCoupon(List<Coupon> coupons)
		{
			ClearCoupon();
			foreach (var coupon in coupons)
			{
				var ctlOneCoupon = new CtlOneCoupon(coupon, _desk);
				ctlOneCoupon.Margin = new Thickness(1.0);
				spnContent.Children.Add(ctlOneCoupon);
			}
		}

		private void OnReloadClick(object sender, RoutedEventArgs e)
		{
			UpdateUI();
		}

		private void btOption_Click(object sender, RoutedEventArgs e)
		{
			WndOption.MyShow(_seller, _desk.AssistWindow, OptionEnum.Coupon, UpdateUI);
		}

	}
}
