using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Bot.Options
{
	public partial class CtlComponentOption : UserControl
	{
		public CtlComponentOption()
		{
			InitializeComponent();
		}

		public string ComponentName
		{
			get
			{
				return tblkTitle.Text.Trim();
			}
			set
			{
				tblkTitle.Text = value.Trim();
			}
		}

		public bool IsCompVisible
		{
			get
			{
				return rbtShow.IsChecked ?? false;
			}
			set
			{
				if (value)
				{
					rbtShow.IsChecked = true;
				}
				else
				{
					rbtHide.IsChecked = true;
				}
			}
		}
	}
}
