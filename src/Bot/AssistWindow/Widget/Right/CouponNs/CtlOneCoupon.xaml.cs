using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using Bot.Common;
using Bot.Common.Db;
using Bot.Automation.ChatDeskNs;
using BotLib;
using BotLib.Wpf.Extensions;
using DbEntity;

namespace Bot.AssistWindow.Widget.Right.CouponNs
{
	public partial class CtlOneCoupon : UserControl
	{
		private Coupon _coupon;
		private ChatDesk _desk;

		public CtlOneCoupon(Coupon coupon, ChatDesk desk)
		{
			InitializeComponent();
			InitUI(coupon, desk);
		}

		private void InitUI(Coupon cp, ChatDesk desk)
		{
			try
			{
				_coupon = cp;
				_desk = desk;
				runRemain.Text = cp.reserveCount.ToString();
				if (cp.reserveCount < 1)
				{
					lkSend.IsEnabled = false;
				}
				runQuota.Text = cp.personLimit.ToString();
				tblkCondition.Text = cp.description;
				var endTime = Convert.ToDateTime(cp.applyEndTime);
				tblkEndDate.Text = string.Format("{0}(剩 {1} 天)", endTime.ToString("yyyy-MM-dd"), (DateTime.Parse(cp.applyEndTime) - DateTime.Now).TotalDays.ToString("0.0"));
				runDenomination.Text = cp.name.ToString();
			}
			catch (Exception e)
			{
				Log.Exception(e);
			}
		}

		private void lkSend_Click(object sender, RoutedEventArgs e)
		{
			SendCoupon();
		}

		private void SendCoupon()
		{
			_desk.SendCoupon(_desk.Buyer,_coupon.activityId);
		}

		private void OnMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				if (e.ClickCount == 2)
				{
					SendCoupon();
				}
			}
		}

	}
}
