using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Threading;
using BotLib.Extensions;

namespace Bot.AssistWindow.Widget.Bottom
{
	public partial class CtlBuyer : UserControl
    {
		public CtlBuyer()
		{
			InitializeComponent();
		}

		public void ShowBuyer(string buyer)
		{
			if (buyer.xIsNullOrEmptyOrSpace())
			{
				tbkBuyer.Text = "没有检测到顾客";
			}
			else
			{
				tbkBuyer.Text = buyer;
			}
		}

		public void ShowBuyerAndChatReadable(string buyer)
		{
			tbkBuyer.Text = "";
			ShowBuyer(buyer);
		}
	}
}
